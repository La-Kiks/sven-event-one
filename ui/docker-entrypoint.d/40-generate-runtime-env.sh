#!/bin/sh
set -eu

envsubst '${API_BASE_URL} ${STRIPE_PUBLISHABLE_KEY}' \
  < /usr/share/nginx/html/env.template.js \
  > /usr/share/nginx/html/env.js
