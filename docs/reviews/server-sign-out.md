# Server-confirmed sign-out

Sign-out previously removed browser storage but left issued tokens authorized. The application now calls an authenticated POST endpoint that rotates only the current account's SecurityStamp. This ends that account's sessions on all devices; the button identifies that behavior. Other users are unaffected, and the user can sign in again normally.

The browser clears session state and cached data after the server confirms success. On server failure it shows a retryable error. The route uses the signed account ID. Browser sessions require origin and CSRF checks. API clients can authenticate with a bearer token.

Four API cases cover all three roles, existing sessions, other-account isolation, fresh login and anonymous requests. Browser checks cover a failed sign-out followed by a successful retry and a different account login. Sign-out uses the security-stamp migration and expires the browser cookie. See [browser sessions](browser-sessions.md).
