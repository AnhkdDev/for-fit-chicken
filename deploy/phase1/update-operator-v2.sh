#!/usr/bin/env bash
set -Eeuo pipefail
umask 077
cd -- "$(dirname -- "$(readlink -f -- "$0")")"
bundle=$PWD
current=/opt/forfit/incoming/compose.images.yaml
release=/opt/forfit/releases/operator-v2-$(date -u +%Y%m%dT%H%M%SZ)
[[ $EUID == 0 ]] || { echo 'Run as root.'; exit 1; }
for tool in docker curl sha256sum flock; do command -v "$tool" >/dev/null; done
exec 9>/run/lock/forfit-update.lock
flock -n 9 || { echo 'Another ForFit update is running.'; exit 1; }
[[ -f "$current" && -f /etc/forfit/appsettings.Production.json ]] || { echo 'Existing ForFit config missing.'; exit 1; }
sha256sum -c SHA256SUMS
docker compose -p forfit-phase1 -f compose.yaml config --quiet
docker network inspect novastay-net --format '{{.Name}}'
for service in api web; do
  name=forfit-phase1-$service-1
  [[ $(docker inspect --format '{{index .Config.Labels "com.docker.compose.project"}}' "$name") == forfit-phase1 ]]
  [[ $(docker inspect --format '{{index .Config.Labels "com.docker.compose.service"}}' "$name") == "$service" ]]
done
# Reserve room for load/unpack; never prune any Docker images, cache or volumes.
available=$(df -Pk /var/lib/docker | awk 'NR==2 {print $4}')
(( available >= 1572864 )) || { echo 'Need at least 1.5GiB free; no cleanup performed.'; exit 1; }
install -d -m 700 "$release"
cp -- "$current" "$release/previous.yaml"
# Record exact running image IDs for rollback, even if old tags changed.
old_api=$(docker inspect --format '{{.Image}}' forfit-phase1-api-1)
old_web=$(docker inspect --format '{{.Image}}' forfit-phase1-web-1)
printf 'services:\n  api:\n    image: "%s"\n  web:\n    image: "%s"\n' "$old_api" "$old_web" > "$release/rollback-images.yaml"
docker load -i images.tar
for image in forfit-api:phase1-20261030-operator-v2 forfit-web:phase1-20261030-operator-v2; do
  [[ $(docker image inspect --format '{{.Os}}/{{.Architecture}}' "$image") == linux/amd64 ]]
done
healthy() {
  local attempt path ok
  for attempt in {1..30}; do
    ok=1
    for path in / /health /api/products; do
      [[ $(curl --max-time 5 -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:18081$path" || true) == 200 ]] || ok=0
    done
    [[ $ok == 1 ]] && return 0
    sleep 2
  done
  return 1
}
rollback() {
  trap - ERR INT TERM
  echo 'Update failed/interrupted; restoring previous ForFit containers only.'
  docker compose -p forfit-phase1 -f compose.yaml rm -s -f web || true
  if docker compose -p forfit-phase1 -f "$release/previous.yaml" -f "$release/rollback-images.yaml" up -d --no-build --pull never --force-recreate api web && healthy; then
    echo 'Previous ForFit restored. Update NOT successful.'
  else
    echo "ROLLBACK NEEDS ATTENTION. Saved configuration: $release"
  fi
  exit 1
}
trap rollback ERR INT TERM
# Remove dependent web before API replacement because they share a network namespace.
docker compose -p forfit-phase1 -f compose.yaml rm -s -f web
docker compose -p forfit-phase1 -f compose.yaml up -d --no-build --pull never --force-recreate api web
healthy
cp -- compose.yaml "$release/compose.yaml"
install -m 600 compose.yaml "$current"
trap - ERR INT TERM
echo 'ForFit updated. Frontend, health and products returned200.'
echo "Rollback configuration saved: $release"
echo 'Now verify Operator login/menu/orders/reports/notifications in browser.'
echo 'No database restore/migration, secrets edits, Nginx reload or other-project restarts were performed.'
