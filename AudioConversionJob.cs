using System.Threading;

namespace AudioConverter
{
    public class AudioConversionJob
    {
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string TemporaryOutputPath { get; set; } = string.Empty;
        public string Format { get; set; } = "mp3";
        public int Bitrate { get; set; } = 192;
        public int SampleRate { get; set; } = 44100;
        public int BitDepth { get; set; } = 16;
        internal AudioChannelMode ChannelMode { get; set; } = AudioChannelMode.Preserve;
        public int RowIndex { get; set; }
        public double ProgressWeight { get; set; } = 1;
        public long InputBytes { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public bool PreserveMetadata { get; set; } = true;
        public bool UseFastPath { get; set; } = false;
        public bool OutputWasAutoRenamed { get; set; }
        public AudioInfo? InputInfo { get; set; }
    }
}
