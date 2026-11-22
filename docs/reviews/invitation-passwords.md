# Invitation password generation

Invitation credentials used System.Random, which is not intended for secrets. They now use RandomNumberGenerator while preserving the existing twelve-character alphabet and password hashing flow. This is a confirmed implementation weakness; this review did not demonstrate a practical password-prediction attack against the former runtime.

The invitation integration cases verify that the generated password reaches the fake sender and successfully signs in with the requested role. Statistical randomness tests would not prove cryptographic security. Expiring single-use invitations and a required password-change flow are separate work.
