using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace AudioConverter;

internal sealed class GitHubUpdateService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;

    public GitHubUpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(Version currentVersion, CancellationToken cancellationToken)
    {
        try
        {
            using var releaseRequest = CreateRequest(new Uri(UpdateTrust.GitHubApiLatestReleaseUrl));
            using var releaseResponse = await _httpClient.SendAsync(releaseRequest, cancellationToken).ConfigureAwait(false);
            if (!releaseResponse.IsSuccessStatusCode)
                return Result(currentVersion, null, "no release");

            using var releaseStream = await releaseResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(releaseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = doc.RootElement;
            string tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
            string releaseUrl = root.TryGetProperty("html_url", out var html) ? html.GetString() ?? string.Empty : string.Empty;

            if (!TryParseVersionTag(tag, out var latestVersion))
                return Result(currentVersion, tag, "latest tag is not a version");

            if (!TryFindAssets(root, out var installer, out var manifestAsset, out var signatureAsset))
                return Result(currentVersion, tag, "update found, but trusted assets are missing", latestVersion);

            byte[] manifestBytes = await GetBytesAsync(manifestAsset.DownloadUrl, cancellationToken).ConfigureAwait(false);
            byte[] signatureBytes = await GetBytesAsync(signatureAsset.DownloadUrl, cancellationToken).ConfigureAwait(false);
            if (!UpdateTrust.VerifyManifestSignature(manifestBytes, signatureBytes))
                return Result(currentVersion, tag, "update found, but manifest validation failed", latestVersion);

            var manifest = JsonSerializer.Deserialize<UpdateManifest>(manifestBytes, JsonOptions);
            if (!TryValidateManifest(tag, installer, manifest, out var validationMessage))
                return Result(currentVersion, tag, validationMessage, latestVersion);

            bool isUpdate = IsUpdateAvailable(latestVersion, currentVersion);
            return new UpdateCheckResult(
                currentVersion,
                latestVersion,
                latestVersion.ToString(),
                isUpdate,
                isUpdate,
                true,
                releaseUrl,
                installer.Name,
                installer.DownloadUrl.ToString(),
                installer.SizeBytes,
                manifest!.Sha256,
                isUpdate ? "update available" : "up to date",
                manifest);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result(currentVersion, null, $"update check failed: {ex.Message}");
        }
    }

    public async Task<string> DownloadAndVerifyAsync(UpdateCheckResult result, CancellationToken cancellationToken, IProgress<int>? progress = null)
    {
        if (!result.CanDownload || result.Manifest == null || string.IsNullOrWhiteSpace(result.DownloadUrl))
            throw new InvalidOperationException("No trusted update is available.");

        string updateDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AudioConverter", "Updates");
        Directory.CreateDirectory(updateDir);
        string tempPath = Path.Combine(updateDir, UpdateTrust.AssetName + ".download");
        string finalPath = Path.Combine(updateDir, "AudioConverterUpdated.exe");
        SafeDelete(tempPath);

        try
        {
            using var request = CreateRequest(new Uri(result.DownloadUrl));
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long length && length != result.Manifest.SizeBytes)
                throw new InvalidOperationException("Downloaded update size did not match the manifest.");

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var destination = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    total += read;
                    if (result.Manifest.SizeBytes > 0)
                        progress?.Report((int)Math.Clamp(total * 100 / result.Manifest.SizeBytes, 0, 100));
                }
            }

            if (!VerifyFileAndDeleteOnMismatch(tempPath, result.Manifest.SizeBytes, result.Manifest.Sha256))
                throw new InvalidOperationException("Downloaded update failed hash or size verification.");

            SafeDelete(finalPath);
            File.Move(tempPath, finalPath);
            progress?.Report(100);
            return finalPath;
        }
        catch
        {
            SafeDelete(tempPath);
            throw;
        }
    }

    internal static bool TryParseVersionTag(string tag, out Version version)
    {
        version = new Version(0, 0);
        string text = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag[1..] : tag;
        return Version.TryParse(text, out version!);
    }

    internal static bool IsUpdateAvailable(Version latestVersion, Version currentVersion) => latestVersion > currentVersion;

    internal static bool TryValidateManifest(string releaseTag, UpdateReleaseAsset asset, UpdateManifest? manifest, out string message)
    {
        message = "update found, but manifest validation failed";
        if (manifest == null)
            return false;

        if (manifest.SchemaVersion != 1 ||
            manifest.Tag != releaseTag ||
            !TryParseVersionTag(releaseTag, out var releaseVersion) ||
            !Version.TryParse(manifest.Version, out var manifestVersion) ||
            manifestVersion != releaseVersion ||
            manifest.AssetName != UpdateTrust.AssetName ||
            manifest.AssetName != asset.Name ||
            manifest.SizeBytes <= 0 ||
            manifest.SizeBytes != asset.SizeBytes ||
            !IsSha256(manifest.Sha256) ||
            manifest.CreatedUtc == default ||
            !Uri.TryCreate(manifest.DownloadUrl, UriKind.Absolute, out var manifestUrl) ||
            manifestUrl.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(manifestUrl.AbsoluteUri, asset.DownloadUrl.AbsoluteUri, StringComparison.OrdinalIgnoreCase) ||
            !manifest.DownloadUrl.StartsWith(UpdateTrust.GitHubReleaseAssetBaseUrl + releaseTag + "/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        message = "trusted update";
        return true;
    }

    internal static bool VerifyFileAndDeleteOnMismatch(string path, long expectedSizeBytes, string expectedSha256)
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length != expectedSizeBytes)
            {
                SafeDelete(path);
                return false;
            }

            string actual;
            using (var stream = File.OpenRead(path))
                actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();

            if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                SafeDelete(path);
                return false;
            }

            return true;
        }
        catch
        {
            SafeDelete(path);
            return false;
        }
    }

    private static UpdateCheckResult Result(Version currentVersion, string? latestText, string message, Version? latestVersion = null) =>
        new(currentVersion, latestVersion, latestText ?? string.Empty, false, false, false, null, null, null, 0, null, message, null);

    private static HttpRequestMessage CreateRequest(Uri uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("AudioConverter-Updater");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        return request;
    }

    private async Task<byte[]> GetBytesAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(uri);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool TryFindAssets(JsonElement releaseRoot, out UpdateReleaseAsset installer, out UpdateReleaseAsset manifest, out UpdateReleaseAsset signature)
    {
        installer = manifest = signature = new UpdateReleaseAsset(string.Empty, new Uri("https://example.invalid"), 0);
        if (!releaseRoot.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var asset in assets.EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? string.Empty;
            string url = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
            long size = asset.TryGetProperty("size", out var sizeValue) ? sizeValue.GetInt64() : 0;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                continue;

            var item = new UpdateReleaseAsset(name, uri, size);
            if (name == UpdateTrust.AssetName)
                installer = item;
            else if (name == UpdateTrust.ManifestName)
                manifest = item;
            else if (name == UpdateTrust.SignatureName)
                signature = item;
        }

        return installer.SizeBytes > 0 && manifest.SizeBytes > 0 && signature.SizeBytes > 0;
    }

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(c => char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F');

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }
}
