using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FloxStudios.Launcher.App;

internal static class ExpectedFailures
{
    public static bool Is(Exception exception)
    {
        return exception is IOException
            || exception is HttpRequestException
            || exception is WebException
            || exception is InvalidDataException
            || exception is UnauthorizedAccessException
            || exception is TaskCanceledException
            || exception is Win32Exception
            || exception is NotSupportedException;
    }
}
