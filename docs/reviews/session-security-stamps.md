# Invalidate sessions after security-sensitive account changes

Old JWTs remained valid after password replacement. A role or organization changed and then restored also reactivated an old token, because the previous guard compared only the account's current permissions with its claims.

Each user now has a random SecurityStamp. Login and registration include it in the signed token; authentication checks it against the database alongside the existing account/role/organization checks. Tracked changes to password hash, email, role or organization rotate the stamp on both synchronous and asynchronous SaveChanges. Display-name-only changes do not. Changes through raw SQL or ExecuteUpdate must also update the stamp explicitly because those operations bypass SaveChanges.

Deployment requires the AddUserSecurityStamp migration before starting the new app. It adds one non-null uniqueidentifier column with NEWID() backfill/default; no existing rows are deleted. Older tokens without the claim are rejected, so existing users must sign in again once. No live migration is run by this review. This is account-level revocation, not a per-device session store, and it does not cancel a request that already passed authentication.

Tests reproduce password replacement and role restoration bypasses with actual login tokens, exercise synchronous/asynchronous writes, and preserve ordinary display-name updates. The SQL Server model/migration can be checked without connecting to a database. Cookie transport and CSRF remain separate work.
