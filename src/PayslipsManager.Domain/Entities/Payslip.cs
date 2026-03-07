namespace PayslipsManager.Domain.Entities;

/// <summary>
/// Represents a monthly payslip document for an employee.
/// </summary>
public class Payslip
{
    public string Id { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }
    public string AccessTier { get; set; } = "Hot";
    public string Status { get; set; } = "Available";

    /// <summary>
    /// Gets the full blob path (container/blobName).
    /// </summary>
    public string GetFullBlobPath() => $"{ContainerName}/{BlobName}";

    /// <summary>
    /// Checks if this payslip belongs to the specified employee.
    /// </summary>
    public bool BelongsTo(string employeeEmail)
    {
        return string.Equals(EmployeeEmail, employeeEmail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a display-friendly month/year string.
    /// </summary>
    public string GetPeriodDisplay() => $"{Year}-{Month:D2}";
}
