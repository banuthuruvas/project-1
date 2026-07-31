using Domain.Dto.Report;

namespace Domain.Services.Reports;

/// <summary>
/// Service for generating reports from HTML templates.
/// Uses Playwright for HTML-to-PDF conversion.
/// </summary>
public interface IPdfGenerationService
{
    /// <summary>
    /// Generate a PDF from an HTML string.
    /// </summary>
    Task<byte[]> GeneratePdfFromHtmlAsync(string html, PdfOptions? options = null);

    /// <summary>
    /// Get the available report types for the system.
    /// </summary>
    List<ReportTypeDefinition> GetAvailableReportTypes();

    /// <summary>
    /// Generate HTML content for a specific report type with filters.
    /// </summary>
    Task<string> GenerateReportHtmlAsync(string reportType, ReportRequestDto filters);
}

public class PdfOptions
{
    public string Format { get; set; } = "A4";
    public bool Landscape { get; set; } = false;
    public string? HeaderTemplate { get; set; }
    public string? FooterTemplate { get; set; }
    public bool PrintBackground { get; set; } = true;
    public string? Margin { get; set; } = "10mm";
}

public class ReportTypeDefinition
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Icon { get; set; } = "📄";
    public List<ReportFilter> Filters { get; set; } = new();
}

public class ReportFilter
{
    public string Name { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Type { get; set; } = "dropdown"; // dropdown, daterange, text
    public List<string>? Options { get; set; }
}
