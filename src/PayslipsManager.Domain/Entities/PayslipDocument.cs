using PayslipsManager.Domain.Enums;

namespace PayslipsManager.Domain.Entities;

/// <summary>
/// Represents a monthly payslip PDF stored in Azure Blob Storage.
/// </summary>
public class PayslipDocument
{
    public string BlobName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public DateOnly PayslipDate { get; set; }
    public BlobAccessTier AccessTier { get; set; } = BlobAccessTier.Hot;
    public bool IsArchived => AccessTier == BlobAccessTier.Archive;
    public DateTimeOffset UploadedOn { get; set; }
    public string ContentType { get; set; } = "application/pdf";
    public Dictionary<string, string> Tags { get; set; } = [];

    /// <summary>
    /// Gets a display-friendly month/year string, e.g. "2026-03".
    /// </summary>
    public string GetPeriodDisplay() => $"{PayslipDate.Year}-{PayslipDate.Month:D2}";

    /// <summary>
    /// Checks whether this payslip belongs to the specified employee.
    /// </summary>
    public bool BelongsTo(string employeeId)
    {
        return string.Equals(EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase);
    }
}
