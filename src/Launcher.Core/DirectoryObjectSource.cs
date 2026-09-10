using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FloxStudios.Launcher.Core;

public sealed class DirectoryObjectSource : IObjectSource
{
    private readonly string _root;

    public DirectoryObjectSource(string root)
    {
        _root = root;
    }

    public Task<Stream> OpenAsync(string relativePath, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        string withoutQuery = relativePath.Split('?')[0];
        Stream stream = File.OpenRead(FileOps.ToLocal(_root, withoutQuery));
        return Task.FromResult(stream);
    }
}
