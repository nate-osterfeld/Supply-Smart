# Room and container permissions

Staff accounts could create, rename, and delete rooms and containers. These six API operations now require Admin or Manager. Existing clinic ownership checks still apply. Staff retains read access.

The production behavior change is six role checks in EndpointMapper.Locations.cs, with 403 responses documented. Program is exposed as a public partial class only so API tests can boot the real application. The review branch is excluded from automatic development deployment.

Validation: before the fix, all six Staff-denial cases failed (the server returned 200). After the fix, all 37 integration cases passed: six operations across anonymous/Admin/Manager/Staff, twelve cross-clinic cases, and a read-access/location-permission check. Tests use real sign-in tokens, SQLite in memory, and fake email. Run `dotnet test SupplySmart.Permissions.Tests`.

This patch has no database migration, token format change, login change, email feature rewrite, billing change, or frontend changes. Staff may still see editing controls; the API now rejects their requests. UI visibility is a separate follow-up. Existing cross-clinic create requests return 500 rather than 404; tests verify they cannot change data. Error normalization is separate work.

Known build warnings: the existing System.Security.Cryptography.Pkcs 9.0.11 dependency has NuGet advisories. No deployed-environment or SQL Server test was performed.
