# Authentication request validation

The minimal API did not run DTO data annotations. A null login password returned 500 for an existing account and 401 for an unknown email, creating a reliable account-existence oracle. Registration accepted empty/short passwords and malformed fields; some invalid requests failed only after creating tenant rows.

Validate both DTOs in AuthService before lookups, hashing or writes, and return 400 for invalid requests. New registration passwords require 12–1024 characters without composition rules; existing shorter passwords remain usable at login. Names/email respect database length limits. The form follows the registration minimum and maximum.

Eighteen tests cover invalid login/registration, unchanged tenant/account data, valid passphrases and existing short-password compatibility. Seventeen failed before the patch. This closes the null-password status oracle; it does not eliminate enumeration through the registration duplicate-email message or login timing. Validation does not replace transaction handling for later registration failures.
