using System;
using System.IO;

namespace Helpers;

public static class DotnetProjects
{
    public static string FindSingleCsprojOrThrow(string dir)
    {
        var csprojs = Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly);
        return csprojs.Length switch
        {
            1 => csprojs[0],
            0 => throw new InvalidOperationException($"No .csproj found in {dir}. Pass --project /path/to/app.csproj"),
            _ => throw new InvalidOperationException($"Multiple .csproj files found in {dir}. Pass --project explicitly.")
        };
    }
}