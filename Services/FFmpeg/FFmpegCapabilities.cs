using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;

namespace AudioConverter;

internal unsafe static class FFmpegCapabilities
{
    public static bool HasEncoder(AVCodecID codecId) => avcodec_find_encoder(codecId) != null;

    public static bool HasDecoder(AVCodecID codecId) => avcodec_find_decoder(codecId) != null;

    public static bool HasMuxer(string muxerName) => av_guess_format(muxerName, null, null) != null;
}
