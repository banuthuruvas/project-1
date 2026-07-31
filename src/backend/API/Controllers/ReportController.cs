using API.Authorization;
using Domain.Dto.Report;
using Domain.Security;
using Domain.Services.Reports;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : BaseController
{
    private readonly IPdfGenerationService _pdfService;

    public ReportController(IPdfGenerationService pdfService)
    {
        _pdfService = pdfService;
    }

    /// <summary>
    /// Get all available report types grouped by category.
    /// </summary>
    [HttpGet("types")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ReportRead)]
    public IActionResult GetReportTypes()
    {
        var types = _pdfService.GetAvailableReportTypes();
        return Ok(types);
    }

    /// <summary>
    /// Generate HTML preview for a report.
    /// </summary>
    [HttpPost("preview")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ReportRead)]
    public async Task<IActionResult> PreviewReport([FromBody] ReportRequestDto request)
    {
        var html = await _pdfService.GenerateReportHtmlAsync(request.ReportType, request);
        return Content(html, "text/html");
    }

    /// <summary>
    /// Generate and download a PDF report.
    /// </summary>
    [HttpPost("download")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ReportRead)]
    public async Task<IActionResult> DownloadReport([FromBody] ReportRequestDto request)
    {
        try
        {
            var html = await _pdfService.GenerateReportHtmlAsync(request.ReportType, request);
            var pdf = await _pdfService.GeneratePdfFromHtmlAsync(html);
            return File(pdf, "application/pdf", $"{request.ReportType}_{DateTime.Now:yyyyMMdd}.pdf");
        }
        catch
        {
            // Fallback: return HTML if PDF generation fails
            var html = await _pdfService.GenerateReportHtmlAsync(request.ReportType, request);
            return Content(html, "text/html");
        }
    }
}
