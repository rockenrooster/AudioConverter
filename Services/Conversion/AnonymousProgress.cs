namespace AudioConverter;

internal sealed class AnonymousProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;

    public AnonymousProgress(Action<T> handler) => _handler = handler;

    public void Report(T value) => _handler(value);
}
