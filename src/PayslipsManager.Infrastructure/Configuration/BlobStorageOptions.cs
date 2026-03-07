namespace PayslipsManager.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Azure Blob Storage.
/// </summary>
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Storage account connection string or service URL.
    /// </summary>
    public string AccountUrl { get; set; } = string.Empty;

    /// <summary>
    /// Container name for storing payslips.
    /// </summary>
    public string ContainerName { get; set; } = "payslips";

    /// <summary>
    /// Use managed identity for authentication.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Connection string (if not using managed identity).
    /// </summary>
    public string? ConnectionString { get; set; }
}
