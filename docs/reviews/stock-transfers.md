# Stock transfer integrity

A Staff request transferring six units into an existing stock row at int.MaxValue minus five returned 200 and persisted a destination quantity of -2,147,483,648. The source lost six units at the same time. This is authenticated data corruption within the user's organization.

The service now checks the destination sum using a wider integer before modifying either tracked row. A sum above the supported stock limit returns 400 with a readable error; quantities and audit history remain unchanged. A transfer exactly reaching int.MaxValue still succeeds.

Three isolated API cases cover overflow rejection, the exact boundary, and a foreign-clinic destination. The overflow case failed before the fix; all three pass afterward. Tests use SQLite and fake email. Run `dotnet test SupplySmart.Inventory.Tests`.

Partial transfers also bypassed the existing item quota: a workspace at its two-row limit could create a third row by splitting stock, using an ordinary Staff request. Both adding an item and splitting into a new row now call the same quota check. The check runs before modifying the source. A full relocation or merge into an existing row still works at quota because neither creates a row.

Four API cases cover rejection at quota, a permitted split below quota, a merge at quota, and a full relocation at quota. The first failed before the fix; all four pass afterward, with stock totals preserved. This closes the sequential split-path bypass. Concurrent count-then-insert quota races across multiple requests remain separate work.
