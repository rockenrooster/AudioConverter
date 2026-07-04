namespace AudioConverter;

internal sealed record ConversionRequest(
    string InputPath,
    string OutputPath,
    string Format,
    int BitrateKbps,
    int SampleRate,
    int BitDepth,
    AudioChannelMode ChannelMode,
    bool PreserveMetadata,
    bool UseFastPath,
    AudioInfo? InputInfo);
