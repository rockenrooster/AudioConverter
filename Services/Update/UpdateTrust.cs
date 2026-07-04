using System.Security.Cryptography;

namespace AudioConverter;

internal static class UpdateTrust
{
    public const string Owner = "rockenrooster";
    public const string Repository = "AudioConverter";
    public const string AssetName = "AudioConverter.exe";
    public const string ManifestName = AssetName + ".manifest.json";
    public const string SignatureName = AssetName + ".manifest.sig";
    public const string GitHubApiLatestReleaseUrl = "https://api.github.com/repos/rockenrooster/AudioConverter/releases/latest";
    public const string GitHubReleaseAssetBaseUrl = "https://github.com/rockenrooster/AudioConverter/releases/download/";

    internal const string PublicKeyBase64 = "MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAwEJ4IYBnQbgeN1yfnoAQNN5kYiedIgYc89IKFkQnX0fb9fQwl43acgzqG4SfmKL+ZQOHD/cYCG7fctMFOFIH0PDmWlPKfEmXgK+5sPBxw84UjlRdO86Jit9eFoFpcROEyaz1q2Q7JLhRo0m1yO8ckQT1tLfbQAqIgLLSkNX48JxbJiX/2RDKJS5ThYka6+QOkvGv8xnoN04iKQdzqNqJuQVuGHhgSzd+Fv+Ew2bN21OdKCi4VWbJtDgD+zI23g9VWUW19NE5Out4k0TV9GyM1/mN3ADut0HJN/LPPr2Yd1rXTyQ1kOKuFIF8MRgugO1pCauyGejmJxDjW+y3ThqJBoH7cWrYIkcwToarZEsb1B5lRWX5qqvNK6KfY8TJKmaikKytPA9+OZyU5sU2Anirga+Va/HufHam6kCjVUsHq9iiYjtmJwK3ocIRDkiniFTpx7bJqgqSLAspSJ6+bIM8AH2JzLMQR/tjiMnpmvezLvFXjeff5m3knY7cKgMSNX2dAgMBAAE=";

    public static bool HasPublicKey => !string.IsNullOrWhiteSpace(PublicKeyBase64);

    public static bool VerifyManifestSignature(byte[] manifestBytes, byte[] signatureBytes)
    {
        if (!HasPublicKey)
            return false;

        return VerifyManifestSignature(manifestBytes, signatureBytes, Convert.FromBase64String(PublicKeyBase64));
    }

    internal static bool VerifyManifestSignature(byte[] manifestBytes, byte[] signatureBytes, byte[] publicKeyBytes)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
        return rsa.VerifyData(manifestBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
