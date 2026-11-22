# Invitation HTML injection

The invitation email interpolated the inviter-supplied full name directly into HTML. An authenticated administrator able to invite users could insert a deceptive link into an email delivered by SupplySmart. This is email content injection; no JavaScript execution or application-origin XSS was demonstrated.

Encode the name, email address, password and configured login URL at the HTML boundary. Recipient addressing still uses the original email. Encoding also preserves literal ampersands and other special characters in credentials.

Two tests capture the real SMTP message on loopback, with no external delivery. Both failed before the fix: a crafted name became an actual link, and special characters were interpreted as HTML. The tests assert escaped content and retain the legitimate login link and password markup.
