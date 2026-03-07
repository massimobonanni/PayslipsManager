namespace PayslipsManager.Infrastructure.Configuration;

/// <summary>
/// Constants for blob metadata and blob index tag keys used on payslip blobs.
/// </summary>
internal static class PayslipTagKeys
{
    public const string EmployeeId = "EmployeeId";
    public const string EmployeeDisplayName = "EmployeeDisplayName";
    public const string PayslipYear = "PayslipYear";
    public const string PayslipMonth = "PayslipMonth";
    public const string EmploymentStatus = "EmploymentStatus";
    public const string DocumentType = "DocumentType";
    public const string DocumentTypeValue = "Payslip";
}
