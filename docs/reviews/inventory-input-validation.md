# Inventory input validation

Authenticated inventory editors, including Staff, could submit negative quantities, thresholds and prices or undefined item types directly to the API. Negative stock can corrupt inventory and transaction interpretation; invalid thresholds can suppress stock warnings. Prices outside the SQL Server decimal(18,2) range also reached persistence unchecked.

Create and update now reject negative quantities/thresholds, negative or out-of-range prices, and undefined item types with HTTP 400 before writes, history or notifications. Full and partial transfers reject negative destination thresholds. Zero values, both supported item types and the full nonnegative integer quantity range remain valid.

Sixteen API cases use real Staff sign-in and isolated SQLite. Before the fix, all twelve invalid cases returned 200; four valid boundary cases passed. Rejection tests verify unchanged stock, container assignment, audit/transaction history and fake email delivery. SQLite does not enforce SQL Server decimal precision, so the upper price bound is based on the declared production column type. No live database or existing-data cleanup was performed.
