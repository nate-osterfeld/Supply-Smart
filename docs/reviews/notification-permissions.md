# Test notification permissions

Staff and Manager accounts could invoke the test-mail API directly, including arbitrary additional recipients and message content, even though notification editing is restricted to Admins in the settings UI and update handler.

The test-mail endpoint now requires the Admin role before dispatch, matching the existing template-edit policy. Five integration cases cover anonymous requests, all three roles, and another organization's Admin. Rejected requests send no email or create templates; permitted recipients come from the signed-in organization even when a different organizationId is supplied in the URL. Both non-Admin cases failed before the fix and all five pass afterward. Tests capture email through a fake sender.

Admins retain the existing custom-recipient feature. Delivery throttling and recipient policy are separate controls.
