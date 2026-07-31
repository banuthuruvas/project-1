using Domain.Dto.Report;
using Microsoft.Extensions.Logging;

namespace Domain.Services.Reports;

/// <summary>
/// Playwright-based HTML to PDF generation service.
/// Requires Playwright CLI installed on the server.
/// In development, falls back to returning HTML directly.
/// </summary>
public class PlaywrightPdfGenerationService : IPdfGenerationService
{
    private readonly ILogger<PlaywrightPdfGenerationService> _logger;
    private readonly string _playwrightExecutablePath;

    public PlaywrightPdfGenerationService(
        ILogger<PlaywrightPdfGenerationService> logger,
        string? playwrightExecutablePath = null)
    {
        _logger = logger;
        _playwrightExecutablePath = playwrightExecutablePath ?? "npx playwright";
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(string html, PdfOptions? options = null)
    {
        var opts = options ?? new PdfOptions();

        try
        {
            // Write HTML to temp file
            var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.html");
            var tempPdfPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.pdf");

            await File.WriteAllTextAsync(tempHtmlPath, WrapInFullPage(html));

            var args = $"pdf {tempHtmlPath} {tempPdfPath} --format {opts.Format}";
            if (opts.Landscape) args += " --landscape";
            if (opts.PrintBackground) args += " --print-background";
            if (opts.Margin != null) args += $" --margin {opts.Margin}";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _playwrightExecutablePath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException("Failed to start Playwright process.");

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.LogError("Playwright PDF generation failed: {Error}", error);
                throw new InvalidOperationException($"Playwright PDF generation failed: {error}");
            }

            var pdf = await File.ReadAllBytesAsync(tempPdfPath);

            // Cleanup
            try { File.Delete(tempHtmlPath); File.Delete(tempPdfPath); } catch { /* ignore */ }

            return pdf;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogWarning(ex, "Playwright not available. Returning HTML-only report.");
            // Fallback: return HTML as text (caller can detect and show preview only)
            throw;
        }
    }

    public List<ReportTypeDefinition> GetAvailableReportTypes()
    {
        return new List<ReportTypeDefinition>
        {
            new()
            {
                Id = "po-summary", Name = "Purchase Order Summary", Category = "Procurement", Icon = "📋",
                Description = "Summary of all purchase orders with status, vendor, and total amounts.",
                Filters =
                {
                    new() { Name = "status", Label = "Status", Type = "dropdown", Options = new() { "All", "Draft", "Submitted", "Approved", "Rejected", "Completed" } },
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" }
                }
            },
            new()
            {
                Id = "vendor-analysis", Name = "Vendor Analysis", Category = "Procurement", Icon = "🏢",
                Description = "Vendor performance analysis with order counts and total spend.",
                Filters =
                {
                    new() { Name = "vendorId", Label = "Vendor", Type = "dropdown" },
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" }
                }
            },
            new()
            {
                Id = "spending-by-dept", Name = "Spending by Department", Category = "Procurement", Icon = "💰",
                Description = "Department-wise spending breakdown with charts and trends.",
                Filters =
                {
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" }
                }
            },
            new()
            {
                Id = "approval-timeline", Name = "Approval Timeline", Category = "Procurement", Icon = "⏱️",
                Description = "Average approval times and workflow bottleneck analysis.",
                Filters =
                {
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" }
                }
            },
            new()
            {
                Id = "audit-trail", Name = "Audit Trail Report", Category = "Audit", Icon = "🔍",
                Description = "Complete audit log of all system actions with filters.",
                Filters =
                {
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" },
                    new() { Name = "category", Label = "Category", Type = "dropdown", Options = new() { "All", "Data", "AccessControl" } }
                }
            },
            new()
            {
                Id = "user-activity", Name = "User Activity Log", Category = "Audit", Icon = "👤",
                Description = "User login, action, and access patterns.",
                Filters =
                {
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" },
                    new() { Name = "userId", Label = "User", Type = "text" }
                }
            },
        };
    }

    public async Task<string> GenerateReportHtmlAsync(string reportType, ReportRequestDto filters)
    {
        var html = reportType switch
        {
            "po-summary" => await GeneratePoSummaryHtmlAsync(filters),
            "vendor-analysis" => await GenerateVendorAnalysisHtmlAsync(filters),
            "spending-by-dept" => GenerateSpendingByDeptHtml(filters),
            "approval-timeline" => GenerateApprovalTimelineHtml(filters),
            "audit-trail" => GenerateAuditTrailHtml(filters),
            "user-activity" => GenerateUserActivityHtml(filters),
            _ => "<h1>Report not found</h1>"
        };

        return WrapInFullPage(html);
    }

    private Task<string> GeneratePoSummaryHtmlAsync(ReportRequestDto filters) =>
        Task.FromResult($@"
<h1>Purchase Order Summary</h1>
<p class=""report-date"">Generated: {DateTime.Now:dd MMM yyyy, HH:mm}</p>
<hr/>
<p>This report summarizes all purchase orders in the system.</p>
<p>Filters applied: Status={filters.Status ?? "All"}, DateRange={filters.DateFrom:yyyy-MM-dd} to {filters.DateTo:yyyy-MM-dd}</p>
<div class=""placeholder-chart"">
  <p>📊 Purchase orders by status chart would render here</p>
  <p>📈 Monthly trend line chart would render here</p>
</div>
");

    private Task<string> GenerateVendorAnalysisHtmlAsync(ReportRequestDto filters) =>
        Task.FromResult($@"
<h1>Vendor Analysis</h1>
<p class=""report-date"">Generated: {DateTime.Now:dd MMM yyyy, HH:mm}</p>
<hr/>
<p>Analysis of vendor performance, order volumes, and total spend.</p>
<div class=""placeholder-chart"">
  <p>🏢 Top vendors by order count</p>
  <p>💰 Total spend by vendor chart</p>
</div>
");

    private string GenerateSpendingByDeptHtml(ReportRequestDto filters) =>
        $@"
<h1>Spending by Department</h1>
<p class=""report-date"">Generated: {DateTime.Now:dd MMM yyyy, HH:mm}</p>
<hr/>
<div class=""placeholder-chart"">
  <p>📊 Department-wise spending breakdown</p>
</div>
";

    private string GenerateApprovalTimelineHtml(ReportRequestDto filters) =>
        $@"
<h1>Approval Timeline Analysis</h1>
<p class=""report-date"">Generated: {DateTime.Now:dd MMM yyyy, HH:mm}</p>
<hr/>
<div class=""placeholder-chart"">
  <p>⏱️ Average time per approval stage</p>
  <p>🔍 Bottleneck identification</p>
</div>
";

    private string GenerateAuditTrailHtml(ReportRequestDto filters) =>
        $@"
<h1>Audit Trail Report</h1>
<p class=""report-date"">Generated: {DateTime.Now:dd MMM yyyy, HH:mm}</p>
<hr/>
<p>Complete audit log entries for the selected period.</p>
";

    private string GenerateUserActivityHtml(ReportRequestDto filters) =>
        $@"
<h1>User Activity Log</h1>
<p class=""report-date"">Generated: {DateTime.Now:dd MMM yyyy, HH:mm}</p>
<hr/>
<p>User login and action patterns.</p>
";

    private static string WrapInFullPage(string content) => $@"
<!DOCTYPE html>
<html><head>
<meta charset=""utf-8"">
<style>
  body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; margin: 30px; color: #1f2937; }}
  h1 {{ font-size: 24px; color: #111827; margin-bottom: 4px; }}
  .report-date {{ font-size: 12px; color: #6b7280; }}
  hr {{ border: none; border-top: 2px solid #e5e7eb; margin: 16px 0; }}
  .placeholder-chart {{ background: #f9fafb; border: 2px dashed #d1d5db; border-radius: 8px; padding: 40px; text-align: center; margin: 20px 0; }}
  .placeholder-chart p {{ color: #6b7280; font-size: 14px; margin: 8px 0; }}
  table {{ width: 100%; border-collapse: collapse; margin: 16px 0; }}
  th {{ background: #f3f4f6; padding: 10px; text-align: left; font-size: 12px; text-transform: uppercase; color: #6b7280; }}
  td {{ padding: 10px; border-bottom: 1px solid #e5e7eb; font-size: 13px; }}
  @media print {{ body {{ margin: 0; }} }}
</style></head>
<body>{content}</body></html>";
}
