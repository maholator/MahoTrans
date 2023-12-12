namespace MahoTrans;

public class SnapshotLoadError : JavaRuntimeError
{
    public SnapshotLoadError()
    {
    }

    public SnapshotLoadError(string? message) : base(message)
    {
    }

    public SnapshotLoadError(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}