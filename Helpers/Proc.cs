using System.Diagnostics;

namespace Helpers;


public static class Proc
{
    public static string Run(string file, string args, string? workDir = null)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            WorkingDirectory = workDir ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {file}");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (p.ExitCode != 0)
            throw new InvalidOperationException($"Command failed: {file} {args}\n{stderr}");

        return stdout.Trim();
    }
}