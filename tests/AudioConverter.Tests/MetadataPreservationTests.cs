using AudioConverter;

namespace AudioConverter.Tests;

public sealed class MetadataPreservationTests
{
    [Fact]
    public void AttachedPicturesAreOnlyCopiedForMuxersThatSupportThem()
    {
        Assert.True(FFmpegConverter.SupportsAttachedPictures("mp3"));
        Assert.True(FFmpegConverter.SupportsAttachedPictures("m4a"));
        Assert.True(FFmpegConverter.SupportsAttachedPictures("flac"));
        Assert.False(FFmpegConverter.SupportsAttachedPictures("wav"));
        Assert.False(FFmpegConverter.SupportsAttachedPictures("ogg"));
        Assert.False(FFmpegConverter.SupportsAttachedPictures("opus"));
    }
}
