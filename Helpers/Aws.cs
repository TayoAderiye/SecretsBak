using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Helpers;

public static class Aws
{
    public static AmazonS3Client CreateS3Client(string? region)
    {
        if (string.IsNullOrWhiteSpace(region))
            return new AmazonS3Client(); // default AWS config/credentials

        var cfg = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };
        return new AmazonS3Client(cfg);
    }

    public static async Task EnsureBucketExistsOrCreateAsync(
        IAmazonS3 s3,
        string bucketName,
        string? region,
        CancellationToken ct = default)
    {
        try
        {
            await s3.HeadBucketAsync(new HeadBucketRequest { BucketName = bucketName }, ct);
            return;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                $"Bucket '{bucketName}' exists but you don't have access (or it's owned by another account).",
                ex);
        }

        var regionName = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
        var createReq = new PutBucketRequest { BucketName = bucketName };

        // us-east-1 must NOT send LocationConstraint
        if (!string.IsNullOrWhiteSpace(regionName) &&
            !string.Equals(regionName, "us-east-1", StringComparison.OrdinalIgnoreCase))
        {
            createReq.BucketRegion = S3Region.FindValue(regionName);
        }

        try
        {
            await s3.PutBucketAsync(createReq, ct);
            await WaitUntilBucketExistsAsync(s3, bucketName, ct);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyExists" or "BucketAlreadyOwnedByYou")
        {
            if (ex.ErrorCode == "BucketAlreadyExists")
                throw new InvalidOperationException(
                    $"Bucket name '{bucketName}' is already taken by another AWS account.", ex);
        }
    }

    private static async Task WaitUntilBucketExistsAsync(IAmazonS3 s3, string bucketName, CancellationToken ct)
    {
        for (var i = 0; i < 10; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await s3.HeadBucketAsync(new HeadBucketRequest { BucketName = bucketName }, ct);
                return;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromMilliseconds(400), ct);
            }
        }
    }
}