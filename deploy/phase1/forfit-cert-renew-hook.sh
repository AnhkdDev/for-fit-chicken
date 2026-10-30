#!/bin/sh
set -eu
# certonly webroot updates disk files; reload only for the ForFit lineage.
if [ "${RENEWED_LINEAGE:-}" = /etc/letsencrypt/live/4fitchicken.io.vn ]; then
    /usr/sbin/nginx -t
    /usr/bin/systemctl reload nginx
fi
