import { environment as buildEnvironment } from '../../environment';

declare global {
    interface Window {
        __env?: {
            apiUrl?: string;
            stripePublishableKey?: string;
        };
    }
}

function pick(runtimeValue: string | undefined, fallback: string): string {
    // Unset Docker env vars are substituted to an empty string by envsubst,
    // and env.js is absent entirely under `ng serve` — fall back to build defaults either way.
    if (!runtimeValue) {
        return fallback;
    }
    return runtimeValue;
}

const runtime = typeof window !== 'undefined' ? window.__env : undefined;

export const environment = {
    apiUrl: pick(runtime?.apiUrl, buildEnvironment.apiUrl),
    stripePublishableKey: pick(runtime?.stripePublishableKey, buildEnvironment.stripePublishableKey)
};
