using AudioConverter;

namespace AudioConverter.Tests;

public sealed class OutputPresetTests
{
    [Fact]
    public void CatalogKeepsSevenCurrentOutputs()
    {
        Assert.Equal(new[] { "mp3", "aac", "flac", "wav", "ogg", "opus", "m4a" }, OutputPresetCatalog.All.Select(p => p.Id));
    }

    [Fact]
    public void AacChoiceUsesM4aContainer()
    {
        var preset = OutputPresetCatalog.Get("aac");

        Assert.Equal("m4a", preset.Extension);
        Assert.Equal("ipod", preset.MuxerName);
    }
}
