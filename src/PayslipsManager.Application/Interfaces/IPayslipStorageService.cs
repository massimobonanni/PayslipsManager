using PayslipsManager.Domain.Entities;

namespace PayslipsManager.Application.Interfaces;

/// <summary>
/// Low-level storage operations for payslip blobs.
/// Implemented by the infrastructure layer against Azure Blob Storage.
/// Each employee has a dedicated container named {prefix}-{employeeId}.
/// Blob naming convention: yyyy-MM.pdf
/// </summary>
public interface IPayslipStorageService
{
    /// <summary>
    /// Lists all payslip documents in the employee's container.
    /// </summary>
    Task<IReadOnlyList<PayslipDocument>> ListPayslipsAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single payslip document by blob name from the employee's container.
    /// </summary>
    Task<PayslipDocument?> GetPayslipAsync(string employeeId, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads payslip content as a stream.
    /// </summary>
    Task<Stream?> DownloadContentAsync(string employeeId, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a time-limited SAS URL for direct download.
    /// </summary>
    Task<string?> GenerateSasUrlAsync(string employeeId, string blobName, TimeSpan validity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a payslip PDF blob with metadata and blob index tags.
    /// </summary>
    Task UploadPayslipAsync(string employeeId, string blobName, Stream content,
        IDictionary<string, string> metadata, IDictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates blob index tags on an existing payslip.
    /// </summary>
    Task SetTagsAsync(string employeeId, string blobName, IDictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the employee container if it does not already exist.
    /// </summary>
    Task EnsureContainerExistsAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the Azure Blob container name for an employee.
    /// </summary>
    string ResolveContainerName(string employeeId);

    /// <summary>
    /// Validates that a blob name follows the yyyy-MM.pdf convention.
    /// </summary>
    PayslipValidationResult ValidateBlobName(string blobName);

    /// <summary>
    /// Validates that the content type is application/pdf.
    /// </summary>
    PayslipValidationResult ValidateContentType(string contentType);
}
