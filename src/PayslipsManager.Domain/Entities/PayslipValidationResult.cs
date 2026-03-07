namespace PayslipsManager.Domain.Entities;

/// <summary>
/// Result of validating a payslip document (e.g. blob name, metadata, content type).
/// </summary>
public class PayslipValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    private PayslipValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static PayslipValidationResult Success() => new(true, []);

    public static PayslipValidationResult Failure(IEnumerable<string> errors) =>
        new(false, errors.ToList().AsReadOnly());

    public static PayslipValidationResult Failure(string error) =>
        new(false, new List<string> { error }.AsReadOnly());
}
