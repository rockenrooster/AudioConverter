using AudioConverter;

namespace AudioConverter.Tests;

public sealed class OutputPathResolverTests
{
    [Fact]
    public void SameExtensionWithEmptyOutputRootDoesNotOverwriteSource()
    {
        string input = Path.Combine(Path.GetTempPath(), "song.mp3");

        var result = Resolve(input, null, null, "mp3");

        Assert.False(OutputPathResolver.SamePath(input, result.FinalOutputPath));
        Assert.EndsWith("song (converted).mp3", result.FinalOutputPath);
        Assert.True(result.WouldOverwriteInput);
    }

    [Fact]
    public void DifferentExtensionWithEmptyOutputRootUsesInputFolder()
    {
        string input = Path.Combine(Path.GetTempPath(), "song.wav");

        var result = Resolve(input, null, null, "mp3");

        Assert.Equal(Path.Combine(Path.GetTempPath(), "song.mp3"), result.FinalOutputPath);
    }

    [Fact]
    public void ExplicitOutputRootUsesThatRoot()
    {
        string outputRoot = Path.Combine(Path.GetTempPath(), "audio-out");
        string input = Path.Combine(Path.GetTempPath(), "song.mp3");

        var result = Resolve(input, null, outputRoot, "mp3");

        Assert.Equal(Path.Combine(outputRoot, "song.mp3"), result.FinalOutputPath);
    }

    [Fact]
    public void ExistingDestinationAutoRenames()
    {
        string dir = NewTempDir();
        File.WriteAllText(Path.Combine(dir, "song.mp3"), "x");

        var result = Resolve(Path.Combine(dir, "song.wav"), null, dir, "mp3");

        Assert.Equal(Path.Combine(dir, "song (2).mp3"), result.FinalOutputPath);
        Assert.True(result.WouldOverwriteExistingOutput);
    }

    [Fact]
    public void ReservedDestinationAutoRenames()
    {
        string dir = NewTempDir();
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string input = Path.Combine(dir, "song.wav");

        var first = Resolve(input, null, dir, "mp3", reserved: reserved);
        reserved.Add(first.FinalOutputPath);
        var second = Resolve(input, null, dir, "mp3", reserved: reserved);

        Assert.Equal(Path.Combine(dir, "song.mp3"), first.FinalOutputPath);
        Assert.Equal(Path.Combine(dir, "song (2).mp3"), second.FinalOutputPath);
    }

    [Fact]
    public void FullFolderStructureIncludesDriveToken()
    {
        string dir = NewTempDir();
        string input = Path.Combine(Path.GetPathRoot(dir)!, "Music", "Album", "song.wav");

        var result = Resolve(input, null, dir, "mp3", preserveFullPath: true);

        Assert.Contains(OutputPathResolver.GetRootToken(Path.GetPathRoot(dir)!), result.FinalOutputPath);
        Assert.EndsWith(Path.Combine("Music", "Album", "song.mp3"), result.FinalOutputPath);
    }

    [Fact]
    public void RelativePathIsPreserved()
    {
        string dir = NewTempDir();
        string input = Path.Combine(Path.GetTempPath(), "song.wav");

        var result = Resolve(input, Path.Combine("Dropped", "Album", "song.wav"), dir, "flac");

        Assert.Equal(Path.Combine(dir, "Dropped", "Album", "song.flac"), result.FinalOutputPath);
    }

    [Fact]
    public void TemporaryPathIsSafeAndKeepsMediaExtension()
    {
        string dir = NewTempDir();
        string input = Path.Combine(Path.GetTempPath(), "song.wav");

        var result = Resolve(input, null, dir, "mp3");

        Assert.Equal(Path.GetDirectoryName(result.FinalOutputPath), Path.GetDirectoryName(result.TemporaryOutputPath));
        Assert.EndsWith(".tmp.mp3", result.TemporaryOutputPath);
        Assert.False(OutputPathResolver.SamePath(result.FinalOutputPath, result.TemporaryOutputPath));
        Assert.False(OutputPathResolver.SamePath(input, result.TemporaryOutputPath));
    }

    [Fact]
    public void SkipPolicyThrowsOnConflict()
    {
        string dir = NewTempDir();
        File.WriteAllText(Path.Combine(dir, "song.mp3"), "x");

        Assert.Throws<OutputPathConflictException>(() => Resolve(
            Path.Combine(dir, "song.wav"),
            null,
            dir,
            "mp3",
            policy: ExistingFilePolicy.Skip));
    }

    private static OutputPathResult Resolve(
        string input,
        string? relativePath,
        string? outputRoot,
        string extension,
        ISet<string>? reserved = null,
        bool preserveFullPath = false,
        ExistingFilePolicy policy = ExistingFilePolicy.AutoRename) =>
        new OutputPathResolver().Resolve(new OutputPathRequest(input, relativePath, outputRoot, extension, preserveFullPath, policy, reserved));

    private static string NewTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "AudioConverterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
