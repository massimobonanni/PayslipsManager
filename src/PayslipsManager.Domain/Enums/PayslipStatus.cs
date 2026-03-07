namespace PayslipsManager.Domain.Enums;

/// <summary>
/// Represents the status of a payslip document.
/// </summary>
public enum PayslipStatus
{
    /// <summary>
    /// Payslip is available for viewing and download.
    /// </summary>
    Available,

    /// <summary>
    /// Payslip has been archived (moved to Cool or Archive tier).
    /// </summary>
    Archived,

    /// <summary>
    /// Payslip is being processed or uploaded.
    /// </summary>
    Processing,

    /// <summary>
    /// Payslip has been deleted or marked for deletion.
    /// </summary>
    Deleted
}
