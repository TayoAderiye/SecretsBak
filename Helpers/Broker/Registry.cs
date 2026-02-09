using System.Text.Json;
using System.Text.Json.Nodes;

namespace Helpers.Broker;

public static class Registry
{
    public static void AddOrUpdateTarget(string registryPath, string target, string bucket, string region, string key, string? kmsKeyId)
    {
        var node = JsonNode.Parse(File.ReadAllText(registryPath)) as JsonObject
                   ?? new JsonObject();

        var targets = node["targets"] as JsonObject ?? new JsonObject();
        node["targets"] = targets;

        targets[target] = new JsonObject
        {
            ["bucket"] = bucket,
            ["region"] = region,
            ["key"] = key.Trim().TrimStart('/'),
            ["kmsKeyId"] = kmsKeyId ?? ""
        };

        File.WriteAllText(registryPath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }
}