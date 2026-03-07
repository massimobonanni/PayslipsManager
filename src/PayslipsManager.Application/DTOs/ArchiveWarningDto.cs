namespace PayslipsManager.Application.DTOs;

/// <summary>
/// Carries archive-tier warning information so the UI can alert the user
/// that the payslip is not immediately downloadable.
/// </summary>
public class ArchiveWarningDto
{
    public bool IsArchived { get; set; }
    public string CurrentTier { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
