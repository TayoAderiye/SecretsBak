# SecretsBak

Backup and restore **.NET User Secrets** (`secrets.json`) to/from **AWS S3**.

This is useful when you work across multiple machines (macOS, Windows, Linux, EC2) and want a quick way to sync your local `.NET User Secrets`.

---

## Features

- Backup `.NET User Secrets` to S3
- Restore `.NET User Secrets` from S3
- Encrypts uploads:
  - Default: **SSE-S3 (AES256)**
  - Optional: **SSE-KMS** with `--kms-key-id`
- Can auto-create the S3 bucket (on push)
- Works on Windows / macOS / Linux

---

## Install (Global .NET Tool)

```bash
dotnet tool install --global SecretsBak
```

Update:

```bash
dotnet tool update --global SecretsBak
```

---

## Prerequisites

- .NET 8+
- AWS credentials configured (any standard method works):
  - `~/.aws/credentials`
  - environment variables
  - AWS SSO
  - IAM Role (EC2, ECS, etc.)

You also need permissions for:

- `s3:PutObject`
- `s3:GetObject`
- `s3:ListBucket`
- `s3:HeadBucket`
- `s3:CreateBucket` (only if bucket auto-create is used)

If using KMS:

- `kms:Encrypt`
- `kms:Decrypt`

---

## Commands

### `where`

Shows the `UserSecretsId` and the local secrets.json path.

```bash
secretsbak where --project ./MyApp.csproj
```

---

### `push`

Uploads your local `secrets.json` to S3.

```bash
secretsbak push --project ./MyApp.csproj --bucket my-bucket --prefix usersecrets --region us-east-1
```

Optional SSE-KMS:

```bash
secretsbak push \
  --project ./MyApp.csproj \
  --bucket my-bucket \
  --prefix usersecrets \
  --region us-east-1 \
  --kms-key-id <kms-key-arn-or-id>
```

**S3 object key format:**

```
s3://<bucket>/<prefix>/<UserSecretsId>/secrets.json
```

---

### `pull`

Downloads secrets from S3 and writes them to your local UserSecrets folder.

```bash
secretsbak pull --project ./MyApp.csproj --bucket my-bucket --prefix usersecrets --region us-east-1
```

> ⚠️ Current behavior: `pull` picks the **newest** `secrets.json` under:
>
> `s3://<bucket>/<prefix>/`
>
> It then updates your `.csproj` `<UserSecretsId>` to match what was pulled.

---

## Examples

```bash
# Push secrets
secretsbak push \
  --project /Users/tayo/Repos/PaymentGateway/BuzaPayCoreApi/BuzaPayCoreApi.csproj \
  --bucket dev-secret1s \
  --prefix BuzapayCoreApi \
  --region us-east-1

# Pull secrets (overwrites local)
secretsbak pull \
  --project /Users/tayo/Repos/PaymentGateway/BuzaPayCoreApi/BuzaPayCoreApi.csproj \
  --bucket dev-secret1s \
  --prefix BuzapayCoreApi \
  --region us-east-1
```

---

## Notes

- If you do **not** pass `--kms-key-id`, uploads still use encryption (**SSE-S3 AES256**).
- If your bucket enforces SSE-KMS by policy, you must pass `--kms-key-id`.

---

## License

MIT
