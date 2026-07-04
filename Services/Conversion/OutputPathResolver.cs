namespace AudioConverter;

internal sealed class OutputPathResolver
{
    public OutputPathResult Resolve(OutputPathRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InputPath))
            throw new ArgumentException("Input path is required.", nameof(request));

        string extension = NormalizeExtension(request.OutputExtension);
        string outputRoot = string.IsNullOrWhiteSpace(request.OutputRoot)
            ? Path.GetDirectoryName(request.InputPath) ?? string.Empty
            : request.OutputRoot!;

        string destinationDirectory = outputRoot;
        string fileName = Path.GetFileNameWithoutExtension(request.InputPath);

        if (request.PreserveFullFolderStructure)
        {
            string fullInput = Path.GetFullPath(request.InputPath);
            string inputDir = Path.GetDirectoryName(fullInput) ?? string.Empty;
            string root = Path.GetPathRoot(fullInput) ?? string.Empty;
            string rootToken = GetRootToken(root);

            if (!string.IsNullOrEmpty(rootToken))
            {
                string relativeFromRoot = string.IsNullOrEmpty(inputDir)
                    ? string.Empty
                    : Path.GetRelativePath(root, inputDir);
                if (relativeFromRoot == ".")
                    relativeFromRoot = string.Empty;

                destinationDirectory = string.IsNullOrEmpty(relativeFromRoot)
                    ? Path.Combine(outputRoot, rootToken)
                    : Path.Combine(outputRoot, rootToken, relativeFromRoot);
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.RelativePath))
        {
            string? relativeDir = Path.GetDirectoryName(request.RelativePath);
            fileName = Path.GetFileNameWithoutExtension(request.RelativePath);
            if (!string.IsNullOrEmpty(relativeDir))
                destinationDirectory = Path.Combine(outputRoot, relativeDir);
        }

        string finalPath = Path.Combine(destinationDirectory, $"{fileName}.{extension}");
        bool wouldOverwriteInput = SamePath(finalPath, request.InputPath);
        bool wouldOverwriteExisting = IsExistingOrReserved(finalPath, request.ReservedOutputPaths);
        bool renamed = false;

        if (wouldOverwriteInput)
        {
            finalPath = NextConvertedPath(destinationDirectory, fileName, extension, request.InputPath, request.ReservedOutputPaths);
            renamed = true;
        }
        else if (wouldOverwriteExisting)
        {
            if (request.ExistingFilePolicy == ExistingFilePolicy.Skip)
                throw new OutputPathConflictException($"Output already exists: {finalPath}");

            if (request.ExistingFilePolicy == ExistingFilePolicy.AutoRename)
            {
                finalPath = NextNumberedPath(destinationDirectory, fileName, extension, request.InputPath, request.ReservedOutputPaths);
                renamed = true;
            }
        }

        string tempPath = Path.Combine(
            Path.GetDirectoryName(finalPath) ?? destinationDirectory,
            $".{Path.GetFileNameWithoutExtension(finalPath)}.{Guid.NewGuid():N}.tmp.{extension}");

        return new OutputPathResult(
            Path.GetFullPath(finalPath),
            Path.GetFullPath(tempPath),
            renamed,
            wouldOverwriteInput,
            wouldOverwriteExisting);
    }

    internal static bool SamePath(string a, string b)
    {
        string fa = Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fb = Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(fa, fb, StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetRootToken(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            return string.Empty;

        string trimmed = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (trimmed.StartsWith(@"\\", StringComparison.Ordinal))
        {
            string unc = trimmed.TrimStart('\\');
            string[] parts = unc.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? Path.Combine("UNC", parts[0], parts[1]) : "UNC";
        }

        return trimmed.TrimEnd(':');
    }

    private static string NormalizeExtension(string extension) =>
        extension.Trim().TrimStart('.').ToLowerInvariant();

    private static bool IsExistingOrReserved(string path, ISet<string>? reserved) =>
        File.Exists(path) || (reserved?.Contains(Path.GetFullPath(path)) ?? false);

    private static string NextConvertedPath(string directory, string fileName, string extension, string inputPath, ISet<string>? reserved)
    {
        string first = Path.Combine(directory, $"{fileName} (converted).{extension}");
        if (!SamePath(first, inputPath) && !IsExistingOrReserved(first, reserved))
            return first;

        for (int i = 2; ; i++)
        {
            string candidate = Path.Combine(directory, $"{fileName} (converted {i}).{extension}");
            if (!SamePath(candidate, inputPath) && !IsExistingOrReserved(candidate, reserved))
                return candidate;
        }
    }

    private static string NextNumberedPath(string directory, string fileName, string extension, string inputPath, ISet<string>? reserved)
    {
        for (int i = 2; ; i++)
        {
            string candidate = Path.Combine(directory, $"{fileName} ({i}).{extension}");
            if (!SamePath(candidate, inputPath) && !IsExistingOrReserved(candidate, reserved))
                return candidate;
        }
    }
}
