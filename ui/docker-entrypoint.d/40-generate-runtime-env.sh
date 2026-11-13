#!/bin/sh
set -eu

envsubst '${API_BASE_URL} ${STRIPE_PUBLISHABLE_KEY} ${EVENT_DATE_LABEL}' \
  < /usr/share/nginx/html/env.template.js \
  > /usr/share/nginx/html/env.js
