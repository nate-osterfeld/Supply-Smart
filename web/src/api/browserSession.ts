import axios from 'axios';
import type { AuthResponseDto } from '../models';

const configuredApi = new URL(import.meta.env.VITE_BASEURL || '/', location.origin);
if (configuredApi.origin !== location.origin) {
    throw new Error('SupplySmart requires its browser API on the same origin as the application.');
}
export const apiBaseUrl = configuredApi.origin + configuredApi.pathname.replace(/\/$/, '');

export const csrfHeader = 'X-SupplySmart-CSRF';
let csrfToken: string | null = null;
export function sessionRequestToken() { return csrfToken; }

export async function readBrowserSession(): Promise<AuthResponseDto | null> {
    const { data } = await axios.get<{ user: AuthResponseDto | null; csrfToken: string | null }>(
        `${apiBaseUrl}/api/auth/session`
    );
    csrfToken = data.csrfToken;
    return data.user;
}

// Only these application-owned fields need to survive signing in on a Stripe return.
export function billingReturnSearch(search: string): string {
    const params = new URLSearchParams(search);
    if (params.get('tab') !== 'settings') return '';
    const target = new URLSearchParams({ tab: 'settings' });
    for (const key of ['billing_success', 'billing_canceled', 'session_id']) {
        const value = params.get(key);
        if (value) target.set(key, value);
    }
    return '?' + target.toString();
}

const changeKey = 'supplysmart_session_change';
export function sessionChanged(search = '') {
    sessionStorage.removeItem('supplysmart_location_id');
    localStorage.setItem(changeKey, crypto.randomUUID());
    window.location.replace('/' + billingReturnSearch(search));
}

export function initializeBrowserSession() {
    // Upgrade old browser installations without retaining previously stored credentials.
    for (const key of ['token', 'org_id', 'email', 'name', 'role', 'location_id'])
        localStorage.removeItem(`supplysmart_${key}`);
    window.addEventListener('storage', event => {
        if (event.key === changeKey) {
            sessionStorage.removeItem('supplysmart_location_id');
            window.location.replace('/');
        }
    });
    window.addEventListener('pageshow', event => {
        if (event.persisted) window.location.reload();
    });
}
