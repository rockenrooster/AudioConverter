using AudioConverter;

namespace AudioConverter.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void SaveAndLoadRoundTripsCurrentOptions()
    {
        string path = NewSettingsPath();
        var settings = new AppSettings
        {
            OutputPath = @"C:\Music\Out",
            Bitrate = 256,
            Threads = 4,
            Format = "flac",
            SampleRate = "48000",
            BitDepth = "24",
            ChannelMode = "Mono",
            UseSourceSampleRate = true,
            OutputFolderStructure = true
        };

        settings.Save(path);
        var loaded = AppSettings.Load(path);

        Assert.Equal(settings.OutputPath, loaded.OutputPath);
        Assert.Equal(settings.Bitrate, loaded.Bitrate);
        Assert.Equal(settings.Threads, loaded.Threads);
        Assert.Equal(settings.Format, loaded.Format);
        Assert.Equal(settings.SampleRate, loaded.SampleRate);
        Assert.Equal(settings.BitDepth, loaded.BitDepth);
        Assert.Equal(settings.ChannelMode, loaded.ChannelMode);
        Assert.Equal(settings.UseSourceSampleRate, loaded.UseSourceSampleRate);
        Assert.Equal(settings.OutputFolderStructure, loaded.OutputFolderStructure);
    }

    [Fact]
    public void LoadCorruptJsonReturnsDefaults()
    {
        string path = NewSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ nope");

        var loaded = AppSettings.Load(path);

        Assert.Equal("mp3", loaded.Format);
        Assert.Equal("Preserve", loaded.ChannelMode);
        Assert.Equal(192, loaded.Bitrate);
    }

    private static string NewSettingsPath() =>
        Path.Combine(Path.GetTempPath(), "AudioConverterTests", Guid.NewGuid().ToString("N"), "settings.json");
}
