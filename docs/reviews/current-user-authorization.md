# Current-user authorization

Deleting an administrator already blocked fresh sign-in, but its existing seven-day JWT still authorized reads and new invitations. A signed JWT also retained its original role and organization after the account changed.

After normal signature/expiry validation, authentication now checks the current database account's existence, organization, and role. A mismatch fails authentication with 401 before an endpoint executes. Unchanged users keep access, and a fresh login after a role/organization change uses the new permissions. The check adds one indexed account lookup per authenticated request and deliberately does not cache revocation state. A request already authorized before a concurrent deletion can still finish.

Six integration cases cover deleted-admin reads/invitations, fresh-login rejection, demotion, tenant reassignment, and unchanged Admin/Manager/Staff accounts. Three stale-token cases failed before the fix; all six pass afterward. Tests use real sign-in tokens, SQLite and fake email.

This initial account lookup did not require a token or schema change. The branch now also includes [security stamps](session-security-stamps.md) and [server-confirmed sign-out](server-sign-out.md), which handle password/security changes and logout. The security-stamp migration must be applied before deploying the current branch, and existing sessions must sign in again. The HttpOnly-cookie/CSRF transition remains separate work.
