import { useRouteLoaderData } from 'react-router-dom';
import type { readBrowserSession } from '../api/browserSession';

export function useCurrentUser() {
    return useRouteLoaderData<typeof readBrowserSession>('browser-session') ?? null;
}
