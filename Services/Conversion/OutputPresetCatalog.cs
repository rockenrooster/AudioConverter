using FFmpeg.AutoGen;

namespace AudioConverter;

internal static class OutputPresetCatalog
{
    private static readonly OutputPreset[] Presets =
    {
        new("mp3", "MP3", "mp3", AVCodecID.AV_CODEC_ID_MP3, "mp3"),
        new("aac", "AAC", "m4a", AVCodecID.AV_CODEC_ID_AAC, "ipod"),
        new("flac", "FLAC", "flac", AVCodecID.AV_CODEC_ID_FLAC, "flac", SupportsBitDepth: true),
        new("wav", "WAV PCM", "wav", AVCodecID.AV_CODEC_ID_PCM_S16LE, "wav", SupportsBitDepth: true),
        new("ogg", "Ogg Vorbis", "ogg", AVCodecID.AV_CODEC_ID_VORBIS, "ogg"),
        new("opus", "Opus", "opus", AVCodecID.AV_CODEC_ID_OPUS, "opus"),
        new("m4a", "M4A AAC", "m4a", AVCodecID.AV_CODEC_ID_AAC, "ipod")
    };

    public static IReadOnlyList<OutputPreset> All => Presets;

    public static IEnumerable<OutputPreset> GetSupportedPresets()
    {
        var supported = Presets.Where(p => FFmpegCapabilities.HasEncoder(p.CodecId) && FFmpegCapabilities.HasMuxer(p.MuxerName)).ToArray();
        return supported.Length == 0 ? Presets : supported;
    }

    public static OutputPreset Get(string id) =>
        Presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Presets[0];

    public static AVCodecID GetCodecId(string id, int bitDepth) =>
        string.Equals(id, "wav", StringComparison.OrdinalIgnoreCase)
            ? bitDepth switch
            {
                16 => AVCodecID.AV_CODEC_ID_PCM_S16LE,
                24 => AVCodecID.AV_CODEC_ID_PCM_S24LE,
                _ => AVCodecID.AV_CODEC_ID_PCM_S16LE
            }
            : Get(id).CodecId;
}
