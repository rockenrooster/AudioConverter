namespace AudioConverter;

internal sealed record ConversionResult(
    bool Success,
    string? ErrorMessage,
    IReadOnlyList<string> Warnings,
    TimeSpan? Duration,
    long? OutputBytes)
{
    public static ConversionResult Failed(string message, IReadOnlyList<string>? warnings = null) =>
        new(false, message, warnings ?? Array.Empty<string>(), null, null);

    public static ConversionResult Succeeded(IReadOnlyList<string>? warnings = null, TimeSpan? duration = null, long? outputBytes = null) =>
        new(true, null, warnings ?? Array.Empty<string>(), duration, outputBytes);
}
