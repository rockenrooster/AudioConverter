using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;

namespace AudioConverter
{
    /// <summary>
    /// Provides audio file analysis capabilities for fast path detection and metadata.
    /// </summary>
    internal unsafe static class AudioAnalyzer
    {
        /// <summary>
        /// Analyzes an audio file and returns its properties.
        /// </summary>
        public static AudioInfo? AnalyzeFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            AVFormatContext* formatContext = null;

            try
            {
                // Open input file
                if (avformat_open_input(&formatContext, filePath, null, null) != 0)
                    return null;

                // Retrieve stream information
                if (avformat_find_stream_info(formatContext, null) < 0)
                    return null;

                // Find audio stream
                int audioStreamIndex = -1;
                for (uint i = 0; i < formatContext->nb_streams; i++)
                {
                    if (formatContext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                    {
                        audioStreamIndex = (int)i;
                        break;
                    }
                }

                if (audioStreamIndex == -1)
                    return null;

                AVCodecParameters* codecPar = formatContext->streams[audioStreamIndex]->codecpar;

                // Get file extension
                string extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

                // Get format name
                string formatName = Marshal.PtrToStringAnsi((IntPtr)formatContext->iformat->name) ?? "unknown";

                // Get codec name
                string codecName = codecPar->codec_id.ToString();

                return new AudioInfo
                {
                    FilePath = filePath,
                    Format = extension,
                    FormatName = formatName,
                    CodecId = codecPar->codec_id,
                    CodecName = codecName,
                    Bitrate = codecPar->bit_rate > 0 ? (int)(codecPar->bit_rate / 1000) : 0,
                    SampleRate = codecPar->sample_rate,
                    Channels = codecPar->ch_layout.nb_channels,
                    Duration = formatContext->duration > 0 ? formatContext->duration / (double)AV_TIME_BASE : 0,
                    AudioStreamIndex = audioStreamIndex
                };
            }
            catch
            {
                return null;
            }
            finally
            {
                if (formatContext != null)
                    avformat_close_input(&formatContext);
            }
        }

        /// <summary>
        /// Determines if a fast-path conversion is possible (same format, only quality/codec changes).
        /// </summary>
        public static bool CanUseFastPath(AudioInfo inputInfo, string targetFormat, int targetBitrate, int targetSampleRate)
        {
            return false;
        }

        /// <summary>
        /// Extracts metadata from an audio file.
        /// </summary>
        public static AudioMetadata? ExtractMetadata(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            AVFormatContext* formatContext = null;

            try
            {
                if (avformat_open_input(&formatContext, filePath, null, null) != 0)
                    return null;

                if (avformat_find_stream_info(formatContext, null) < 0)
                    return null;

                var metadata = new AudioMetadata();

                // Extract metadata tags
                AVDictionary* dict = formatContext->metadata;
                AVDictionaryEntry* tag = null;

                while ((tag = av_dict_get(dict, "", tag, AV_DICT_IGNORE_SUFFIX)) != null)
                {
                    string key = Marshal.PtrToStringUTF8((IntPtr)tag->key) ?? "";
                    string value = Marshal.PtrToStringUTF8((IntPtr)tag->value) ?? "";

                    if (string.IsNullOrEmpty(key))
                        continue;

                    switch (key.ToLowerInvariant())
                    {
                        case "title":
                            metadata.Title = value;
                            break;
                        case "artist":
                        case "album_artist":
                            if (string.IsNullOrEmpty(metadata.Artist))
                                metadata.Artist = value;
                            break;
                        case "album":
                            metadata.Album = value;
                            break;
                        case "year":
                        case "date":
                            metadata.Year = value;
                            break;
                        case "track":
                            metadata.Track = value;
                            break;
                        case "genre":
                            metadata.Genre = value;
                            break;
                        case "comment":
                            metadata.Comment = value;
                            break;
                        case "composer":
                            metadata.Composer = value;
                            break;
                    }
                }

                // Check for attached pictures (album art)
                for (uint i = 0; i < formatContext->nb_streams; i++)
                {
                    if (formatContext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                    {
                        // This is likely attached picture
                        metadata.HasAttachedPicture = true;
                        break;
                    }
                }

                return metadata;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (formatContext != null)
                    avformat_close_input(&formatContext);
            }
        }
    }

    /// <summary>
    /// Contains information about an audio file.
    /// </summary>
    public class AudioInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string FormatName { get; set; } = string.Empty;
        public AVCodecID CodecId { get; set; }
        public string CodecName { get; set; } = string.Empty;
        public int Bitrate { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public double Duration { get; set; }
        public int AudioStreamIndex { get; set; }
    }

    /// <summary>
    /// Contains metadata tags from an audio file.
    /// </summary>
    public class AudioMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Track { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string Composer { get; set; } = string.Empty;
        public bool HasAttachedPicture { get; set; }
    }
}
