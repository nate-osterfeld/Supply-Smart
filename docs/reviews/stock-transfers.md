# Stock transfer integrity

A Staff request transferring six units into an existing stock row at int.MaxValue minus five returned 200 and persisted a destination quantity of -2,147,483,648. The source lost six units at the same time. This is authenticated data corruption within the user's organization.

The service now checks the destination sum using a wider integer before modifying either tracked row. A sum above the supported stock limit returns 400 with a readable error; quantities and audit history remain unchanged. A transfer exactly reaching int.MaxValue still succeeds.

Three isolated API cases cover overflow rejection, the exact boundary, and a foreign-clinic destination. The overflow case failed before the fix; all three pass afterward. Tests use SQLite and fake email. Run `dotnet test SupplySmart.Inventory.Tests`.
