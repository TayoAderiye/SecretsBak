namespace Helpers.Broker;

public static class BrokerAdd
{
    public static void Run(
        string brokerRepo,          // e.g. "TayoAderiye/SecretsBak"
        string target,
        string bucket,
        string region,
        string key,
        string? kmsKeyId)
    {
        Gh.Ensure();
        Git.Ensure();

        var tmp = Path.Combine(Path.GetTempPath(), "secretsbak-broker-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        var repoUrl = $"https://github.com/{brokerRepo}.git";
        Git.Clone(repoUrl, tmp);

        var branch = $"secretsbak/add-{target}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        Git.CheckoutNewBranch(tmp, branch);

        var registryPath = Path.Combine(tmp, "registry.json");
        if (!File.Exists(registryPath))
            throw new InvalidOperationException($"registry.json not found in broker repo root. Create it once in {brokerRepo}.");

        Registry.AddOrUpdateTarget(registryPath, target, bucket, region, key, kmsKeyId);

        Git.AddCommit(tmp, $"Add broker target '{target}'");
        Git.PushSetUpstream(tmp, "origin", branch);

        // Create PR
        Proc.Run("gh", $"pr create -R {brokerRepo} --base main --head {branch} --title \"Add target {target}\" --body \"Add/update target {target} in registry.json\"", tmp);

        Console.WriteLine($"✅ Created PR in {brokerRepo} to add target '{target}'. Merge it, then pull/push will work.");
    }
}