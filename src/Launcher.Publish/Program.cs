using System;
using System.Collections.Generic;
using System.Linq;
using FloxStudios.Launcher.Core;

namespace FloxStudios.Launcher.Publish;

internal static class Program
{
    private const string USAGE =
        "launcher-publish --build <dir> --site <dir> --version <v> --exe <file> " +
        "[--min-version <v>] [--screenshot <file>] [--changelog <file>] " +
        "[--launcher-exe <file> --launcher-version <v>]";

    private static readonly string[] REQUIRED = { "build", "site", "version", "exe" };

    private static int Main(string[] args)
    {
        Dictionary<string, string>? options = ParseOptions(args);
        if (options == null || REQUIRED.Any(key => !options.ContainsKey(key)))
        {
            Console.Error.WriteLine(USAGE);
            return 2;
        }
        try
        {
            Manifest manifest = ReleaseBuilder.Build(ToRequest(options));
            Report(manifest);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static Dictionary<string, string>? ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--") || i + 1 >= args.Length)
                return null;
            options[args[i].Substring(2)] = args[i + 1];
        }
        return options;
    }

    private static ReleaseRequest ToRequest(Dictionary<string, string> options)
    {
        return new ReleaseRequest
        {
            BuildDirectory = options["build"],
            SiteDirectory = options["site"],
            Version = options["version"],
            Exe = options["exe"],
            MinVersion = Optional(options, "min-version"),
            Screenshot = Optional(options, "screenshot"),
            Changelog = Optional(options, "changelog"),
            LauncherExe = Optional(options, "launcher-exe"),
            LauncherVersion = Optional(options, "launcher-version"),
        };
    }

    private static string? Optional(Dictionary<string, string> options, string key)
    {
        return options.TryGetValue(key, out string? value) ? value : null;
    }

    private static void Report(Manifest manifest)
    {
        long size = manifest.Game.Files.Sum(file => file.Size);
        long packed = manifest.Game.Files.Sum(file => file.PackedSize);
        Console.WriteLine($"version={manifest.Game.Version}");
        Console.WriteLine($"minVersion={manifest.Game.MinVersion}");
        Console.WriteLine($"files={manifest.Game.Files.Count}");
        Console.WriteLine($"bytes={size}");
        Console.WriteLine($"packedBytes={packed}");
        Console.WriteLine($"launcher={manifest.Launcher?.Version ?? "-"}");
    }
}
