using System;
using System.IO;
using System.Security.Cryptography;

namespace FloxStudios.Launcher.Core;

public static class FileHash
{
    public static string OfFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return OfStream(stream);
    }

    public static string OfStream(Stream stream)
    {
        using SHA256 sha = SHA256.Create();
        return ToHex(sha.ComputeHash(stream));
    }

    public static string OfBytes(byte[] bytes)
    {
        using SHA256 sha = SHA256.Create();
        return ToHex(sha.ComputeHash(bytes));
    }

    private static string ToHex(byte[] hash)
    {
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
}
