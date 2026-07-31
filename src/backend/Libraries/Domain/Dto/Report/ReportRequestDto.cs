namespace Domain.Dto.Report;

public class ReportRequestDto
{
    public string ReportType { get; set; } = default!;
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? VendorId { get; set; }
    public string? Category { get; set; }
    public string? UserId { get; set; }
}
