# Repeated login guesses

The application accepted repeated password guesses without a limit. Add a ten-attempt, one-minute window per normalized email, before credential hashing. Count known and unknown addresses alike, share the limit across clients and case/whitespace variants, and ignore caller-supplied forwarding headers. Rejections return 429 with Retry-After and a readable message.

The process tracks up to 10,000 addresses individually. Additional addresses share 1,024 fixed counters selected by a case-insensitive hash. This bounds memory, keeps established limits intact, and allows new accounts to sign in while the table is full. Addresses sharing a counter also share the temporary limit. Tests cover concurrent requests, window expiry and sign-in at capacity.

This is a per-application-process mitigation. Restarts reset counters and multiple replicas each have a counter; a shared rate-limit store or trusted gateway is still needed for a deployment-wide limit. It does not address password spraying across many accounts or public-registration abuse. Someone can consume another account's allowance for a minute; no persistent account lockout is created. Gateway settings were not inspected.
