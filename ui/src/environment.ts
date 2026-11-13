// Fallback values used by `ng serve` and whenever env.js hasn't been generated
// (e.g. no runtime config injected). In Docker, real values come from env.js,
// generated at container startup from API_BASE_URL / STRIPE_PUBLISHABLE_KEY / EVENT_DATE_LABEL
// (see ui/docker-entrypoint.d/40-generate-runtime-env.sh). See app/core/runtime-env.ts.
export const environment = {
    apiUrl: 'http://localhost:7163',
    stripePublishableKey: '',
    eventDateLabel: 'Date à confirmer'
};
