namespace PayslipsManager.Application.DTOs;

/// <summary>
/// Lightweight DTO for the payslip list page.
/// </summary>
public class PayslipListItemDto
{
    public string BlobName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateOnly PayslipDate { get; set; }
    public string Period { get; set; } = string.Empty;
    public string AccessTier { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTimeOffset UploadedOn { get; set; }
}
