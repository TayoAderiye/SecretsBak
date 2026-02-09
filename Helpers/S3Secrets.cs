using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace Helpers;

public static class S3Secrets
{
    public static string BuildS3Key(string prefix, string secretsId)
    {
        return string.IsNullOrWhiteSpace(prefix)
            ? $"{secretsId}/secrets.json"
            : $"{prefix.Trim().Trim('/')}/{secretsId}/secrets.json";
    }

    public static async Task<(string Key, DateTime LastModified)?> FindLatestSecretsObjectAsync(
        IAmazonS3 s3,
        string bucket,
        string prefix,
        CancellationToken ct = default)
    {
        var basePrefix = string.IsNullOrWhiteSpace(prefix) ? "" : prefix.Trim().Trim('/') + "/";

        string? token = null;
        (string Key, DateTime LastModified)? best = null;

        do
        {
            var list = await s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = basePrefix,
                ContinuationToken = token
            }, ct);

            foreach (var obj in list.S3Objects)
            {
                if (!obj.Key.EndsWith("/secrets.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                var lm = obj.LastModified;

                if (best is null || lm > best.Value.LastModified)
                    best = (obj.Key, (DateTime)lm!);
            }

            token = (bool)list.IsTruncated! ? list.NextContinuationToken : null;
        } while (token is not null);

        return best;
    }

    public static string? ExtractSecretsIdFromKey(string prefix, string key)
    {
        var basePrefix = string.IsNullOrWhiteSpace(prefix) ? "" : prefix.Trim().Trim('/') + "/";
        if (!key.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        // key: "{basePrefix}{secretsId}/secrets.json"
        var rest = key.Substring(basePrefix.Length);
        var parts = rest.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[0] : null;
    }
}