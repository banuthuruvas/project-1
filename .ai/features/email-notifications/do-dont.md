# Email Notifications — Do and Don't

## DO ✅

1. **DO** call `SendBaseTemplatedEmailAsync(toEmail, subject, contentHtml)` for ad-hoc notifications. The base template adds the NIE-branded header / footer / date / year so emails are visually consistent across services.
2. **DO** create a per-event template under `src/backend/API/Templates/` (e.g. `ApprovalRequest.html`) when the email layout is non-trivial. Use `{Placeholder}` tokens and pass them through the `placeholders` dictionary to `SendTemplatedEmailAsync`.
3. **DO** call `IAuditLogger.LogEmailSentAsync(recipient, subject, "Success" | "Failed")` after the send completes. The audit row carries the recipient + subject for compliance.
4. **DO** populate `EmailSettings.AppName` with a short identifier (`"i3g"`, `"safeguard"`, `"myportal"`). The service auto-derives `SenderEmail = "<AppName>@nie.edu.sg"` if `SenderEmail` is left blank — this is the default and recommended.
5. **DO** use `EnableSsl = true` (STARTTLS) in production. Plain SMTP is fine for dev (Mailpit at `localhost:1025`) but never acceptable for prod.
6. **DO** populate `BccEmails` with an audit / archive mailbox in production. The service appends each entry as a BCC on every outgoing message — useful for compliance records.
7. **DO** wrap email sends in a TickerQ job for retry-critical workflows (e.g. password reset, payment confirmation). The shipped `EmailService` does NOT retry on failure.
8. **DO** validate recipient strings before calling — `SendCoreAsync` filters empty entries (`.Where(e => !string.IsNullOrWhiteSpace(e))`) and skips when no valid `To` remains, but checking earlier surfaces bugs faster.
9. **DO** keep templates pure HTML + `{Placeholder}` tokens. Razor-style logic does not work; the replacement is a literal string `Replace`.
10. **DO** test against Mailpit / mailhog locally — `SmtpHost = "localhost", SmtpPort = 1025` reaches a fake SMTP server that lets you inspect the message in a browser at `http://localhost:8025`.

## DON'T ❌

1. **DON'T** call `SendEmailAsync(rawHtml)` directly from random services unless the body is a single self-contained HTML page. The base template gives you the brand surface — skipping it produces orphaned-looking emails.
2. **DON'T** put runtime data into the template file itself (e.g. hardcoding "Dear {{User.Name}}"). The placeholder-replace is a flat string substitution; embedding logic forces the template to know your data shape.
3. **DON'T** include attachments via the current API — the service does not expose them. Add an overload with `IEnumerable<MimeEntity>` if you need it; do NOT inline files as base64 in HTML.
4. **DON'T** chain `SendCoreAsync` calls in a tight loop. Each call opens and closes a fresh SMTP connection. For batch sends, accept a `List<string>` (the service supports this) so MailKit reuses the connection.
5. **DON'T** log full HTML bodies. They contain user data and increase log volume. Log recipient + subject + outcome only.
6. **DON'T** swallow MailKit `SmtpProtocolException` — it indicates server-side rejection (bad credentials, blocked sender). Let it propagate so the caller can decide on retry.
7. **DON'T** assume `EmailSettings.SmtpUsername` is non-empty. The service's `if (!string.IsNullOrEmpty(_settings.SmtpUsername) && !string.IsNullOrEmpty(_settings.SmtpPassword))` check skips authentication for anonymous SMTP relays — useful for internal-only relay servers.
8. **DON'T** set `SenderEmail` to a personal address. Use the AppName-derived inbox or a project shared mailbox — recipients reply to the sender.
9. **DON'T** put SMTP credentials in `appsettings.json` for production. Use ASP.NET secrets, environment variables (`EmailSettings__SmtpPassword`), or a secret store.
10. **DON'T** assume the recipient inbox accepts your sender domain. NIE outbound mail typically requires SPF + DKIM alignment; coordinate with infra before going live with a new sender.
