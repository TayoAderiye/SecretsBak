using System.Diagnostics;
using System.Text.Json;

namespace Helpers;

public sealed class GithubGh
{
    public static void EnsureGhExists()
    {
        try { Run("gh", "--version"); }
        catch { throw new InvalidOperationException("GitHub CLI 'gh' is required. Install it, then run: gh auth login"); }
    }

    public static void EnsureAuthenticated()
    {
        var outp = Run("gh", "auth status -t");
        if (!outp.Contains("Logged in", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Not authenticated to GitHub. Run: gh auth login");
    }

    public static void CreateOrUpdateSecret(string ownerRepo, string name, string value)
    {
        // gh secret set NAME -R owner/repo -b "value"
        Run("gh", $"secret set {name} -R {ownerRepo} -b {EscapeForShell(value)}");
    }

    public static void DispatchPullWorkflow(string ownerRepo)
    {
        // workflow_dispatch: POST /repos/{owner}/{repo}/actions/workflows/secretsbak.yml/dispatches
        // easiest: use gh api
        Run("gh", $"api -X POST repos/{ownerRepo}/actions/workflows/secretsbak.yml/dispatches -f ref=main -f inputs[action]=pull");
    }

    public static void DispatchPushEvent(string ownerRepo, string contentBase64)
    {
        // repository_dispatch event with payload
        var payload = JsonSerializer.Serialize(new { contentBase64 });
        Run("gh", $"api -X POST repos/{ownerRepo}/dispatches -f event_type=secretsbak_push -f client_payload={EscapeForShell(payload)}");
    }

    public static string DownloadLatestArtifact(string ownerRepo, string artifactName, string outZipPath)
    {
        // Find latest run and artifact via gh api.
        // 1) list workflow runs for secretsbak.yml
        var runsJson = Run("gh", $"api repos/{ownerRepo}/actions/workflows/secretsbak.yml/runs?per_page=10");
        using var doc = JsonDocument.Parse(runsJson);
        var runs = doc.RootElement.GetProperty("workflow_runs");
        if (runs.GetArrayLength() == 0) throw new InvalidOperationException("No workflow runs found.");

        var runId = runs[0].GetProperty("id").GetInt64();

        // 2) list artifacts for that run
        var artsJson = Run("gh", $"api repos/{ownerRepo}/actions/runs/{runId}/artifacts");
        using var doc2 = JsonDocument.Parse(artsJson);
        var arts = doc2.RootElement.GetProperty("artifacts").EnumerateArray();

        long? artifactId = null;
        foreach (var a in arts)
        {
            if (string.Equals(a.GetProperty("name").GetString(), artifactName, StringComparison.OrdinalIgnoreCase))
            {
                artifactId = a.GetProperty("id").GetInt64();
                break;
            }
        }
        if (artifactId is null) throw new InvalidOperationException($"Artifact '{artifactName}' not found on latest run.");

        // 3) download artifact zip
        Run("gh", $"api -X GET repos/{ownerRepo}/actions/artifacts/{artifactId}/zip > {outZipPath}");
        return outZipPath;
    }

    private static string Run(string file, string args)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (p.ExitCode != 0)
            throw new InvalidOperationException($"Command failed: {file} {args}\n{stderr}");

        return stdout;
    }

    private static string EscapeForShell(string s)
    {
        // Simple single-quote shell escape
        return "'" + s.Replace("'", "'\"'\"'") + "'";
    }
}