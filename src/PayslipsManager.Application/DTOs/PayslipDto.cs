namespace PayslipsManager.Application.DTOs;

/// <summary>
/// Data transfer object for payslip information.
/// </summary>
public class PayslipDto
{
    public string Id { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string Period { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }
    public string AccessTier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
