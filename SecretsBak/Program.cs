using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Amazon.S3;
using Amazon.S3.Model;
using Helpers;
using Helpers.Broker;

try
{
    Cli.PrintPathFixHintIfNeeded();

    if (args.Length == 0 || Cli.IsHelp(args[0]))
    {
        Cli.PrintHelp();
        return 1;
    }

    var command = args[0].Trim().ToLowerInvariant();
    var opts = Cli.ParseArgs(args.Skip(1).ToArray());
    //
    // var project = opts.GetValueOrDefault("project") ?? DotnetProjects.FindSingleCsprojOrThrow(Directory.GetCurrentDirectory());
    // var bucket  = opts.GetValueOrDefault("bucket");
    // var prefix  = (opts.GetValueOrDefault("prefix") ?? "usersecrets").Trim().Trim('/');
    // var region  = opts.GetValueOrDefault("region");
    // var kmsKeyId = opts.GetValueOrDefault("kms-key-id");

    switch (command)
    {
        // case "where":
        // {
        //     var secretsId = DotnetUserSecrets.GetUserSecretsId(project);
        //     var secretsPath = DotnetUserSecrets.GetSecretsJsonPath(secretsId);
        //     Console.WriteLine($"UserSecretsId: {secretsId}");
        //     Console.WriteLine($"Path: {secretsPath}");
        //     return 0;
        // }

        // case "push":
        // {
        // //     Cli.Ensure(bucket, "--bucket is required for push");
        // //
        // //     var secretsId = DotnetUserSecrets.GetUserSecretsId(project);
        // //     var secretsPath = DotnetUserSecrets.GetSecretsJsonPath(secretsId);
        // //
        // //     if (!File.Exists(secretsPath))
        // //         throw new FileNotFoundException($"secrets.json not found for secretsId '{secretsId}'.", secretsPath);
        // //
        // //     var key = S3Secrets.BuildS3Key(prefix, secretsId);
        // //
        // //     using var s3 = Aws.CreateS3Client(region);
        // //
        // //     await Aws.EnsureBucketExistsOrCreateAsync(s3, bucket!, region);
        // //
        // //     var put = new PutObjectRequest
        // //     {
        // //         BucketName = bucket!,
        // //         Key = key,
        // //         FilePath = secretsPath,
        // //         ContentType = "application/json",
        // //     };
        // //
        // //     if (!string.IsNullOrWhiteSpace(kmsKeyId))
        // //     {
        // //         put.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS;
        // //         put.ServerSideEncryptionKeyManagementServiceKeyId = kmsKeyId;
        // //     }
        // //     else
        // //     {
        // //         put.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
        // //     }
        // //
        // //     put.Metadata["userSecretsId"] = secretsId;
        // //     put.Metadata["projectFile"] = Path.GetFileName(project);
        // //     put.Metadata["uploadedAtUtc"] = DateTime.UtcNow.ToString("o");
        // //
        // //     await s3.PutObjectAsync(put);
        // //
        // //     Console.WriteLine($"✅ Pushed secrets to s3://{bucket}/{key}");
        // //     return 0;
        // // }
        // //
        // // case "pull":
        // // {
        // //     Cli.Ensure(bucket, "--bucket is required for pull");
        // //
        // //     using var s3 = Aws.CreateS3Client(region);
        // //
        // //     // Always replace local secrets with the newest secrets.json under the prefix
        // //     var latest = await S3Secrets.FindLatestSecretsObjectAsync(s3, bucket!, prefix);
        // //
        // //     if (latest is null)
        // //         throw new InvalidOperationException($"No secrets.json found under s3://{bucket}/{prefix}/");
        // //
        // //     var secretsId = S3Secrets.ExtractSecretsIdFromKey(prefix, latest.Value.Key)
        // //         ?? throw new InvalidOperationException($"Could not parse secretsId from key '{latest.Value.Key}'");
        // //
        // //     DotnetUserSecrets.SetUserSecretsId(project, secretsId);
        // //
        // //     var secretsPath = DotnetUserSecrets.GetSecretsJsonPath(secretsId);
        // //     Directory.CreateDirectory(Path.GetDirectoryName(secretsPath)!);
        // //
        // //     using var resp = await s3.GetObjectAsync(new GetObjectRequest
        // //     {
        // //         BucketName = bucket!,
        // //         Key = latest.Value.Key
        // //     });
        // //
        // //     await resp.WriteResponseStreamToFileAsync(secretsPath, false, CancellationToken.None);
        // //
        // //     Console.WriteLine($"✅ Pulled secrets to {secretsPath}");
        // //     Console.WriteLine($"✅ Using S3 key: s3://{bucket}/{latest.Value.Key}");
        // //     Console.WriteLine($"✅ Updated {Path.GetFileName(project)} UserSecretsId to: {secretsId}");
        // //     return 0;
        // // }
        case "broker":
        {
            var sub = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "";
            var opts2 = Cli.ParseArgs(args.Skip(2).ToArray());

            var brokerRepo = opts2.GetValueOrDefault("broker-repo") ?? "SecretsBak";
            var target = opts2.GetValueOrDefault("target");

            switch (sub)
            {
                case "add":
                    Cli.Ensure(target, "--target required");
                    var bucket = opts2.GetValueOrDefault("bucket"); Cli.Ensure(bucket, "--bucket required");
                    var region = opts2.GetValueOrDefault("region"); Cli.Ensure(region, "--region required");
                    var key = opts2.GetValueOrDefault("key"); Cli.Ensure(key, "--key required");
                    var kms = opts2.GetValueOrDefault("kms-key-id");
                    BrokerAdd.Run(brokerRepo, target!, bucket!, region!, key!, kms);
                    return 0;

                case "pull":
                    Cli.Ensure(target, "--target required");
                    var project = opts2.GetValueOrDefault("project") ?? DotnetProjects.FindSingleCsprojOrThrow(Directory.GetCurrentDirectory());
                    var wait = opts2.GetValueOrDefault("wait-seconds");
                    await BrokerPull.Run(brokerRepo, target!, project, wait);
                    return 0;

                case "push":
                    Cli.Ensure(target, "--target required");
                    var project2 = opts2.GetValueOrDefault("project") ?? DotnetProjects.FindSingleCsprojOrThrow(Directory.GetCurrentDirectory());
                    await BrokerPush.Run(brokerRepo, target!, project2);
                    return 0;

                default:
                    Console.Error.WriteLine("Usage: secretsbak broker <add|pull|push> ...");
                    return 2;
            }
        }


        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            Cli.PrintHelp();
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("❌ Error:");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(ex);
    return 99;
}