namespace AudioConverter;

internal sealed record OutputPathRequest(
    string InputPath,
    string? RelativePath,
    string? OutputRoot,
    string OutputExtension,
    bool PreserveFullFolderStructure,
    ExistingFilePolicy ExistingFilePolicy,
    ISet<string>? ReservedOutputPaths = null);
