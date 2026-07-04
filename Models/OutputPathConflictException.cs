namespace AudioConverter;

internal sealed class OutputPathConflictException : InvalidOperationException
{
    public OutputPathConflictException(string message) : base(message)
    {
    }
}
