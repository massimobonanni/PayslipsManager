namespace PayslipsManager.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Azure Blob Storage.
/// </summary>
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Storage account service URL (e.g. https://mystorageaccount.blob.core.windows.net).
    /// </summary>
    public string AccountUrl { get; set; } = string.Empty;

    /// <summary>
    /// Prefix for employee containers. Each employee container is named {ContainerPrefix}-{employeeId}.
    /// </summary>
    public string ContainerPrefix { get; set; } = "payslips";

    /// <summary>
    /// Use managed identity (DefaultAzureCredential) for authentication.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Connection string for local development only. Do not use in production.
    /// </summary>
    public string? ConnectionString { get; set; }
}
