using System;
using System.IO;

namespace AudioConverter
{
    internal static class AudioHelper
    {
        private static readonly string[] AudioExtensions =
        {
            ".3ga", ".3gp", ".aac", ".ac3", ".aif", ".aiff", ".alac", ".amr",
            ".ape", ".au", ".bwf", ".caf", ".dsf",
            ".flac", ".gsm", ".m4a", ".m4b", ".m4r",
            ".mka", ".mp2", ".mp3", ".mpc", ".msv", ".ogg", ".opus",
            ".pcm", ".ra", ".rf64", ".shn", ".snd", ".spx", ".swa",
            ".tta", ".voc", ".vox", ".wav", ".wma", ".wv",
            ".dts",

            // Video containers (audio extraction)
            ".mp4", ".mov", ".webm", ".mkv", ".mxf", ".avi", ".wmv", ".flv", ".mpg",
            ".vob", ".mts", ".m2ts", ".m4v", ".rmvb", ".divx", ".xvid", ".ts", ".asf",
            ".f4v", ".ogv", ".m2v", ".svi",
            ".amv", ".nsv", ".roq", ".dat", ".gifv", ".m1v", ".qt",
            ".mj2", ".dv", ".drc", ".viv", ".ivf", ".f4p", ".mjp", ".mpe",
            ".mpv", ".lsf", ".gvi", ".3g2"
        };

        public static bool IsAudioFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string ext = Path.GetExtension(path).ToLowerInvariant();
            return Array.Exists(AudioExtensions, e => e == ext);
        }

        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public static double GetPercentSaved(long beforeSize, long afterSize)
        {
            if (beforeSize == 0) return 0;
            return ((beforeSize - afterSize) * 100.0) / beforeSize;
        }

        public static int ClampNumeric(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static decimal ClampNumeric(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
