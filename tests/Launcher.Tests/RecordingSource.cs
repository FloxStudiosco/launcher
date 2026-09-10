using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FloxStudios.Launcher.Core;

namespace FloxStudios.Launcher.Tests;

internal sealed class RecordingSource : IObjectSource
{
    private readonly IObjectSource _inner;

    public RecordingSource(IObjectSource inner)
    {
        _inner = inner;
    }

    public ConcurrentBag<string> Opened { get; } = new ConcurrentBag<string>();

    public Task<Stream> OpenAsync(string relativePath, CancellationToken cancellation)
    {
        Opened.Add(relativePath);
        return _inner.OpenAsync(relativePath, cancellation);
    }
}
