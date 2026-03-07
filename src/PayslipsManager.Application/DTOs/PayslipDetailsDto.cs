namespace PayslipsManager.Application.DTOs;

/// <summary>
/// Full-detail DTO for the payslip details page.
/// </summary>
public class PayslipDetailsDto
{
    public string BlobName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public DateOnly PayslipDate { get; set; }
    public string Period { get; set; } = string.Empty;
    public string AccessTier { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTimeOffset UploadedOn { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = [];

    /// <summary>
    /// If the payslip is archived, contains warning information.
    /// </summary>
    public ArchiveWarningDto? ArchiveWarning { get; set; }
}
