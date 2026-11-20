# Billing verification authorization

Checkout and billing-portal creation already require Admin, but POST `/api/settings/subscription/verify-session` only required authentication. It calls a service that changes the organization's subscription and quotas. Staff and Managers could reach that mutation path if they obtained a matching checkout session ID.

Apply the same Admin requirement at the verification route. Organization scoping still comes from the signed token. This does not validate the age or current subscription status of a checkout session; that remains a separate billing-lifecycle concern.

Five API cases exercise anonymous, Admin, Staff, Manager and another organization's Admin using actual login tokens and a recording billing service. Before the fix, Staff and Manager both received 200. The patched route must return 403 without calling the service; anonymous requests return 401, while both Admins can verify only for their own organization even with an injected organization query parameter. No Stripe requests are made by this test.
