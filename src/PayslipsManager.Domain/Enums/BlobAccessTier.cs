namespace PayslipsManager.Domain.Enums;

/// <summary>
/// Represents the Azure Blob Storage access tier.
/// </summary>
public enum BlobAccessTier
{
    /// <summary>
    /// Hot tier - optimized for frequent access.
    /// </summary>
    Hot,

    /// <summary>
    /// Cool tier - optimized for infrequent access (30+ days).
    /// </summary>
    Cool,

    /// <summary>
    /// Archive tier - optimized for rare access (180+ days).
    /// </summary>
    Archive
}
