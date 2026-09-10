using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FloxStudios.Launcher.Core;

public interface IObjectSource
{
    Task<Stream> OpenAsync(string relativePath, CancellationToken cancellation);
}
