#!/usr/bin/env bash
set -Eeuo pipefail
umask 077
cd -- "$(dirname -- "$(readlink -f -- "$0")")"
bundle=$PWD
current=/opt/forfit/incoming/compose.images.yaml
release=/opt/forfit/releases/full-v3-$(date -u +%Y%m%dT%H%M%SZ)-$$
[[ $EUID == 0 ]] || { echo 'Run as root.'; exit 1; }
for tool in docker curl sha256sum flock; do command -v "$tool" >/dev/null; done
exec 9>/run/lock/forfit-update.lock
flock -n 9 || { echo 'Another ForFit update is running.'; exit 1; }
[[ -f "$current" && -f /etc/forfit/appsettings.Production.json ]] || { echo 'Existing config missing; nothing changed.'; exit 1; }
sha256sum -c SHA256SUMS
docker compose -p forfit-phase1 -f compose.yaml config --quiet
docker compose -p forfit-phase1 -f "$current" config --quiet
docker network inspect novastay-net --format '{{.Name}}' >/dev/null
old_services=$(docker compose -p forfit-phase1 -f "$current" config --services)
for service in $old_services; do
  [[ "$service" == api || "$service" == web || "$service" == ssr ]] || { echo 'Unexpected service; nothing changed.'; exit 1; }
  name=forfit-phase1-$service-1
  [[ $(docker inspect --format '{{index .Config.Labels "com.docker.compose.project"}}' "$name") == forfit-phase1 ]]
  [[ $(docker inspect --format '{{index .Config.Labels "com.docker.compose.service"}}' "$name") == "$service" ]]
done
[[ " $old_services " == *api* && " $old_services " == *web* ]]
[[ $(docker port forfit-phase1-api-1 8081/tcp) == 127.0.0.1:18081 ]] || { echo 'Unexpected published port; nothing changed.'; exit 1; }
docker_root=$(docker info --format '{{.DockerRootDir}}')
available=$(df -Pk "$docker_root" | awk 'NR==2 {print $4}')
(( available >= 1572864 )) || { echo 'Need 1.5GiB free Docker disk; no cleanup performed.'; exit 1; }
memory=$(awk '/MemAvailable:/ {print $2}' /proc/meminfo)
(( memory >= 665600 )) || { echo 'Need 650MiB available RAM; nothing changed.'; exit 1; }
install -d -m 700 "$release"
cp -- "$current" "$release/previous.yaml"
printf 'services:\n' > "$release/rollback-images.yaml"
for service in $old_services; do
  old_image=$(docker inspect --format '{{.Image}}' "forfit-phase1-$service-1")
  printf '  %s:\n    image: "%s"\n' "$service" "$old_image" >> "$release/rollback-images.yaml"
done
docker load -i images.tar
for image in forfit-api:full-v3-20261031 forfit-web:full-v3-20261031 forfit-ssr:full-v3-20261031; do
  [[ $(docker image inspect --format '{{.Os}}/{{.Architecture}}' "$image") == linux/amd64 ]]
done
healthy() {
  local attempt code page
  for attempt in {1..30}; do
    code=$(curl --max-time 5 -s -o /dev/null -w '%{http_code}' http://127.0.0.1:18081/health || true)
    if [[ "$code" == 200 ]]; then
      code=$(curl --max-time 10 -s -o /dev/null -w '%{http_code}' http://127.0.0.1:18081/api/products || true)
      page=$(curl --max-time 15 -fsS http://127.0.0.1:18081/ 2>/dev/null || true)
      if [[ "$code" == 200 && -n "$page" ]]; then
        if [[ ${1:-new} == old ]]; then return 0; fi
        if [[ "$page" == *'id="public-data"'* && "$page" == *'<h1'* ]] &&
          curl --max-time 10 -fsS http://127.0.0.1:18081/sitemap.xml 2>/dev/null | grep -q '<urlset'; then return 0; fi
      fi
    fi
    sleep 2
  done
  return 1
}
rollback() {
  trap - ERR INT TERM
  echo 'Update failed/interrupted; rolling back ForFit only.'
  # Remove network-namespace dependents before recreating the API owner.
  docker compose -p forfit-phase1 -f "$bundle/compose.yaml" rm -s -f web ssr || true
  local ok=1 service
  docker compose -p forfit-phase1 -f "$release/previous.yaml" -f "$release/rollback-images.yaml" up -d --no-deps --no-build --pull never --force-recreate api || ok=0
  if [[ "$old_services" == *ssr* ]]; then
    docker compose -p forfit-phase1 -f "$release/previous.yaml" -f "$release/rollback-images.yaml" up -d --no-deps --no-build --pull never --force-recreate ssr || ok=0
  fi
  docker compose -p forfit-phase1 -f "$release/previous.yaml" -f "$release/rollback-images.yaml" up -d --no-deps --no-build --pull never --force-recreate web || ok=0
  docker compose -p forfit-phase1 -f "$release/previous.yaml" -f "$release/rollback-images.yaml" config > "$release/restored.yaml" || ok=0
  install -m 600 "$release/restored.yaml" "$current" || ok=0
  if [[ $ok == 1 ]] && healthy old; then echo 'Previous ForFit restored. Update NOT successful.';
  else echo "ROLLBACK NEEDS ATTENTION: $release"; fi
  exit 1
}
trap rollback ERR INT TERM
if [[ "$old_services" == *ssr* ]]; then
  docker compose -p forfit-phase1 -f "$current" rm -s -f web ssr
else
  docker compose -p forfit-phase1 -f "$current" rm -s -f web
fi
for service in api ssr web; do
  docker compose -p forfit-phase1 -f "$bundle/compose.yaml" up -d --no-deps --no-build --pull never --force-recreate "$service"
done
healthy new
cp -- compose.yaml "$release/compose.yaml"
install -m 600 compose.yaml "$current"
trap - ERR INT TERM
echo 'ForFit full-v3 updated: API/products/SSR HTML/sitemap checks passed.'
echo "Rollback snapshot: $release"
echo "Manual rollback: bash $bundle/rollback.sh $release"
echo 'No DB restore/migration, secret edits, host Nginx reload, prune or other-project restart.'
echo 'Verify login, cart, manual orders and image framing in browser. SSR rollout is not proof of SEO rankings or mobile visual acceptance.'
