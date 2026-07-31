# Email Notifications — Customize

## 1. Configure local SMTP for development

1. Run Mailpit (or mailhog):
   ```bash
   docker run -d -p 1025:1025 -p 8025:8025 axllent/mailpit
   ```
2. Edit `src/backend/API/appsettings.Development.json`:
   ```json
   "EmailSettings": {
     "AppName": "nietemplate-local",
     "SmtpHost": "localhost",
     "SmtpPort": 1025,
     "SmtpUsername": "",
     "SmtpPassword": "",
     "SenderEmail": "noreply@local",
     "SenderName": "NIE Template (Local)",
     "EnableSsl": false,
     "BccEmails": []
   }
   ```
3. Open `http://localhost:8025` to view captured emails.

## 2. Configure production SMTP

1. Edit `appsettings.Production.json` (or use env vars):
   ```json
   "EmailSettings": {
     "AppName": "i3g",
     "SmtpHost": "smtp.nie.edu.sg",
     "SmtpPort": 587,
     "SmtpUsername": "",
     "SmtpPassword": "",
     "SenderName": "I3G Notifications",
     "EnableSsl": true,
     "BccEmails": [ "i3g-archive@nie.edu.sg" ]
   }
   ```
2. Leave `SenderEmail` empty — the service derives `i3g@nie.edu.sg` from `AppName`.
3. Coordinate with infra to ensure SPF / DKIM cover the new sender.

## 3. Add a new templated email (e.g. "Approval Request")

1. Create `src/backend/API/Templates/ApprovalRequest.html`:
   ```html
   <html><body>
     <h2>Approval Required: {EntityName}</h2>
     <p>Hello {ApproverName},</p>
     <p>{RequesterName} has submitted "{EntityName}" for your approval.</p>
     <p><a href="{ApprovalUrl}">Open in portal</a></p>
   </body></html>
   ```
2. Mark the file as "Copy to Output Directory: PreserveNewest" in `API.csproj` (or place under a `<Content>` group with `CopyToOutputDirectory`):
   ```xml
   <ItemGroup>
     <Content Include="Templates\**\*.html" CopyToOutputDirectory="PreserveNewest" />
   </ItemGroup>
   ```
3. Call from your service:
   ```csharp
   await _email.SendTemplatedEmailAsync(
       toEmail: approver.Email,
       subject: $"[Approval] {entity.Name}",
       templateFileName: "ApprovalRequest.html",
       placeholders: new Dictionary<string, string>
       {
           ["EntityName"] = entity.Name,
           ["ApproverName"] = approver.FullName,
           ["RequesterName"] = requester.FullName,
           ["ApprovalUrl"] = $"https://staff.app.nie.edu.sg/approvals/{entity.Id}"
       },
       toName: approver.FullName);
   ```
4. Audit-log the send:
   ```csharp
   await _auditLogger.LogEmailSentAsync(approver.Email, $"[Approval] {entity.Name}", "Success");
   ```

## 4. Customize the base template (header / footer)

1. Open `src/backend/API/Templates/BaseTemplate.html`.
2. The placeholders are `{AppName}`, `{Content}`, `{DateTime}`, `{Year}`. They are replaced verbatim. Do not introduce new placeholders without updating `EmailService.BuildBaseTemplateAsync` (lines 149-161 — `placeholders` dictionary).
3. To add a new placeholder (e.g. `{LogoUrl}`), edit `BuildBaseTemplateAsync` and add the entry. Keep token names short and unambiguous.

## 5. Send to multiple recipients in one connection

```csharp
await _email.SendEmailAsync(
    toEmails: approvers.Select(a => a.Email).ToList(),
    subject: "Quarterly review",
    htmlBody: htmlContent);
```

Single SMTP connection, single message with multiple `To` addresses. For per-recipient personalization (different `{ApproverName}` per email), call `SendTemplatedEmailAsync` per recipient — or use a queue.

## 6. Add a retry/queue layer for critical emails

The shipped service does NOT retry on failure (it logs and re-throws). For password reset / payment receipts:

1. Add an entity `EmailQueue` with `ToEmail`, `Subject`, `BodyHtml`, `Attempts`, `Status`, `LastError`, `CreatedOn`.
2. Replace direct `IEmailService.SendEmailAsync` calls with `IEmailQueueService.EnqueueAsync(...)` that just inserts the row.
3. Add a TickerQ job `EmailDispatchJob` that runs every minute, picks up `Status = Pending`, calls `IEmailService.SendEmailAsync`, updates status. Use exponential backoff via `Attempts`.
4. Wire `IAuditLogger.LogEmailSentAsync` from inside the job (not the enqueue site).

## 7. Switch to SES / SendGrid / Mailgun

The cleanest swap is to keep the `IEmailService` interface and write a new implementation:

1. Create `src/backend/Libraries/Services/Services/SesEmailService.cs` implementing `IEmailService`. Use `AWSSDK.SimpleEmailV2` `SendEmailAsync`.
2. In `Program.cs:97-103`, switch the registration based on a config flag:
   ```csharp
   var emailProvider = configuration["EmailSettings:Provider"] ?? "Smtp";
   if (emailProvider == "SES")
       builder.Services.AddScoped<IEmailService, SesEmailService>();
   else
       builder.Services.AddScoped<IEmailService>(sp => new EmailService(...));
   ```
3. Existing call sites are unchanged — they only see `IEmailService`.

## 8. Add CC

Use `SendEmailWithCCAsync(toEmails, ccEmails, subject, htmlBody)`. The current API does not have a `SendTemplatedEmailWithCCAsync`; add one in `IEmailService` + `EmailService` if you need templating + CC together.
