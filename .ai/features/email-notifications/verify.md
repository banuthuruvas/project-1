# Email Notifications — Verify

## Backend

```bash
dotnet build src/backend/NieTemplate.sln
dotnet run --project src/backend/API
```

## Local SMTP via Mailpit

```bash
# Start Mailpit
docker run -d --name mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit

# Confirm appsettings.Development.json points to localhost:1025
grep -A 2 "EmailSettings" src/backend/API/appsettings.Development.json
```

## Trigger a send programmatically

The cleanest way is a small temporary endpoint or a scratch controller; for verification we'll lean on whatever caller already exists in your project (e.g. password reset, approval). For the template repo, the procurement sample's approval flow triggers an email when `ProcessApproval` is called.

```bash
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"devia"}' | jq -r .sessionToken)

curl -s -X POST http://localhost:5002/api/AccessControl/AssignRole \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"userId":"devia","roleId":1}'

# (Replace with whatever your project uses to trigger an email — e.g. submit a PO)
curl -s -X POST http://localhost:5002/api/PurchaseOrder/Submit \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"id":1}' | jq

# Open Mailpit
open http://localhost:8025
# Expect: a captured email with the configured AppName-derived sender
```

## Without a trigger — direct service smoke

Add a temporary minimal endpoint:

```csharp
app.MapGet("/dev/test-email", async (IEmailService email) =>
{
    await email.SendBaseTemplatedEmailAsync(
        toEmail: "test@local",
        subject: "Smoke test",
        contentHtml: "<p>Hello from the smoke test.</p>",
        toName: "Smoke Tester");
    return Results.Ok();
}).RequireHost("localhost"); // dev only
```

```bash
curl -s http://localhost:5002/dev/test-email -H "X-Session-Id: $SESSION"
# Open Mailpit → confirm the email with NIE-branded BaseTemplate shell.
```

Remove the endpoint before merging.

## Audit row

```sql
SELECT "Action", "EntityId", "Outcome", "AdditionalData"
FROM "AuditLogs"
WHERE "Action" = 53      -- EAuditAction.EmailSent
ORDER BY "Timestamp" DESC LIMIT 5;
-- Expect: rows with EntityId = recipient email and AdditionalData JSON containing { recipient, subject }
```

## SMTP failure path

```bash
# Stop Mailpit to simulate a dead SMTP
docker stop mailpit

# Trigger another send. The API logs should contain:
#   "Failed to send email to ... — Subject: ..."
# and the call should bubble up an exception (caller chooses how to handle).

# Restart Mailpit
docker start mailpit
```

## STARTTLS check (production-mode setup)

```bash
# Use openssl to confirm the SMTP host serves STARTTLS
openssl s_client -connect smtp.nie.edu.sg:587 -starttls smtp -servername smtp.nie.edu.sg
# Expect: a successful TLS handshake. If failure, EmailSettings.EnableSsl=true would still fail.
```

## Sender derivation

```bash
# With AppName="i3g" and SenderEmail="" — confirm the service derives "i3g@nie.edu.sg"
# Send a smoke email and inspect the From header in Mailpit:
# From: <SenderName> <i3g@nie.edu.sg>
```

## Bcc archive

```bash
# Add an entry to BccEmails in appsettings.Development.json:
# "BccEmails": [ "archive@local" ]
# Send a smoke email. In Mailpit, both the original recipient and archive@local should receive it.
```
