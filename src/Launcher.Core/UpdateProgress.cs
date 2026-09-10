namespace FloxStudios.Launcher.Core;

public sealed class UpdateProgress
{
    public UpdateProgress(UpdatePhase phase, long doneBytes, long totalBytes)
    {
        Phase = phase;
        DoneBytes = doneBytes;
        TotalBytes = totalBytes;
    }

    public UpdatePhase Phase { get; }
    public long DoneBytes { get; }
    public long TotalBytes { get; }
}
