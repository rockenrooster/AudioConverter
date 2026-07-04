using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;

namespace AudioConverter
{
    internal unsafe static class FFmpegConverter
    {
        private static bool _initialized = false;
        private static readonly string[] RequiredLibraries = new[]
        {
            "avcodec-62.dll",
            "avformat-62.dll",
            "avutil-60.dll",
            "swresample-6.dll"
        };
        private static readonly Dictionary<string, IntPtr> LoadedLibraries = new(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                Logger.LogInfo("Initializing FFmpeg");

                // Get the directory where the native DLLs should be
                var baseDir = ResolveRuntimeDirectory();
                Logger.LogInfo($"Runtime Directory: {baseDir}");

                // Ensure FFmpeg.AutoGen loads DLLs from the extracted app folder
                ffmpeg.RootPath = baseDir;

                // Check if FFmpeg DLLs are present and preload them
                foreach (var dll in RequiredLibraries)
                {
                    var dllPath = Path.Combine(baseDir, dll);
                    var exists = File.Exists(dllPath);
                    Logger.LogInfo($"  {dll}: {(exists ? "FOUND" : "NOT FOUND")}");
                    if (!exists)
                        throw new FileNotFoundException($"Required FFmpeg library is missing: {dll}", dllPath);
                }
                PreloadLibraries(baseDir);
                VerifyExports();
                LogRuntimeVersions();

                // Initialize FFmpeg networking (optional for local files)
                Logger.LogInfo("Calling avformat_network_init()");
                try
                {
                    avformat_network_init();
                }
                catch (NotSupportedException)
                {
                    Logger.LogInfo("avformat_network_init not supported by the current FFmpeg build. Continuing without network support.");
                }

                _initialized = true;
                Logger.LogInfo("FFmpeg initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to initialize FFmpeg", ex);
                throw new InvalidOperationException($"Failed to initialize FFmpeg: {ex.Message}", ex);
            }
        }

        public static ConversionResult Convert(
            ConversionRequest request,
            IProgress<ConversionProgress>? progress,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var warnings = new List<string>();
            try
            {
                bool success = Convert(
                    request.InputPath,
                    request.OutputPath,
                    request.Format,
                    request.BitrateKbps,
                    request.SampleRate,
                    request.BitDepth,
                    cancellationToken,
                    request.PreserveMetadata,
                    request.UseFastPath,
                    request.InputInfo,
                    progress,
                    warnings,
                    request.ChannelMode);

                if (!success)
                    return ConversionResult.Failed("Conversion failed.", warnings);

                long? outputBytes = File.Exists(request.OutputPath) ? new FileInfo(request.OutputPath).Length : null;
                return ConversionResult.Succeeded(warnings, stopwatch.Elapsed, outputBytes);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return ConversionResult.Failed(ex.Message, warnings);
            }
        }

        public static string GetRuntimeInfo()
        {
            if (!_initialized)
                Initialize();

            return string.Join(Environment.NewLine, new[]
            {
                $"FFmpeg root: {ffmpeg.RootPath}",
                $"avcodec: {avcodec_version()}",
                $"avformat: {avformat_version()}",
                $"avutil: {avutil_version()}",
                $"swresample: {swresample_version()}"
            });
        }

        public static bool Convert(
            string inputPath,
            string outputPath,
            string format,
            int bitrate,
            int sampleRate,
            int bitDepth,
            CancellationToken cancellationToken,
            bool preserveMetadata = true,
            bool useFastPath = false,
            AudioInfo? inputInfo = null,
            IProgress<ConversionProgress>? progress = null,
            ICollection<string>? warnings = null,
            AudioChannelMode channelMode = AudioChannelMode.Preserve)
        {
            if (!_initialized)
            {
                Initialize();
            }

            int invalidDataCount = 0;

            cancellationToken.ThrowIfCancellationRequested();

            // Fast path: Check if we can just copy the stream (no re-encoding needed)
            if (useFastPath && inputInfo != null && CanUseStreamCopy(inputInfo, format, bitrate, sampleRate, bitDepth))
            {
                return ConvertWithStreamCopy(inputPath, outputPath, format, preserveMetadata, cancellationToken);
            }

            AVFormatContext* inputFormatContext = null;
            AVFormatContext* outputFormatContext = null;
            AVCodecContext* inputCodecContext = null;
            AVCodecContext* outputCodecContext = null;
            SwrContext* swrContext = null;
            AVDictionary* inputOptions = null;
            AVFrame* inputFrame = null;
            AVPacket* inputPacket = null;
            AVPacket* outputPacket = null;
            AVFrame* resampleFrame = null;
            AVFrame* encodeFrame = null;
            AVAudioFifo* audioFifo = null;
            AVChannelLayout inputChannelLayout = new();
            AVChannelLayout outputChannelLayout = new();

            try
            {
                // Open input file (be tolerant of minor corruption)
                av_dict_set(&inputOptions, "err_detect", "ignore_err", 0);
                av_dict_set(&inputOptions, "fflags", "discardcorrupt", 0);
                if (avformat_open_input(&inputFormatContext, inputPath, null, &inputOptions) != 0)
                {
                    throw new InvalidOperationException($"Could not open input file: {inputPath}");
                }
                av_dict_free(&inputOptions);

                // Discard corrupt packets where supported
                inputFormatContext->flags |= AVFMT_FLAG_DISCARD_CORRUPT;

                if (avformat_find_stream_info(inputFormatContext, null) < 0)
                {
                    throw new InvalidOperationException("Could not find stream information");
                }

                // Find audio stream
                int audioStreamIndex = -1;
                for (uint i = 0; i < inputFormatContext->nb_streams; i++)
                {
                    if (inputFormatContext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                    {
                        audioStreamIndex = (int)i;
                        break;
                    }
                }

                if (audioStreamIndex == -1)
                {
                    throw new InvalidOperationException("No audio stream found");
                }

                // Get input codec parameters
                AVCodecParameters* inputCodecPar = inputFormatContext->streams[audioStreamIndex]->codecpar;

                // Find input decoder
                AVCodec* inputCodec = avcodec_find_decoder(inputCodecPar->codec_id);
                if (inputCodec == null)
                {
                    throw new InvalidOperationException("Unsupported input codec");
                }

                // Allocate input codec context
                inputCodecContext = avcodec_alloc_context3(inputCodec);
                if (inputCodecContext == null)
                {
                    throw new InvalidOperationException("Could not allocate input codec context");
                }

                if (avcodec_parameters_to_context(inputCodecContext, inputCodecPar) < 0)
                {
                    throw new InvalidOperationException("Could not copy codec parameters");
                }

                if (avcodec_open2(inputCodecContext, inputCodec, null) < 0)
                {
                    throw new InvalidOperationException("Could not open input codec");
                }
                inputCodecContext->err_recognition = AV_EF_IGNORE_ERR;

                // Resolve input channel layout
                if (av_channel_layout_copy(&inputChannelLayout, &inputCodecContext->ch_layout) < 0 || inputChannelLayout.nb_channels == 0)
                {
                    av_channel_layout_uninit(&inputChannelLayout);
                    int channels = inputCodecPar->ch_layout.nb_channels > 0
                        ? inputCodecPar->ch_layout.nb_channels
                        : 2;
                    av_channel_layout_default(&inputChannelLayout, channels);
                }

                // Find output encoder based on format
                AVCodecID outputCodecId = GetOutputCodecId(format, bitDepth);
                AVCodec* outputCodec = avcodec_find_encoder(outputCodecId);
                if (outputCodec == null)
                {
                    throw new InvalidOperationException($"Unsupported output codec for format: {format}");
                }

                // Allocate output format context early for global header handling
                if (avformat_alloc_output_context2(&outputFormatContext, null, null, outputPath) < 0)
                {
                    throw new InvalidOperationException("Could not create output format context");
                }

                // Allocate output codec context
                outputCodecContext = avcodec_alloc_context3(outputCodec);
                if (outputCodecContext == null)
                {
                    throw new InvalidOperationException("Could not allocate output codec context");
                }

                // Select output settings based on encoder capabilities
                int outputSampleRate = SelectSampleRate(outputCodec, sampleRate);
                SelectChannelLayout(outputCodec, &inputChannelLayout, &outputChannelLayout, channelMode);
                AVSampleFormat outputSampleFormat = SelectSampleFormat(outputCodec, bitDepth);

                av_channel_layout_copy(&outputCodecContext->ch_layout, &outputChannelLayout);
                outputCodecContext->sample_rate = outputSampleRate;
                outputCodecContext->sample_fmt = outputSampleFormat;
                outputCodecContext->bit_rate = (long)bitrate * 1000;
                outputCodecContext->time_base = new AVRational { num = 1, den = outputSampleRate };

                if ((outputFormatContext->oformat->flags & AVFMT_GLOBALHEADER) != 0)
                {
                    outputCodecContext->flags |= AV_CODEC_FLAG_GLOBAL_HEADER;
                }

                if (avcodec_open2(outputCodecContext, outputCodec, null) < 0)
                {
                    throw new InvalidOperationException("Could not open output codec");
                }

                // Add audio stream to output format
                AVStream* outputStream = avformat_new_stream(outputFormatContext, null);
                if (outputStream == null)
                {
                    throw new InvalidOperationException("Could not create output stream");
                }

                if (avcodec_parameters_from_context(outputStream->codecpar, outputCodecContext) < 0)
                {
                    throw new InvalidOperationException("Could not copy codec parameters to output stream");
                }

                outputStream->time_base = outputCodecContext->time_base;

                // Open output file
                if ((outputFormatContext->oformat->flags & AVFMT_NOFILE) == 0)
                {
                    if (avio_open(&outputFormatContext->pb, outputPath, AVIO_FLAG_WRITE) < 0)
                    {
                        throw new InvalidOperationException($"Could not open output file: {outputPath}");
                    }
                }

                // Copy metadata if requested
                if (preserveMetadata)
                {
                    CopyMetadata(inputFormatContext, outputFormatContext);
                    CopyDictionary(inputFormatContext->streams[audioStreamIndex]->metadata, &outputStream->metadata);
                    CopyAttachedPictures(inputFormatContext, outputFormatContext, format, warnings);
                }

                // Write header
                if (avformat_write_header(outputFormatContext, null) < 0)
                {
                    throw new InvalidOperationException("Could not write output header");
                }
                WriteAttachedPictures(outputFormatContext);

                // Initialize resampler if needed
                bool needResampling = inputCodecContext->sample_fmt != outputCodecContext->sample_fmt ||
                                     inputCodecContext->sample_rate != outputCodecContext->sample_rate ||
                                     av_channel_layout_compare(&inputChannelLayout, &outputChannelLayout) != 0;

                if (needResampling)
                {
                    // Use swr_alloc_set_opts2 for FFmpeg 6+ API
                    int ret = swr_alloc_set_opts2(
                        &swrContext,
                        &outputChannelLayout,
                        outputCodecContext->sample_fmt,
                        outputCodecContext->sample_rate,
                        &inputChannelLayout,
                        inputCodecContext->sample_fmt,
                        inputCodecContext->sample_rate,
                        0,
                        null);

                    if (ret < 0 || swrContext == null)
                    {
                        throw new InvalidOperationException("Could not allocate resampler");
                    }

                    if (swr_init(swrContext) < 0)
                    {
                        throw new InvalidOperationException("Could not initialize resampler");
                    }
                }

                // Allocate frames and packets
                inputFrame = av_frame_alloc();
                resampleFrame = av_frame_alloc();
                encodeFrame = av_frame_alloc();
                inputPacket = av_packet_alloc();
                outputPacket = av_packet_alloc();

                if (inputFrame == null || resampleFrame == null || encodeFrame == null || inputPacket == null || outputPacket == null)
                {
                    throw new InvalidOperationException("Failed to allocate FFmpeg buffers");
                }

                // Initialize audio FIFO for proper frame sizing
                int fifoCapacity = outputCodecContext->frame_size > 0 ? outputCodecContext->frame_size : 1024;
                audioFifo = av_audio_fifo_alloc(outputCodecContext->sample_fmt, outputCodecContext->ch_layout.nb_channels, fifoCapacity);
                if (audioFifo == null)
                {
                    throw new InvalidOperationException("Could not allocate audio FIFO");
                }

                long nextPts = 0;
                AVStream* inputStream = inputFormatContext->streams[audioStreamIndex];
                double totalSeconds = inputInfo?.Duration > 0
                    ? inputInfo.Duration
                    : inputFormatContext->duration > 0
                        ? inputFormatContext->duration / (double)AV_TIME_BASE
                        : 0;
                var progressClock = Stopwatch.StartNew();

                void ReportProgress(AVFrame* frame)
                {
                    if (progress == null || totalSeconds <= 0 || progressClock.ElapsedMilliseconds < 200)
                        return;

                    long timestamp = frame->best_effort_timestamp;
                    if (timestamp == AV_NOPTS_VALUE)
                        return;

                    double currentSeconds = timestamp * av_q2d(inputStream->time_base);
                    double fraction = Math.Clamp(currentSeconds / totalSeconds, 0, 0.999);
                    progress.Report(new ConversionProgress(inputPath, currentSeconds, totalSeconds, fraction));
                    progressClock.Restart();
                }

                // Read and process packets
                while (av_read_frame(inputFormatContext, inputPacket) >= 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (inputPacket->stream_index == audioStreamIndex)
                    {
                        // Send packet to decoder
                        int ret = avcodec_send_packet(inputCodecContext, inputPacket);
                        if (ret < 0 && ret != AVERROR(EAGAIN) && ret != AVERROR_EOF)
                        {
                            if (ret == AVERROR_INVALIDDATA)
                            {
                                invalidDataCount++;
                                Logger.LogWarning($"Skipping corrupt packet: {GetErrorString(ret)}");
                                av_packet_unref(inputPacket);
                                continue;
                            }
                            throw new InvalidOperationException($"Error sending packet to decoder: {GetErrorString(ret)}");
                        }

                        while (true)
                        {
                            ret = avcodec_receive_frame(inputCodecContext, inputFrame);
                            if (ret == AVERROR(EAGAIN) || ret == AVERROR_EOF)
                                break;
                            if (ret < 0)
                            {
                                if (ret == AVERROR_INVALIDDATA)
                                {
                                    invalidDataCount++;
                                    Logger.LogWarning($"Skipping corrupt frame: {GetErrorString(ret)}");
                                    break;
                                }
                                throw new InvalidOperationException($"Error decoding audio frame: {GetErrorString(ret)}");
                            }

                            if (needResampling)
                            {
                                int outCapacity = (int)av_rescale_rnd(
                                    swr_get_delay(swrContext, inputCodecContext->sample_rate) + inputFrame->nb_samples,
                                    outputCodecContext->sample_rate,
                                    inputCodecContext->sample_rate,
                                    AVRounding.AV_ROUND_UP);

                                if (outCapacity > 0)
                                {
                                    EnsureFrameCapacity(resampleFrame, outputCodecContext, outCapacity);
                                    int outSamples = swr_convert(
                                        swrContext,
                                        resampleFrame->extended_data,
                                        outCapacity,
                                        inputFrame->extended_data,
                                        inputFrame->nb_samples);

                                    if (outSamples < 0)
                                    {
                                        throw new InvalidOperationException("Error resampling audio");
                                    }

                                    WriteSamplesToFifo(audioFifo, resampleFrame, outSamples);
                                }
                            }
                            else
                            {
                                WriteSamplesToFifo(audioFifo, inputFrame, inputFrame->nb_samples);
                            }

                            ReportProgress(inputFrame);
                            DrainFifo(audioFifo, encodeFrame, outputCodecContext, outputPacket, outputStream, outputFormatContext, ref nextPts, false);
                            av_frame_unref(inputFrame);
                        }
                    }

                    av_packet_unref(inputPacket);
                }

                // Flush decoder
                avcodec_send_packet(inputCodecContext, null);
                while (true)
                {
                    int ret = avcodec_receive_frame(inputCodecContext, inputFrame);
                    if (ret == AVERROR(EAGAIN) || ret == AVERROR_EOF)
                        break;
                    if (ret < 0)
                    {
                        if (ret == AVERROR_INVALIDDATA)
                        {
                            invalidDataCount++;
                            Logger.LogWarning($"Skipping corrupt frame: {GetErrorString(ret)}");
                            break;
                        }
                        throw new InvalidOperationException($"Error decoding audio frame: {GetErrorString(ret)}");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (needResampling)
                    {
                        int outCapacity = (int)av_rescale_rnd(
                            swr_get_delay(swrContext, inputCodecContext->sample_rate) + inputFrame->nb_samples,
                            outputCodecContext->sample_rate,
                            inputCodecContext->sample_rate,
                            AVRounding.AV_ROUND_UP);

                        if (outCapacity > 0)
                        {
                            EnsureFrameCapacity(resampleFrame, outputCodecContext, outCapacity);
                            int outSamples = swr_convert(
                                swrContext,
                                resampleFrame->extended_data,
                                outCapacity,
                                inputFrame->extended_data,
                                inputFrame->nb_samples);

                            if (outSamples < 0)
                            {
                                throw new InvalidOperationException("Error resampling audio");
                            }

                            WriteSamplesToFifo(audioFifo, resampleFrame, outSamples);
                        }
                    }
                    else
                    {
                        WriteSamplesToFifo(audioFifo, inputFrame, inputFrame->nb_samples);
                    }

                    ReportProgress(inputFrame);
                    DrainFifo(audioFifo, encodeFrame, outputCodecContext, outputPacket, outputStream, outputFormatContext, ref nextPts, false);
                    av_frame_unref(inputFrame);
                }

                // Flush resampler
                if (needResampling)
                {
                    while (true)
                    {
                        int outCapacity = (int)av_rescale_rnd(
                            swr_get_delay(swrContext, inputCodecContext->sample_rate),
                            outputCodecContext->sample_rate,
                            inputCodecContext->sample_rate,
                            AVRounding.AV_ROUND_UP);

                        if (outCapacity <= 0)
                            break;

                        EnsureFrameCapacity(resampleFrame, outputCodecContext, outCapacity);
                        int outSamples = swr_convert(
                            swrContext,
                            resampleFrame->extended_data,
                            outCapacity,
                            null,
                            0);

                        if (outSamples <= 0)
                            break;

                        WriteSamplesToFifo(audioFifo, resampleFrame, outSamples);
                        DrainFifo(audioFifo, encodeFrame, outputCodecContext, outputPacket, outputStream, outputFormatContext, ref nextPts, false);
                    }
                }

                // Drain any remaining samples and flush encoder
                DrainFifo(audioFifo, encodeFrame, outputCodecContext, outputPacket, outputStream, outputFormatContext, ref nextPts, true);
                EncodeAndWriteFrame(outputCodecContext, null, outputPacket, outputStream, outputFormatContext);

                // Write trailer
                av_write_trailer(outputFormatContext);
                progress?.Report(new ConversionProgress(inputPath, totalSeconds, totalSeconds > 0 ? totalSeconds : null, 1));
                AddInvalidDataWarning(warnings, invalidDataCount);

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AddInvalidDataWarning(warnings, invalidDataCount);
                Logger.LogError("FFmpeg conversion failed", ex);
                throw;
            }
            finally
            {
                // Clean up channel layouts
                av_channel_layout_uninit(&inputChannelLayout);
                av_channel_layout_uninit(&outputChannelLayout);

                // Clean up resources
                if (audioFifo != null) av_audio_fifo_free(audioFifo);
                if (outputPacket != null) av_packet_free(&outputPacket);
                if (inputPacket != null) av_packet_free(&inputPacket);
                if (inputFrame != null) av_frame_free(&inputFrame);
                if (resampleFrame != null) av_frame_free(&resampleFrame);
                if (encodeFrame != null) av_frame_free(&encodeFrame);
                if (swrContext != null) swr_free(&swrContext);
                if (outputCodecContext != null) avcodec_free_context(&outputCodecContext);
                if (inputCodecContext != null) avcodec_free_context(&inputCodecContext);
                if (outputFormatContext != null)
                {
                    if ((outputFormatContext->oformat->flags & AVFMT_NOFILE) == 0)
                    {
                        avio_closep(&outputFormatContext->pb);
                    }
                    avformat_free_context(outputFormatContext);
                }
                if (inputFormatContext != null) avformat_close_input(&inputFormatContext);
            }
        }

        private static void EncodeAndWriteFrame(
            AVCodecContext* codecContext,
            AVFrame* frame,
            AVPacket* packet,
            AVStream* stream,
            AVFormatContext* formatContext)
        {
            int ret = avcodec_send_frame(codecContext, frame);
            if (ret == AVERROR(EAGAIN))
            {
                DrainEncoderPackets(codecContext, packet, stream, formatContext);
                ret = avcodec_send_frame(codecContext, frame);
            }
            if (ret < 0 && ret != AVERROR_EOF)
            {
                throw new InvalidOperationException("Error sending frame to encoder");
            }

            DrainEncoderPackets(codecContext, packet, stream, formatContext);
        }

        private static AVCodecID GetOutputCodecId(string format, int bitDepth)
        {
            return OutputPresetCatalog.GetCodecId(format, bitDepth);
        }

        private static int SelectSampleRate(AVCodec* codec, int requested)
        {
            if (codec->supported_samplerates == null)
                return requested;

            int best = 0;
            int bestDiff = int.MaxValue;

            for (int* p = codec->supported_samplerates; *p != 0; p++)
            {
                int rate = *p;
                int diff = Math.Abs(rate - requested);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = rate;
                }

                if (diff == 0)
                    break;
            }

            return best != 0 ? best : requested;
        }

        private static void SelectChannelLayout(AVCodec* codec, AVChannelLayout* inputLayout, AVChannelLayout* outputLayout, AudioChannelMode channelMode)
        {
            int requestedChannels = channelMode switch
            {
                AudioChannelMode.Mono => 1,
                AudioChannelMode.Stereo => 2,
                _ => inputLayout->nb_channels > 0 ? inputLayout->nb_channels : 2
            };

            if (channelMode != AudioChannelMode.Preserve)
            {
                if (codec->ch_layouts != null)
                {
                    for (AVChannelLayout* p = codec->ch_layouts; p->nb_channels != 0; p++)
                    {
                        if (p->nb_channels == requestedChannels)
                        {
                            av_channel_layout_copy(outputLayout, p);
                            return;
                        }
                    }

                    Logger.LogWarning("Encoder does not advertise requested channel layout; falling back to default channel layout.");
                }

                av_channel_layout_default(outputLayout, requestedChannels);
                return;
            }

            if (codec->ch_layouts == null)
            {
                if (av_channel_layout_copy(outputLayout, inputLayout) < 0 || outputLayout->nb_channels == 0)
                {
                    av_channel_layout_uninit(outputLayout);
                    av_channel_layout_default(outputLayout, inputLayout->nb_channels > 0 ? inputLayout->nb_channels : 2);
                }
                return;
            }

            AVChannelLayout* best = null;
            for (AVChannelLayout* p = codec->ch_layouts; p->nb_channels != 0; p++)
            {
                if (p->nb_channels == inputLayout->nb_channels)
                {
                    best = p;
                    break;
                }
                if (best == null)
                {
                    best = p;
                }
            }

            if (best != null)
            {
                av_channel_layout_copy(outputLayout, best);
                return;
            }

            av_channel_layout_default(outputLayout, inputLayout->nb_channels > 0 ? inputLayout->nb_channels : 2);
        }

        private static AVSampleFormat SelectSampleFormat(AVCodec* codec, int bitDepth)
        {
            AVSampleFormat requested = GetSampleFormatFromBitDepth(bitDepth);

            if (codec->sample_fmts == null)
                return requested;

            AVSampleFormat planar = av_get_planar_sample_fmt(requested);
            AVSampleFormat packed = av_get_packed_sample_fmt(requested);

            AVSampleFormat[] candidates = bitDepth switch
            {
                8 => new[]
                {
                    requested, planar, packed,
                    AVSampleFormat.AV_SAMPLE_FMT_U8, AVSampleFormat.AV_SAMPLE_FMT_U8P,
                    AVSampleFormat.AV_SAMPLE_FMT_S16, AVSampleFormat.AV_SAMPLE_FMT_S16P
                },
                16 => new[]
                {
                    requested, planar, packed,
                    AVSampleFormat.AV_SAMPLE_FMT_S16P, AVSampleFormat.AV_SAMPLE_FMT_S16,
                    AVSampleFormat.AV_SAMPLE_FMT_FLTP, AVSampleFormat.AV_SAMPLE_FMT_FLT
                },
                24 => new[]
                {
                    requested, planar, packed,
                    AVSampleFormat.AV_SAMPLE_FMT_S32, AVSampleFormat.AV_SAMPLE_FMT_S32P,
                    AVSampleFormat.AV_SAMPLE_FMT_S16P, AVSampleFormat.AV_SAMPLE_FMT_S16,
                    AVSampleFormat.AV_SAMPLE_FMT_FLTP, AVSampleFormat.AV_SAMPLE_FMT_FLT
                },
                32 => new[]
                {
                    requested, planar, packed,
                    AVSampleFormat.AV_SAMPLE_FMT_FLTP, AVSampleFormat.AV_SAMPLE_FMT_FLT,
                    AVSampleFormat.AV_SAMPLE_FMT_S32, AVSampleFormat.AV_SAMPLE_FMT_S32P,
                    AVSampleFormat.AV_SAMPLE_FMT_S16P, AVSampleFormat.AV_SAMPLE_FMT_S16
                },
                _ => new[]
                {
                    requested, planar, packed,
                    AVSampleFormat.AV_SAMPLE_FMT_S16P, AVSampleFormat.AV_SAMPLE_FMT_S16,
                    AVSampleFormat.AV_SAMPLE_FMT_FLTP, AVSampleFormat.AV_SAMPLE_FMT_FLT
                }
            };

            foreach (var fmt in candidates)
            {
                if (fmt == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                    continue;
                if (IsSampleFormatSupported(codec, fmt))
                    return fmt;
            }

            return codec->sample_fmts[0];
        }

        private static AVSampleFormat GetSampleFormatFromBitDepth(int bitDepth)
        {
            return bitDepth switch
            {
                8 => AVSampleFormat.AV_SAMPLE_FMT_U8,
                16 => AVSampleFormat.AV_SAMPLE_FMT_S16,
                24 => AVSampleFormat.AV_SAMPLE_FMT_S32,
                32 => AVSampleFormat.AV_SAMPLE_FMT_FLT,
                _ => AVSampleFormat.AV_SAMPLE_FMT_S16
            };
        }

        private static bool IsSampleFormatSupported(AVCodec* codec, AVSampleFormat format)
        {
            if (codec->sample_fmts == null)
                return true;

            for (AVSampleFormat* p = codec->sample_fmts; *p != AVSampleFormat.AV_SAMPLE_FMT_NONE; p++)
            {
                if (*p == format)
                    return true;
            }

            return false;
        }

        private static void EnsureFrameCapacity(AVFrame* frame, AVCodecContext* codecContext, int nbSamples)
        {
            if (nbSamples <= 0)
                return;

            if (frame->nb_samples != nbSamples ||
                frame->format != (int)codecContext->sample_fmt ||
                av_channel_layout_compare(&frame->ch_layout, &codecContext->ch_layout) != 0)
            {
                av_frame_unref(frame);
                av_channel_layout_uninit(&frame->ch_layout);
                frame->nb_samples = nbSamples;
                frame->format = (int)codecContext->sample_fmt;
                av_channel_layout_copy(&frame->ch_layout, &codecContext->ch_layout);

                if (av_frame_get_buffer(frame, 0) < 0)
                {
                    throw new InvalidOperationException("Could not allocate audio frame buffer");
                }
            }

            if (av_frame_make_writable(frame) < 0)
            {
                throw new InvalidOperationException("Could not make audio frame writable");
            }
        }

        private static void PreloadLibraries(string baseDir)
        {
            foreach (var dll in RequiredLibraries)
            {
                var dllPath = Path.Combine(baseDir, dll);
                if (!File.Exists(dllPath))
                {
                    continue;
                }

                if (LoadedLibraries.ContainsKey(dll))
                {
                    continue;
                }

                try
                {
                    var handle = NativeLibrary.Load(dllPath);
                    LoadedLibraries[dll] = handle;
                    Logger.LogInfo($"  {dll}: LOADED");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"  {dll}: FAILED TO LOAD", ex);
                    throw new InvalidOperationException($"Failed to load FFmpeg library {dll}. A dependent DLL may be missing.", ex);
                }
            }
        }

        private static string ResolveRuntimeDirectory()
        {
            var nativeSearchDirs = ((AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") as string) ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var candidates = nativeSearchDirs.Concat(new[]
            {
                AppContext.BaseDirectory,
                AppDomain.CurrentDomain.BaseDirectory,
                Path.GetDirectoryName(typeof(FFmpegConverter).Assembly.Location) ?? string.Empty,
                Environment.CurrentDirectory
            });

            foreach (var candidate in candidates.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (RequiredLibraries.All(dll => File.Exists(Path.Combine(candidate, dll))))
                    return candidate;
            }

            return AppContext.BaseDirectory;
        }

        private static void VerifyExports()
        {
            VerifyExport("avformat-62.dll", "avformat_open_input");
            VerifyExport("avformat-62.dll", "avformat_find_stream_info");
            VerifyExport("avcodec-62.dll", "avcodec_find_decoder");
            VerifyExport("avutil-60.dll", "av_strerror");
            VerifyExport("swresample-6.dll", "swr_init");
        }

        private static void VerifyExport(string libraryName, string exportName)
        {
            if (!LoadedLibraries.TryGetValue(libraryName, out var handle) || handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"{libraryName} was not loaded; cannot verify export {exportName}.");
            }

            if (NativeLibrary.TryGetExport(handle, exportName, out _))
            {
                Logger.LogInfo($"  {libraryName}: export OK {exportName}");
            }
            else
            {
                Logger.LogInfo($"  {libraryName}: export MISSING {exportName}");
                throw new InvalidOperationException($"{libraryName} is missing required export {exportName}.");
            }
        }

        private static void LogRuntimeVersions()
        {
            Logger.LogInfo($"FFmpeg RootPath: {ffmpeg.RootPath}");
            Logger.LogInfo($"avcodec version: {avcodec_version()}");
            Logger.LogInfo($"avformat version: {avformat_version()}");
            Logger.LogInfo($"avutil version: {avutil_version()}");
            Logger.LogInfo($"swresample version: {swresample_version()}");
        }

        private static void AddInvalidDataWarning(ICollection<string>? warnings, int invalidDataCount)
        {
            if (invalidDataCount > 0)
                warnings?.Add($"Converted with {invalidDataCount} corrupt frame(s) skipped.");
        }

        private static string GetErrorString(int error)
        {
            const int bufferSize = 1024;
            byte* buffer = stackalloc byte[bufferSize];
            int result = av_strerror(error, buffer, (ulong)bufferSize);
            if (result < 0)
            {
                return $"FFmpeg error {error}";
            }

            string? message = Marshal.PtrToStringUTF8((IntPtr)buffer);
            if (string.IsNullOrWhiteSpace(message))
            {
                return $"FFmpeg error {error}";
            }

            return $"{message} ({error})";
        }

        private static void WriteSamplesToFifo(AVAudioFifo* fifo, AVFrame* frame, int nbSamples)
        {
            if (nbSamples <= 0)
                return;

            if (av_audio_fifo_realloc(fifo, av_audio_fifo_size(fifo) + nbSamples) < 0)
            {
                throw new InvalidOperationException("Could not resize audio FIFO");
            }

            int written = av_audio_fifo_write(fifo, (void**)frame->extended_data, nbSamples);
            if (written < nbSamples)
            {
                throw new InvalidOperationException("Could not write samples to audio FIFO");
            }
        }

        private static void DrainFifo(
            AVAudioFifo* audioFifo,
            AVFrame* encodeFrame,
            AVCodecContext* outputCodecContext,
            AVPacket* outputPacket,
            AVStream* outputStream,
            AVFormatContext* outputFormatContext,
            ref long nextPts,
            bool flush)
        {
            if (audioFifo == null)
                return;

            int frameSize = outputCodecContext->frame_size;
            while (true)
            {
                int available = av_audio_fifo_size(audioFifo);
                if (available <= 0)
                    break;

                if (!flush && frameSize > 0 && available < frameSize)
                    break;

                int toRead = frameSize > 0 ? Math.Min(frameSize, available) : available;
                EnsureFrameCapacity(encodeFrame, outputCodecContext, toRead);

                int read = av_audio_fifo_read(audioFifo, (void**)encodeFrame->extended_data, toRead);
                if (read < toRead)
                {
                    throw new InvalidOperationException("Could not read samples from audio FIFO");
                }

                encodeFrame->nb_samples = read;
                encodeFrame->pts = nextPts;
                nextPts += read;

                EncodeAndWriteFrame(outputCodecContext, encodeFrame, outputPacket, outputStream, outputFormatContext);
            }
        }

        private static void DrainEncoderPackets(
            AVCodecContext* codecContext,
            AVPacket* packet,
            AVStream* stream,
            AVFormatContext* formatContext)
        {
            while (true)
            {
                int recv = avcodec_receive_packet(codecContext, packet);
                if (recv == AVERROR(EAGAIN) || recv == AVERROR_EOF)
                    return;
                if (recv < 0)
                    throw new InvalidOperationException("Error receiving packet from encoder");

                // Rescale packet timestamps
                av_packet_rescale_ts(packet, codecContext->time_base, stream->time_base);
                packet->stream_index = stream->index;

                // Write packet
                int write = av_interleaved_write_frame(formatContext, packet);
                if (write < 0)
                {
                    throw new InvalidOperationException("Error writing packet");
                }

                av_packet_unref(packet);
            }
        }

        /// <summary>
        /// Copies metadata from input to output format context.
        /// </summary>
        private static void CopyMetadata(AVFormatContext* inputContext, AVFormatContext* outputContext)
        {
            CopyDictionary(inputContext->metadata, &outputContext->metadata);
        }

        private static void CopyDictionary(AVDictionary* source, AVDictionary** destination)
        {
            if (source == null)
                return;

            AVDictionaryEntry* tag = null;
            while ((tag = av_dict_get(source, "", tag, AV_DICT_IGNORE_SUFFIX)) != null)
            {
                string key = Marshal.PtrToStringUTF8((IntPtr)tag->key) ?? "";
                string value = Marshal.PtrToStringUTF8((IntPtr)tag->value) ?? "";

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    av_dict_set(destination, key, value, 0);
                }
            }
        }

        /// <summary>
        /// Checks if attached pictures (album art) exist in the input file.
        /// </summary>
        internal static bool SupportsAttachedPictures(string format)
        {
            return format.Equals("mp3", StringComparison.OrdinalIgnoreCase) ||
                format.Equals("m4a", StringComparison.OrdinalIgnoreCase) ||
                format.Equals("flac", StringComparison.OrdinalIgnoreCase);
        }

        private static void CopyAttachedPictures(
            AVFormatContext* inputContext,
            AVFormatContext* outputContext,
            string format,
            ICollection<string>? warnings)
        {
            if (!SupportsAttachedPictures(format))
                return;

            int copied = 0;
            for (uint i = 0; i < inputContext->nb_streams; i++)
            {
                AVStream* inputStream = inputContext->streams[i];
                if (!IsAttachedPictureStream(inputStream))
                    continue;

                if (inputStream->attached_pic.data == null || inputStream->attached_pic.size <= 0)
                {
                    warnings?.Add("Album art was detected but had no packet data to copy.");
                    continue;
                }

                AVPacket picturePacket = default;
                int packetRef = av_packet_ref(&picturePacket, &inputStream->attached_pic);
                if (packetRef < 0)
                {
                    warnings?.Add($"Album art was not copied: {GetErrorString(packetRef)}");
                    continue;
                }

                AVStream* outputStream = avformat_new_stream(outputContext, null);
                if (outputStream == null)
                {
                    av_packet_unref(&picturePacket);
                    warnings?.Add("Album art was not copied: could not create output cover stream.");
                    continue;
                }

                if (avcodec_parameters_copy(outputStream->codecpar, inputStream->codecpar) < 0)
                {
                    av_packet_unref(&picturePacket);
                    throw new InvalidOperationException("Could not copy album art stream parameters");
                }

                outputStream->time_base = inputStream->time_base;
                outputStream->disposition = inputStream->disposition | AV_DISPOSITION_ATTACHED_PIC;
                CopyDictionary(inputStream->metadata, &outputStream->metadata);

                picturePacket.stream_index = outputStream->index;
                picturePacket.pts = 0;
                picturePacket.dts = 0;
                outputStream->attached_pic = picturePacket;
                copied++;
            }

            if (copied == 0 && FindAttachedPictureStream(inputContext) >= 0)
                warnings?.Add("Album art was detected but could not be copied.");
        }

        private static void WriteAttachedPictures(AVFormatContext* outputContext)
        {
            for (uint i = 0; i < outputContext->nb_streams; i++)
            {
                AVStream* stream = outputContext->streams[i];
                if (!IsAttachedPictureStream(stream) ||
                    stream->attached_pic.data == null ||
                    stream->attached_pic.size <= 0)
                {
                    continue;
                }

                AVPacket packet = default;
                int packetRef = av_packet_ref(&packet, &stream->attached_pic);
                if (packetRef < 0)
                    throw new InvalidOperationException($"Could not reference album art packet: {GetErrorString(packetRef)}");

                try
                {
                    packet.stream_index = stream->index;
                    packet.pts = 0;
                    packet.dts = 0;
                    packet.pos = -1;

                    int write = av_interleaved_write_frame(outputContext, &packet);
                    if (write < 0)
                        throw new InvalidOperationException($"Could not write album art packet: {GetErrorString(write)}");
                }
                finally
                {
                    av_packet_unref(&packet);
                }
            }
        }

        private static bool IsAttachedPictureStream(AVStream* stream)
        {
            return stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO &&
                (stream->disposition & AV_DISPOSITION_ATTACHED_PIC) != 0;
        }

        private static int FindAttachedPictureStream(AVFormatContext* formatContext)
        {
            for (uint i = 0; i < formatContext->nb_streams; i++)
            {
                AVStream* stream = formatContext->streams[i];
                if (IsAttachedPictureStream(stream))
                    return (int)i;
            }
            return -1;
        }

        /// <summary>
        /// Determines if we can use stream copy (no re-encoding) for the conversion.
        /// This is much faster than re-encoding.
        /// </summary>
        private static bool CanUseStreamCopy(AudioInfo inputInfo, string targetFormat, int targetBitrate, int targetSampleRate, int targetBitDepth)
        {
            // Stream copy is only possible when:
            // 1. The container format is the same (e.g., mp3->mp3, m4a->m4a)
            // 2. We're not changing codec parameters (bitrate, sample rate, etc.)
            // Note: In practice, for lossy formats like MP3, stream copy without parameter changes
            // is rarely useful. This is more applicable for format conversions (e.g., m4a->mp4 container)

            // For now, we'll be conservative and only use stream copy when:
            // - Input format matches target format exactly
            // - No quality settings are being changed
            // This allows for operations like metadata updates or container changes

            return false; // Disabled for now - re-encoding is usually needed for quality changes
        }

        /// <summary>
        /// Converts audio by copying streams without re-encoding (fast path).
        /// </summary>
        private static bool ConvertWithStreamCopy(
            string inputPath,
            string outputPath,
            string format,
            bool preserveMetadata,
            CancellationToken cancellationToken)
        {
            AVFormatContext* inputContext = null;
            AVFormatContext* outputContext = null;

            try
            {
                // Open input
                if (avformat_open_input(&inputContext, inputPath, null, null) != 0)
                    return false;

                if (avformat_find_stream_info(inputContext, null) < 0)
                    return false;

                // Allocate output context
                if (avformat_alloc_output_context2(&outputContext, null, null, outputPath) < 0)
                    return false;

                // Copy all streams
                for (uint i = 0; i < inputContext->nb_streams; i++)
                {
                    AVStream* inStream = inputContext->streams[i];
                    AVStream* outStream = avformat_new_stream(outputContext, null);
                    if (outStream == null)
                        return false;

                    if (avcodec_parameters_copy(outStream->codecpar, inStream->codecpar) < 0)
                        return false;

                    outStream->time_base = inStream->time_base;
                }

                // Copy metadata if requested
                if (preserveMetadata)
                {
                    CopyMetadata(inputContext, outputContext);
                }

                // Open output file
                if ((outputContext->oformat->flags & AVFMT_NOFILE) == 0)
                {
                    if (avio_open(&outputContext->pb, outputPath, AVIO_FLAG_WRITE) < 0)
                        return false;
                }

                // Write header
                if (avformat_write_header(outputContext, null) < 0)
                    return false;

                // Copy packets
                AVPacket* packet = av_packet_alloc();
                if (packet == null)
                    return false;

                try
                {
                    while (av_read_frame(inputContext, packet) >= 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Adjust timestamp
                        av_packet_rescale_ts(packet,
                            inputContext->streams[packet->stream_index]->time_base,
                            outputContext->streams[packet->stream_index]->time_base);

                        packet->pos = -1;

                        if (av_interleaved_write_frame(outputContext, packet) < 0)
                        {
                            av_packet_unref(packet);
                            return false;
                        }

                        av_packet_unref(packet);
                    }
                }
                finally
                {
                    av_packet_free(&packet);
                }

                // Write trailer
                av_write_trailer(outputContext);

                return true;
            }
            finally
            {
                if (outputContext != null)
                {
                    if ((outputContext->oformat->flags & AVFMT_NOFILE) == 0)
                    {
                        avio_closep(&outputContext->pb);
                    }
                    avformat_free_context(outputContext);
                }
                if (inputContext != null)
                    avformat_close_input(&inputContext);
            }
        }
    }
}
