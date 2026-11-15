# Explicit invitation roles

Malformed or omitted invitation roles silently granted Manager. Enum parsing also accepted numeric roles (including "0" as Admin), undefined numbers, and comma-separated names. The inviter already needed Admin permission; this was unsafe privilege assignment from invalid input, not an anonymous escalation.

Invitations now require an explicit Admin, Manager, or Staff name. Matching is case-insensitive and trims surrounding spaces. Missing, null, misspelled, numeric, undefined, and combined roles return 400 before creating an account, audit entry, or email.

Twelve integration cases cover seven rejected inputs and five valid names/case/spacing variants. The seven rejection cases failed before the fix. Accepted invitations also verify that the emailed temporary password signs in with the intended role. The current UI already submits an explicit named role.
