using FFmpeg.AutoGen;

namespace AudioConverter;

internal sealed record OutputPreset(
    string Id,
    string DisplayName,
    string Extension,
    AVCodecID CodecId,
    string MuxerName,
    bool SupportsBitDepth = false);
