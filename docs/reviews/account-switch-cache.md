# Previous clinic data survives an account switch

React Query stores tenant users, catalog, audit and other data under shared query keys with five-minute freshness. Sign-out cleared localStorage but left this cache alive. Signing into another clinic invalidated only locations, so the previous clinic's team list was displayed without requesting the new clinic's users.

A headless Chromium check signs into clinic one, opens its team list, signs out, and signs into clinic two. Before the fix, only clinic one's users endpoint was requested and its email remained visible to clinic two. The test uses synthetic clinic records.

Cancel queries and clear their cache on sign-out and before accepting login/registration results. Clear the previous selected location, and navigate sign-out to the login route. The regression verifies two correctly scoped requests and no prior-clinic email after switching accounts.

Start Vite on port 5175 or set BASE_URL to its address. Run `node web/tests/auth-account-switch.mjs` with Playwright installed. PLAYWRIGHT_MODULE and CHROMIUM_PATH support an existing tool installation. The check also covers sign-out across tabs. See [browser sessions](browser-sessions.md).
