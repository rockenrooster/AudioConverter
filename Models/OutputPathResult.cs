namespace AudioConverter;

internal sealed record OutputPathResult(
    string FinalOutputPath,
    string TemporaryOutputPath,
    bool WasAutoRenamed,
    bool WouldOverwriteInput,
    bool WouldOverwriteExistingOutput);
