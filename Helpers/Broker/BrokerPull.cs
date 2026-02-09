using System.IO.Compression;
using System.Text.Json;

namespace Helpers.Broker;

public static class BrokerPull
{
    public static async Task Run(string brokerRepo, string target, string projectCsproj, string? waitSeconds)
    {
        Gh.Ensure();

        Gh.WorkflowDispatch(
            ownerRepo: brokerRepo,
            workflowFile: "secretsbak-broker.yml",
            @ref: "main",
            inputs: new()
            {
                ["action"] = "pull",
                ["target"] = target
            });

        var delay = int.TryParse(waitSeconds, out var s) ? s : 25;
        Console.WriteLine($"✅ Dispatched pull for '{target}'. Waiting {delay}s then downloading artifact...");
        await Task.Delay(TimeSpan.FromSeconds(delay));

        // Find latest run for workflow, then artifact id
        var runsJson = Gh.Api($"repos/{brokerRepo}/actions/workflows/secretsbak-broker.yml/runs?per_page=5");
        using var doc = JsonDocument.Parse(runsJson);
        var runs = doc.RootElement.GetProperty("workflow_runs");
        if (runs.GetArrayLength() == 0) throw new InvalidOperationException("No workflow runs found.");

        var runId = runs[0].GetProperty("id").GetInt64();

        var artsJson = Gh.Api($"repos/{brokerRepo}/actions/runs/{runId}/artifacts");
        using var doc2 = JsonDocument.Parse(artsJson);
        var arts = doc2.RootElement.GetProperty("artifacts").EnumerateArray().ToList();
        var art = arts.FirstOrDefault(a => string.Equals(a.GetProperty("name").GetString(), "secretsbak-secrets", StringComparison.OrdinalIgnoreCase));
        if (art.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException("Artifact 'secretsbak-secrets' not found. Wait longer or check Actions logs.");

        var artifactId = art.GetProperty("id").GetInt64();

        var zipPath = Path.Combine(Path.GetTempPath(), "secretsbak-artifact.zip");
        var cmd = $"api -H \"Accept: application/vnd.github+json\" repos/{brokerRepo}/actions/artifacts/{artifactId}/zip > \"{zipPath}\"";
        Proc.Run("bash", $"-lc \"{cmd}\""); // uses shell redirect

        var extractDir = Path.Combine(Path.GetTempPath(), "secretsbak-artifact-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractDir);
        ZipFile.ExtractToDirectory(zipPath, extractDir);

        var downloaded = Path.Combine(extractDir, "secrets.json");
        if (!File.Exists(downloaded)) throw new InvalidOperationException("Downloaded artifact does not contain secrets.json.");

        var secretsId = DotnetUserSecrets.GetUserSecretsId(projectCsproj);
        var dest = DotnetUserSecrets.GetSecretsJsonPath(secretsId);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(downloaded, dest, overwrite: true);

        Console.WriteLine($"✅ Pulled secrets for '{target}' to: {dest}");
    }
}