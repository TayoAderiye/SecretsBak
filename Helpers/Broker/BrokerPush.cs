namespace Helpers.Broker;

public static class BrokerPush
{
    public static async Task Run(string brokerRepo, string target, string projectCsproj)
    {
        Gh.Ensure();

        var secretsId = DotnetUserSecrets.GetUserSecretsId(projectCsproj);
        var path = DotnetUserSecrets.GetSecretsJsonPath(secretsId);

        if (!File.Exists(path))
            throw new FileNotFoundException("secrets.json not found.", path);

        var bytes = await File.ReadAllBytesAsync(path);
        var b64 = Convert.ToBase64String(bytes);

        Gh.WorkflowDispatch(
            ownerRepo: brokerRepo,
            workflowFile: "secretsbak-broker.yml",
            @ref: "main",
            inputs: new()
            {
                ["action"] = "push",
                ["target"] = target,
                ["contentBase64"] = b64
            });

        Console.WriteLine($"✅ Dispatched push for '{target}'. S3 upload will happen in broker workflow.");
    }
}