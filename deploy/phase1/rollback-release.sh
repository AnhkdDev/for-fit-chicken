#!/usr/bin/env bash
set -Eeuo pipefail
umask 077
[[ $EUID == 0 && $# == 1 ]] || { echo 'Usage: bash rollback.sh /opt/forfit/releases/SNAPSHOT'; exit 1; }
release=$(readlink -f -- "$1")
[[ "$release" == /opt/forfit/releases/* && -f "$release/previous.yaml" && -f "$release/rollback-images.yaml" ]]
current=/opt/forfit/incoming/compose.images.yaml
exec 9>/run/lock/forfit-update.lock
flock -n 9 || { echo 'Another ForFit update is running.'; exit 1; }
for service in api web; do
  [[ $(docker inspect --format '{{index .Config.Labels "com.docker.compose.project"}}' "forfit-phase1-$service-1") == forfit-phase1 ]]
done
old_services=$(docker compose -p forfit-phase1 -f "$release/previous.yaml" config --services)
for service in $old_services; do [[ "$service" == api || "$service" == web || "$service" == ssr ]]; done
current_services=$(docker compose -p forfit-phase1 -f "$current" config --services)
if [[ "$current_services" == *ssr* ]]; then
  docker compose -p forfit-phase1 -f "$current" rm -s -f web ssr
else
  docker compose -p forfit-phase1 -f "$current" rm -s -f web
fi
for service in api ssr web; do
  [[ "$service" != ssr || "$old_services" == *ssr* ]] || continue
  docker compose -p forfit-phase1 -f "$release/previous.yaml" -f "$release/rollback-images.yaml" up -d --no-deps --no-build --pull never --force-recreate "$service"
done
docker compose -p forfit-phase1 -f "$release/previous.yaml" -f "$release/rollback-images.yaml" config > "$release/restored.yaml"
install -m 600 "$release/restored.yaml" "$current"
curl --retry 20 --retry-delay 2 --retry-connrefused --max-time 10 -fsS -o /dev/null http://127.0.0.1:18081/health
echo 'Previous ForFit images restored. Database/secrets/other projects unchanged. Check website and login.'
