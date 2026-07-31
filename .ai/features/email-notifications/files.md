# Email Notifications — File Map

## Owned files

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/backend/Libraries/Shared/Interfaces/IEmailService.cs` | Interface | The send contract: `SendEmailAsync(toEmail, subject, htmlBody, toName?)`, `SendEmailAsync(List<toEmails>, ...)`, `SendEmailWithCCAsync(...)`, `SendTemplatedEmailAsync(...)` (single+list), `SendBaseTemplatedEmailAsync(...)` (single+list) |
| `src/backend/Libraries/Services/Services/EmailService.cs` | Service | MailKit + MimeKit implementation. Connects to `SmtpHost:SmtpPort` with `SecureSocketOptions.StartTls` when `EnableSsl = true`, authenticates if username/password present, builds the `MimeMessage`, attaches BCC from settings, sends, disconnects, logs success/failure |
| `src/backend/Libraries/Shared/Models/EmailSettings.cs` | Options POCO | Strongly-typed `EmailSettings` config block with `AppName`, `SmtpHost`, `SmtpPort` (default 25), `SmtpUsername`, `SmtpPassword`, `SenderEmail` (auto-derived from `AppName` if blank), `SenderName`, `EnableSsl`, `BccEmails` |
| `src/backend/API/Templates/BaseTemplate.html` | Template | The NIE-branded HTML shell. Placeholders `{AppName}`, `{Content}`, `{DateTime}`, `{Year}`. Used by every `SendBaseTemplatedEmailAsync` call |

## Touched files

| Path | What it contains | Why must be touched |
| --- | --- | --- |
| `src/backend/API/Program.cs` lines 97-103 | `builder.Services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"))` + factory registration of `IEmailService` that captures `builder.Environment.ContentRootPath` so the service can locate `Templates/` folder | DI wiring; remove if the feature is deleted |
| `src/backend/API/appsettings.json` | `"EmailSettings": { "AppName": ..., "SmtpHost": ..., "SmtpPort": ..., ... }` | Required for the service to know where to send |
| `src/backend/API/appsettings.Development.json` | Local SMTP override (e.g. Mailpit at `localhost:1025`) | Dev convenience |
| Project-specific templates `src/backend/API/Templates/<Name>.html` | New templates that the project's services use | Each template MUST live under `Templates/` so `LoadAndReplaceTemplateAsync` can find it via `Path.Combine(ContentRootPath, "Templates", templateFileName)` |
| Caller services (e.g. `PurchaseOrderService` for approval emails) | Inject `IEmailService` and call `SendBaseTemplatedEmailAsync` after the workflow event | Call sites are scattered; document each new caller in its feature's dossier |
| `src/backend/Libraries/Services/Services/AuditLog/AuditLogger.cs` `LogEmailSentAsync` | The recommended audit hook to call after a successful send | Adds `EAuditCategory.System` row with recipient + subject |

## Migrations

None — emails are fire-and-forget; nothing is persisted to the database by this feature. If you add an `EmailQueue` entity for retries (see customize), that's a new feature and gets its own migration.

## External dependencies

| Package | Purpose |
| --- | --- |
| `MailKit` | SMTP client (`SmtpClient`, `SecureSocketOptions`) |
| `MimeKit` | Email construction (`MimeMessage`, `MailboxAddress`, `BodyBuilder`) |
| `Microsoft.Extensions.Options` | Strongly-typed `IOptions<EmailSettings>` |
| `Microsoft.Extensions.Logging` | Send success/failure logging |
