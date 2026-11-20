# Conflicting stock writes

Two requests could both read quantity 10 and save quantity 9, leaving stock at 9 while recording two checkouts. A move or delete could also overwrite a quantity change that arrived after its initial read.

Quantity is now an EF concurrency token. UPDATE and DELETE statements include the originally read quantity in their condition; if another request changed it, SaveChanges fails and its transaction rolls back stock, audit and checkout-history writes. The three affected APIs return 409 with a reload message. The handler does not automatically retry a stale absolute-quantity edit.

An update explicitly writes Quantity even if its input matches the tracked snapshot. Otherwise EF could skip the item write, acknowledge stale data, and save only a misleading audit entry. A regression reproduces that edge case; an unchanged request without a competing write still succeeds and creates no checkout history.

The existing Quantity column is reused; no new column or DDL migration is needed. The model snapshot records the concurrency metadata. A model-only SQL Server check verifies that runtime metadata matches the snapshot without opening a database connection.

Four regression cases failed before the fix: two services using the same old snapshot, and update/move/delete API requests with a deterministic competing database write. After the fix, the losing request produces no audit/history entries or email and does not partly move stock. These are controlled interleavings on SQLite, not SQL Server load tests. The full inventory suite also covers normal transfer boundaries and quotas.

This detects quantity changes between a request's database read and save. It does not detect every stale browser form, metadata-only edits, or concurrent item-quota count/insert races. A browser-visible version and stronger quota transaction design remain separate work.
