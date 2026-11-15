# Forecast arithmetic

Main b5e9f34913ff09759a31c18396cfdf94f08bb028 includes PR #2. Its forecast sums and response fields use 32-bit integers. Two accepted checkouts of 1.5 billion units overflow the historical sum and make every forecast horizon return 500 for that organization. A separate example with demand 350 million and threshold 2 billion wraps the recommended order to zero.

The fix promotes historical totals before absolute-value conversion and uses 64-bit fields for demand, projected stock, recommendations, and summary demand. A decimal intermediate calculates whole-unit demand without floating-point rounding errors (31 units over a 30-day horizon stays 31). Existing stored quantities and database schema are unchanged. JSON field names and numeric value types remain compatible with the frontend's number fields; consumers generated from the OpenAPI schema should accept int64 for the four widened fields. JavaScript number precision above 2^53 is a separate client constraint.

Validation, November 15, 2026:

- Ten SQLite calculation cases: seven failed before the fix, all ten pass after it. Coverage includes repeated large checkouts, all three horizons, a recommendation above the int limit, combined demand across items, int.MinValue conversion, ordinary rounding, old history, and restocks.
- Three isolated API regression cases pass with the same source patch on b5e9f34. Accepted Staff stock edits now leave 7/14/30-day forecasts at 200 with correct totals; the large-threshold order is 2,350,000,000; anonymous access and cross-clinic isolation still reject the tested requests.
- Run `dotnet test SupplySmart.Forecast.Tests`. The API replay uses real sign-in tokens, SQLite, and a fake email sender in the isolated review harness. It also compiles the application and endpoint consumers of the widened DTOs.

The review branch is excluded from automatic development deployment. The concurrent-update/history inconsistency needs a separate fix. Existing NuGet advisories remain; SQL Server and deployment behavior were not tested.
