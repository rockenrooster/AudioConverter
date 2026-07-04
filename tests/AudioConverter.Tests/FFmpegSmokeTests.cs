using AudioConverter;

namespace AudioConverter.Tests;

public sealed class FFmpegSmokeTests
{
    [Fact]
    public void Mp3CoverArtSurvivesTranscode()
    {
        if (!HasNativeRuntime())
            return;

        string dir = NewTempDir();
        string wav = Path.Combine(dir, "input.wav");
        string plain = Path.Combine(dir, "plain.mp3");
        string tagged = Path.Combine(dir, "tagged.mp3");
        string output = Path.Combine(dir, "out.mp3");

        WriteSineWave(wav);
        ConvertAudio(wav, plain, "mp3");
        AddId3Cover(plain, tagged);

        Assert.True(HasId3Apic(tagged), "Generated MP3 fixture should contain cover art.");
        ConvertAudio(tagged, output, "mp3");
        Assert.True(HasId3Apic(output), "Converted MP3 should keep cover art.");
    }

    [Fact]
    public void FlacCoverArtSurvivesTranscode()
    {
        if (!HasNativeRuntime())
            return;

        string dir = NewTempDir();
        string wav = Path.Combine(dir, "input.wav");
        string plain = Path.Combine(dir, "plain.flac");
        string tagged = Path.Combine(dir, "tagged.flac");
        string output = Path.Combine(dir, "out.flac");

        WriteSineWave(wav);
        ConvertAudio(wav, plain, "flac");
        AddFlacPictureBlock(plain, tagged);

        Assert.True(HasFlacPictureBlock(tagged), "Generated FLAC fixture should contain cover art.");
        ConvertAudio(tagged, output, "flac");
        Assert.True(HasFlacPictureBlock(output), "Converted FLAC should keep cover art.");
    }

    [Fact]
    public void M4aOutputKeepsCoverArtWhenInputHasAttachedPicture()
    {
        if (!HasNativeRuntime())
            return;

        string dir = NewTempDir();
        string wav = Path.Combine(dir, "input.wav");
        string plain = Path.Combine(dir, "plain.mp3");
        string tagged = Path.Combine(dir, "tagged.mp3");
        string output = Path.Combine(dir, "out.m4a");

        WriteSineWave(wav);
        ConvertAudio(wav, plain, "mp3");
        AddId3Cover(plain, tagged);

        ConvertAudio(tagged, output, "m4a");
        Assert.True(HasM4aCovrAtom(output), "Converted M4A should keep cover art.");
    }

    private static bool HasNativeRuntime() =>
        File.Exists(Path.Combine(AppContext.BaseDirectory, "avcodec-62.dll")) &&
        File.Exists(Path.Combine(AppContext.BaseDirectory, "avformat-62.dll")) &&
        File.Exists(Path.Combine(AppContext.BaseDirectory, "avutil-60.dll")) &&
        File.Exists(Path.Combine(AppContext.BaseDirectory, "swresample-6.dll"));

    private static void ConvertAudio(string source, string destination, string format)
    {
        var warnings = new List<string>();
        bool ok = FFmpegConverter.Convert(
            source,
            destination,
            format,
            bitrate: 128,
            sampleRate: 44100,
            bitDepth: 16,
            CancellationToken.None,
            preserveMetadata: true,
            useFastPath: false,
            inputInfo: null,
            progress: null,
            warnings: warnings,
            channelMode: AudioChannelMode.Preserve);

        Assert.True(ok, string.Join("; ", warnings));
        Assert.True(File.Exists(destination), $"Missing output: {destination}");
        Assert.True(new FileInfo(destination).Length > 0, $"Empty output: {destination}");
    }

    private static void WriteSineWave(string path)
    {
        const int sampleRate = 44100;
        const int samples = sampleRate;
        const int dataBytes = samples * 2;

        using var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write));
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataBytes);
        writer.Write("WAVEfmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(dataBytes);

        for (int i = 0; i < samples; i++)
        {
            short value = (short)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 12000);
            writer.Write(value);
        }
    }

    private static void AddId3Cover(string source, string destination)
    {
        byte[] jpeg = CoverJpeg();
        byte[] apicBody = Concat(
            new byte[] { 0 },
            "image/jpeg"u8.ToArray(),
            new byte[] { 0, 3, 0 },
            jpeg);
        byte[] frame = Concat(
            "APIC"u8.ToArray(),
            UInt32Bytes(apicBody.Length),
            new byte[] { 0, 0 },
            apicBody);
        byte[] tag = Concat(
            "ID3"u8.ToArray(),
            new byte[] { 3, 0, 0 },
            SynchsafeBytes(frame.Length),
            frame);

        File.WriteAllBytes(destination, Concat(tag, File.ReadAllBytes(source)));
    }

    private static bool HasId3Apic(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length < 10 || bytes[0] != 'I' || bytes[1] != 'D' || bytes[2] != '3')
            return false;

        int tagSize = ((bytes[6] & 0x7f) << 21) |
            ((bytes[7] & 0x7f) << 14) |
            ((bytes[8] & 0x7f) << 7) |
            (bytes[9] & 0x7f);
        int end = Math.Min(bytes.Length, 10 + tagSize);

        for (int offset = 10; offset + 10 <= end;)
        {
            string id = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
            int frameSize = (bytes[offset + 4] << 24) |
                (bytes[offset + 5] << 16) |
                (bytes[offset + 6] << 8) |
                bytes[offset + 7];

            if (id == "APIC" && frameSize > 0)
                return true;
            if (frameSize <= 0)
                break;

            offset += 10 + frameSize;
        }

        return false;
    }

    private static void AddFlacPictureBlock(string source, string destination)
    {
        byte[] bytes = File.ReadAllBytes(source);
        Assert.True(bytes.Length > 8 && bytes[0] == 'f' && bytes[1] == 'L' && bytes[2] == 'a' && bytes[3] == 'C');

        int offset = 4;
        int lastHeaderOffset = -1;
        int insertOffset = -1;
        while (offset + 4 <= bytes.Length)
        {
            bool isLast = (bytes[offset] & 0x80) != 0;
            int length = (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
            lastHeaderOffset = offset;
            insertOffset = offset + 4 + length;
            if (isLast)
                break;
            offset = insertOffset;
        }

        Assert.True(lastHeaderOffset >= 0 && insertOffset > 0);
        bytes[lastHeaderOffset] = (byte)(bytes[lastHeaderOffset] & 0x7f);

        byte[] picture = BuildFlacPictureBlock();
        byte[] header =
        {
            0x86,
            (byte)((picture.Length >> 16) & 0xff),
            (byte)((picture.Length >> 8) & 0xff),
            (byte)(picture.Length & 0xff)
        };

        File.WriteAllBytes(destination, Concat(
            bytes.Take(insertOffset).ToArray(),
            header,
            picture,
            bytes.Skip(insertOffset).ToArray()));
    }

    private static bool HasFlacPictureBlock(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length < 8 || bytes[0] != 'f' || bytes[1] != 'L' || bytes[2] != 'a' || bytes[3] != 'C')
            return false;

        int offset = 4;
        while (offset + 4 <= bytes.Length)
        {
            bool isLast = (bytes[offset] & 0x80) != 0;
            int type = bytes[offset] & 0x7f;
            int length = (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
            if (type == 6 && length > 0)
                return true;
            offset += 4 + length;
            if (isLast)
                break;
        }

        return false;
    }

    private static bool HasM4aCovrAtom(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        byte[] needle = "covr"u8.ToArray();
        for (int i = 0; i <= bytes.Length - needle.Length; i++)
        {
            if (bytes[i] == needle[0] &&
                bytes[i + 1] == needle[1] &&
                bytes[i + 2] == needle[2] &&
                bytes[i + 3] == needle[3])
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] BuildFlacPictureBlock()
    {
        byte[] jpeg = CoverJpeg();
        using var stream = new MemoryStream();
        WriteBigEndian(stream, 3);
        WriteBigEndian(stream, "image/jpeg"u8.Length);
        stream.Write("image/jpeg"u8);
        WriteBigEndian(stream, 0);
        WriteBigEndian(stream, 1);
        WriteBigEndian(stream, 1);
        WriteBigEndian(stream, 24);
        WriteBigEndian(stream, 0);
        WriteBigEndian(stream, jpeg.Length);
        stream.Write(jpeg);
        return stream.ToArray();
    }

    private static void WriteBigEndian(Stream stream, int value)
    {
        stream.WriteByte((byte)((value >> 24) & 0xff));
        stream.WriteByte((byte)((value >> 16) & 0xff));
        stream.WriteByte((byte)((value >> 8) & 0xff));
        stream.WriteByte((byte)(value & 0xff));
    }

    private static byte[] CoverJpeg() => System.Convert.FromBase64String(
        "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAX/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAAH/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oACAEBAAEFAqf/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oACAEDAQE/ASP/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oACAECAQE/ASP/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oACAEBAAY/Aqf/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oACAEBAAE/IV//2gAMAwEAAgADAAAAEP/EFBQRAQAAAAAAAAAAAAAAAAAAABD/2gAIAQMBAT8QH//EFBQRAQAAAAAAAAAAAAAAAAAAABD/2gAIAQIBAT8QH//EFBABAQAAAAAAAAAAAAAAAAAAABD/2gAIAQEAAT8QH//Z");

    private static byte[] SynchsafeBytes(int value) =>
    [
        (byte)((value >> 21) & 0x7f),
        (byte)((value >> 14) & 0x7f),
        (byte)((value >> 7) & 0x7f),
        (byte)(value & 0x7f)
    ];

    private static byte[] UInt32Bytes(int value) =>
    [
        (byte)((value >> 24) & 0xff),
        (byte)((value >> 16) & 0xff),
        (byte)((value >> 8) & 0xff),
        (byte)(value & 0xff)
    ];

    private static byte[] Concat(params byte[][] parts)
    {
        byte[] result = new byte[parts.Sum(p => p.Length)];
        int offset = 0;
        foreach (byte[] part in parts)
        {
            Buffer.BlockCopy(part, 0, result, offset, part.Length);
            offset += part.Length;
        }

        return result;
    }

    private static string NewTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "AudioConverterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
