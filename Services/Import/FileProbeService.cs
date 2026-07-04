namespace AudioConverter;

internal sealed record FileProbeResult(bool Accepted, string? ErrorMessage, AudioInfo? AudioInfo)
{
    public static FileProbeResult Accept(AudioInfo info) => new(true, null, info);
    public static FileProbeResult Reject(string message) => new(false, message, null);
}

internal static class FileProbeService
{
    public static FileProbeResult Probe(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return FileProbeResult.Reject("File does not exist.");

        var info = AudioAnalyzer.AnalyzeFile(path);
        if (info == null)
            return FileProbeResult.Reject("No decodable audio stream was found.");

        if (!FFmpegCapabilities.HasDecoder(info.CodecId))
            return FileProbeResult.Reject($"No FFmpeg decoder is available for {info.CodecName}.");

        return FileProbeResult.Accept(info);
    }
}
