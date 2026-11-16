# Server-confirmed sign-out

Sign-out previously removed browser storage but left issued tokens authorized. The application now calls an authenticated POST endpoint that rotates only the current account's SecurityStamp. This ends that account's sessions on all devices; the button identifies that behavior. Other users are unaffected, and the user can sign in again normally.

The browser clears session state/cache and returns to login only after the server confirms success. On server failure it shows a retryable error. Already-invalid sessions still follow the existing 401 cleanup. The route uses the signed account ID, ignores caller-supplied target IDs, and requires bearer authentication.

Four API cases cover all three roles, existing sessions, other-account isolation, fresh login and anonymous requests. The browser check covers a failed sign-out followed by a successful retry and a different account login. This uses the security-stamp migration from the preceding revocation fix; no additional migration is needed. It does not provide per-device session selection or replace the planned cookie/CSRF transition.
