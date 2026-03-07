using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PayslipsManager.Functions;

/// <summary>
/// Azure Function triggered when a new payslip blob is created.
/// Handles metadata tagging and access tier management.
/// </summary>
public class PayslipBlobTriggerFunction
{
    private readonly ILogger<PayslipBlobTriggerFunction> _logger;

    public PayslipBlobTriggerFunction(ILogger<PayslipBlobTriggerFunction> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Triggered when a new blob is created in the payslips container.
    /// Processes the blob metadata and sets appropriate tags.
    /// </summary>
    [Function(nameof(PayslipBlobTriggerFunction))]
    public async Task Run(
        [BlobTrigger("payslips/{name}", Connection = "BlobStorage:ConnectionString")] BlobClient blobClient,
        string name,
        FunctionContext context)
    {
        _logger.LogInformation("Processing blob: {BlobName}", name);

        try
        {
            // Get blob properties and metadata
            var properties = await blobClient.GetPropertiesAsync();
            var metadata = properties.Value.Metadata;

            _logger.LogInformation("Blob metadata count: {Count}", metadata.Count);

            // Extract payslip information from blob name or metadata
            // Expected format: {employeeId}_{year}_{month}.pdf
            var payslipInfo = ParsePayslipFileName(name);

            if (payslipInfo != null)
            {
                // Set blob tags for efficient querying
                var tags = new Dictionary<string, string>
                {
                    { "EmployeeId", payslipInfo.EmployeeId },
                    { "EmployeeEmail", payslipInfo.EmployeeEmail },
                    { "EmployeeName", payslipInfo.EmployeeName },
                    { "Year", payslipInfo.Year.ToString() },
                    { "Month", payslipInfo.Month.ToString() },
                    { "ProcessedAt", DateTimeOffset.UtcNow.ToString("O") }
                };

                await blobClient.SetTagsAsync(tags);
                _logger.LogInformation("Successfully tagged blob {BlobName} for employee {EmployeeEmail}", name, payslipInfo.EmployeeEmail);

                // Update metadata
                metadata["Status"] = "Available";
                metadata["ProcessedBy"] = "PayslipBlobTriggerFunction";
                metadata["ProcessedAt"] = DateTimeOffset.UtcNow.ToString("O");
                await blobClient.SetMetadataAsync(metadata);

                _logger.LogInformation("Payslip blob {BlobName} processed successfully", name);
            }
            else
            {
                _logger.LogWarning("Unable to parse payslip information from blob name: {BlobName}", name);
                
                // Tag as unprocessed
                var tags = new Dictionary<string, string>
                {
                    { "Status", "Unprocessed" },
                    { "ProcessedAt", DateTimeOffset.UtcNow.ToString("O") }
                };
                await blobClient.SetTagsAsync(tags);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing blob {BlobName}: {ErrorMessage}", name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Parses payslip information from the blob filename.
    /// Expected format: {employeeId}_{year}_{month}.pdf or metadata in blob
    /// </summary>
    private PayslipInfo? ParsePayslipFileName(string blobName)
    {
        try
        {
            // Remove .pdf extension
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(blobName);
            var parts = nameWithoutExtension.Split('_');

            if (parts.Length >= 3)
            {
                var employeeId = parts[0];
                var year = int.Parse(parts[1]);
                var month = int.Parse(parts[2]);

                // In a real scenario, you would look up employee details from a database
                // For demo purposes, generate email from employee ID
                var employeeEmail = $"{employeeId}@contoso.com";
                var employeeName = $"Employee {employeeId}";

                return new PayslipInfo
                {
                    EmployeeId = employeeId,
                    EmployeeEmail = employeeEmail,
                    EmployeeName = employeeName,
                    Year = year,
                    Month = month
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing blob name: {BlobName}", blobName);
            return null;
        }
    }

    private class PayslipInfo
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
    }
}
