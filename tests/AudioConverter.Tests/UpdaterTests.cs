using System.Security.Cryptography;
using System.Text;
using AudioConverter;

namespace AudioConverter.Tests;

public sealed class UpdaterTests
{
    [Fact]
    public void VersionTagsAllowLeadingVAndCompare()
    {
        Assert.True(GitHubUpdateService.TryParseVersionTag("v2.0.0.22", out var parsed));
        Assert.Equal(new Version(2, 0, 0, 22), parsed);
        Assert.True(GitHubUpdateService.IsUpdateAvailable(new Version(2, 0, 0, 23), new Version(2, 0, 0, 22)));
        Assert.False(GitHubUpdateService.IsUpdateAvailable(new Version(2, 0, 0, 22), new Version(2, 0, 0, 22)));
    }

    [Fact]
    public void ManifestValidationRejectsWrongTrustedFields()
    {
        var asset = new UpdateReleaseAsset(
            UpdateTrust.AssetName,
            new Uri("https://github.com/rockenrooster/AudioConverter/releases/download/v2.0.0.23/AudioConverter.exe"),
            123);
        var manifest = ValidManifest();

        Assert.True(GitHubUpdateService.TryValidateManifest("v2.0.0.23", asset, manifest, out _));

        Assert.False(GitHubUpdateService.TryValidateManifest("v2.0.0.23", asset, manifest with { Tag = "v2.0.0.24" }, out _));
        Assert.False(GitHubUpdateService.TryValidateManifest("v2.0.0.23", asset, manifest with { Version = "2.0.0.24" }, out _));
        Assert.False(GitHubUpdateService.TryValidateManifest("v2.0.0.23", asset, manifest with { AssetName = "Other.exe" }, out _));
        Assert.False(GitHubUpdateService.TryValidateManifest("v2.0.0.23", asset, manifest with { Sha256 = "bad" }, out _));
        Assert.False(GitHubUpdateService.TryValidateManifest("v2.0.0.23", asset, manifest with { SizeBytes = 456 }, out _));
        Assert.False(GitHubUpdateService.TryValidateManifest("v2.0.0.23", asset, manifest with { DownloadUrl = "http://github.com/rockenrooster/AudioConverter/releases/download/v2.0.0.23/AudioConverter.exe" }, out _));
    }

    [Fact]
    public void SignatureValidationRejectsTamperedManifest()
    {
        byte[] manifest = Encoding.UTF8.GetBytes("""{"version":"2.0.0.23"}""");
        using var rsa = RSA.Create(2048);
        byte[] signature = rsa.SignData(manifest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        byte[] publicKey = rsa.ExportSubjectPublicKeyInfo();

        Assert.True(UpdateTrust.VerifyManifestSignature(manifest, signature, publicKey));
        manifest[5] ^= 1;
        Assert.False(UpdateTrust.VerifyManifestSignature(manifest, signature, publicKey));
    }

    [Fact]
    public void VerifyFileDeletesBadDownloads()
    {
        string dir = Path.Combine(Path.GetTempPath(), "AudioConverterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string sizePath = Path.Combine(dir, "AudioConverter-size.exe.download");
        File.WriteAllText(sizePath, "partial");

        bool sizeOk = GitHubUpdateService.VerifyFileAndDeleteOnMismatch(sizePath, 999, new string('0', 64));

        Assert.False(sizeOk);
        Assert.False(File.Exists(sizePath));

        string hashPath = Path.Combine(dir, "AudioConverter-hash.exe.download");
        File.WriteAllText(hashPath, "partial");

        bool hashOk = GitHubUpdateService.VerifyFileAndDeleteOnMismatch(hashPath, 7, new string('0', 64));

        Assert.False(hashOk);
        Assert.False(File.Exists(hashPath));
    }

    private static UpdateManifest ValidManifest() =>
        new(
            1,
            "2.0.0.23",
            "v2.0.0.23",
            UpdateTrust.AssetName,
            "https://github.com/rockenrooster/AudioConverter/releases/download/v2.0.0.23/AudioConverter.exe",
            new string('a', 64),
            123,
            DateTimeOffset.UtcNow);
}
