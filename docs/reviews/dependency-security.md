# PKCS dependency security update

The SQL Server dependency graph resolved System.Security.Cryptography.Pkcs 9.0.11. Published advisories [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7) and [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx) identify denial-of-service flaws affecting versions 9.0.0 through 9.0.20; both list 9.0.21 as the first patched 9.x version.

The integrations project explicitly references 9.0.21 so dependent application and test projects resolve the patched version. This preserves the dependency's existing major version. No reachable SupplySmart API exploit against PKCS parsing was demonstrated; this is remediation of a known vulnerable dependency.

Validate with restore, the backend regression suites, and `dotnet list SupplySmart.Host package --vulnerable --include-transitive`. Earlier review notes record the warnings seen before this override.
