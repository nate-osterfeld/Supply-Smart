# Dashboard denial of service through stock quantities

A Staff user could update two items to large, valid nonnegative quantities and make dashboard metrics return HTTP 500 for the clinic's Admin and other users. The expiring-soon and low-stock totals each summed quantities as 32-bit integers. The failure persists until inventory is corrected. Other organizations remain scoped out.

Sum those two indicators as 64-bit values and widen their DTO fields to match. Stored stock quantities and JSON field names stay the same; the API schema now describes those response counts as int64. The web client's JavaScript number fields require no change.

Three API tests exercise Staff updates followed by a different user's dashboard request. Two separate maximum-quantity cases returned 500 before the fix, while the ordinary-quantity control passed. Expected totals are 4,294,967,294 units for large values and 30 for ordinary values. Each case also checks that another clinic's dashboard stays empty. This addresses quantity counters, not extreme decimal inventory-valuation totals, which remain a separate review candidate.
