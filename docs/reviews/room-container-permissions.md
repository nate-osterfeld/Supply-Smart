# Room and container permissions

Staff accounts could create, rename, and delete rooms and containers. These six API operations now require Admin or Manager. Existing clinic ownership checks still apply. Staff retains read access and inventory-item operations.

The API has six role checks in EndpointMapper.Locations.cs, with 403 responses documented. StoragePage hides the associated buttons and modals unless the displayed role is Admin or Manager. Client-side role metadata controls presentation only; signed claims and the API role checks enforce authorization. Program is exposed as a public partial class so API tests can boot the real application. The review branch is excluded from automatic development deployment.

Validation against main b5e9f34913ff09759a31c18396cfdf94f08bb028, November 15, 2026: all 37 integration cases passed after rebasing: six operations across anonymous/Admin/Manager/Staff, twelve cross-clinic cases, and a read-access/location-permission check. Before the original fix, all six Staff-denial cases failed with 200. Tests use real sign-in tokens, SQLite in memory, and fake email. Run `dotnet test SupplySmart.Permissions.Tests`.

The frontend production build passed. Five headless Chromium checks with mocked API responses passed for Staff, Manager, Admin, missing role, and unknown role. They verified room/container controls, permitted dialogs, hierarchy navigation, nested containers, and item visibility. These UI checks complement the integration tests; they do not test real sign-in or server authorization.

Existing cross-clinic create requests return 500 rather than 404; tests verify they cannot change data. Error normalization is separate work. This patch requires no database migration.

Known build warnings: the existing System.Security.Cryptography.Pkcs 9.0.11 dependency has NuGet advisories. No deployed-environment or SQL Server test was performed.
