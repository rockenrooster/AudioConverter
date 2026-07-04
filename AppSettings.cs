using System.IO;
using System.Text.Json;

namespace AudioConverter
{
    internal class AppSettings
    {
        public string OutputPath { get; set; } = @"";
        public int Bitrate { get; set; } = 192;
        public int Threads { get; set; } = 1;
        public string Format { get; set; } = "mp3";
        public string SampleRate { get; set; } = "44100";
        public string BitDepth { get; set; } = "16";
        public string ChannelMode { get; set; } = "Preserve";
        public bool UseSourceSampleRate { get; set; } = false;
        public bool OutputFolderStructure { get; set; } = false;

        private static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AudioConverter",
            "settings.json");

        internal static bool HasSavedSettings => File.Exists(SettingsFilePath);

        public static AppSettings Load()
        {
            return Load(SettingsFilePath);
        }

        internal static AppSettings Load(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
                catch
                {
                    return new AppSettings();
                }
            }
            return new AppSettings();
        }

        public void Save()
        {
            Save(SettingsFilePath);
        }

        internal void Save(string path)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }
    }
}
