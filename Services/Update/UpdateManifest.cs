namespace AudioConverter;

internal sealed record UpdateManifest(
    int SchemaVersion,
    string Version,
    string Tag,
    string AssetName,
    string DownloadUrl,
    string Sha256,
    long SizeBytes,
    DateTimeOffset CreatedUtc);

internal sealed record UpdateReleaseAsset(
    string Name,
    Uri DownloadUrl,
    long SizeBytes);

internal sealed record UpdateCheckResult(
    Version CurrentVersion,
    Version? LatestVersion,
    string LatestVersionText,
    bool IsUpdateAvailable,
    bool CanDownload,
    bool CanInstall,
    string? ReleaseUrl,
    string? AssetName,
    string? DownloadUrl,
    long AssetSizeBytes,
    string? AssetSha256,
    string Message,
    UpdateManifest? Manifest);
