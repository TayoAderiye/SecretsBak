using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helpers;

public static class Cli
{
    public static void Ensure(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message);
    }

    public static bool IsHelp(string arg)
    {
        var a = arg.Trim();
        return a is "--help" or "-h";
    }

    public static Dictionary<string, string> ParseArgs(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith("--")) continue;

            var trimmed = a[2..];

            if (trimmed.Contains('='))
            {
                var parts = trimmed.Split('=', 2);
                dict[parts[0]] = parts[1];
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                dict[trimmed] = args[i + 1];
                i++;
            }
            else
            {
                dict[trimmed] = "true";
            }
        }

        return dict;
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""

                              secretsbak - Backup and restore .NET User Secrets to S3

                              Commands:
                                push   --project <path.csproj> --bucket <bucket> [--prefix <prefix>] [--region <region>] [--kms-key-id <kmsKey>]
                                pull   --project <path.csproj> --bucket <bucket> [--prefix <prefix>] [--region <region>]
                                where  --project <path.csproj>

                              Examples:
                                secretsbak push  --project ./MyApp.csproj --bucket my-bucket --prefix usersecrets --region us-east-1
                                secretsbak pull  --project ./MyApp.csproj --bucket my-bucket --prefix usersecrets --region us-east-1
                                secretsbak where --project ./MyApp.csproj
                                          
                          """);
    }

    public static void PrintPathFixHintIfNeeded()
    {
        if (IsDotnetToolsOnPath()) return;

        Console.WriteLine("⚠️  Your PATH does not include ~/.dotnet/tools so the command may not be found.");
        Console.WriteLine("Add this to your shell profile (zsh):");
        Console.WriteLine("  echo 'export PATH=\"$PATH:$HOME/.dotnet/tools\"' >> ~/.zshrc");
        Console.WriteLine("  source ~/.zshrc");
    }

    private static bool IsDotnetToolsOnPath()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var tools = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");
        return path.Split(Path.PathSeparator)
            .Any(p => string.Equals(p.TrimEnd('/'), tools, StringComparison.OrdinalIgnoreCase));
    }
    static async Task PullViaGithubAsync(string ownerRepo, string projectCsproj)
    {
        GithubGh.EnsureGhExists();
        GithubGh.EnsureAuthenticated();

        GithubGh.DispatchPullWorkflow(ownerRepo);
        Console.WriteLine("✅ Dispatched pull to GitHub Actions. Waiting ~20–60s then downloading artifact...");

        // (v1) simple wait; you can poll runs for completion next.
        await Task.Delay(TimeSpan.FromSeconds(25));

        var zipPath = Path.Combine(Path.GetTempPath(), "secretsbak-artifact.zip");
        GithubGh.DownloadLatestArtifact(ownerRepo, "secretsbak-secrets", zipPath);

        var extractDir = Path.Combine(Path.GetTempPath(), "secretsbak-artifact");
        if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
        Directory.CreateDirectory(extractDir);

        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);
        var downloaded = Path.Combine(extractDir, "secrets.json");
        if (!File.Exists(downloaded))
            throw new InvalidOperationException("Downloaded artifact did not contain secrets.json");

        var secretsId = DotnetUserSecrets.GetUserSecretsId(projectCsproj);
        var secretsPath = DotnetUserSecrets.GetSecretsJsonPath(secretsId);
        Directory.CreateDirectory(Path.GetDirectoryName(secretsPath)!);

        File.Copy(downloaded, secretsPath, overwrite: true);
        Console.WriteLine($"✅ Pulled secrets to {secretsPath}");
    }


    static async Task PushViaGithubAsync(string ownerRepo, string projectCsproj)
    {
        GithubGh.EnsureGhExists();
        GithubGh.EnsureAuthenticated();

        var secretsId = DotnetUserSecrets.GetUserSecretsId(projectCsproj);
        var secretsPath = DotnetUserSecrets.GetSecretsJsonPath(secretsId);

        if (!File.Exists(secretsPath))
            throw new FileNotFoundException("secrets.json not found.", secretsPath);

        var bytes = await File.ReadAllBytesAsync(secretsPath);
        var b64 = Convert.ToBase64String(bytes);

        GithubGh.DispatchPushEvent(ownerRepo, b64);

        Console.WriteLine("✅ Dispatched push to GitHub Actions (S3 upload happens in workflow).");
    }

}