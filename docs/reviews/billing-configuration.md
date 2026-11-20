# Paid plans require configured billing

The checkout service applied paid plans and their quotas locally whenever the Stripe secret or selected price ID was missing. An administrator could therefore obtain paid entitlements without payment in a misconfigured installation. Production configuration was not inspected; this is a confirmed conditional bypass, not a claim that the deployed service is misconfigured.

Reject paid-plan requests when either setting is absent, before modifying the organization or contacting Stripe. The existing endpoint returns HTTP 400 for this configuration error. Free remains available without Stripe. Local demo installations now need Stripe test configuration to exercise paid tiers.

Six regression cases cover all three paid tiers with a missing secret or missing price, asserting the plan, quotas, customer ID and audit history are unchanged. All six failed before the fix. A seventh case preserves the Free-plan behavior. No live billing API or customer account was used.
