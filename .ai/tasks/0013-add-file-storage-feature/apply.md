# Task 0013 — Add File Storage Abstraction

> **Status:** scaffolded — `runOnClone: true`. New derived repos benefit from a clean abstraction even before they need S3.

> **Why:** Most derived repos write files directly to disk via `File.WriteAllBytesAsync`, then later need S3 for production. This task front-loads the abstraction so the migration is a config change, not a refactor.

## Pre-checks

```bash
test ! -f src/backend/Libraries/Services/Services/FileStorage/IFileStorageProvider.cs \
  || { echo "Already added; skipping."; exit 0; }
```

## 1. Files to create

```text
src/backend/Libraries/Services/Services/FileStorage/IFileStorageProvider.cs
src/backend/Libraries/Services/Services/FileStorage/LocalFileStorageProvider.cs
src/backend/Libraries/Services/Services/FileStorage/S3FileStorageProvider.cs
```

## 2. Files to edit

### `src/backend/API/Program.cs`

Register based on configuration so swapping providers is config-only:

```diff
+ var storageProvider = configuration["FileStorage:Provider"] ?? "Local";
+ if (string.Equals(storageProvider, "S3", StringComparison.OrdinalIgnoreCase))
+ {
+     builder.Services.AddSingleton<IFileStorageProvider, S3FileStorageProvider>();
+ }
+ else
+ {
+     builder.Services.AddSingleton<IFileStorageProvider, LocalFileStorageProvider>();
+ }
```

**Why:** singleton because both providers are stateless after configuration.

### `build/appsettings.api.json`

Add the configuration section. **Do not commit real S3 credentials** — leave secrets blank and resolve via environment variables / secret store.

```diff
+ "FileStorage": {
+   "Provider": "Local",
+   "BasePath": "/app/files",
+   "S3BucketName": "",
+   "S3Region": "ap-southeast-1",
+   "S3AccessKey": "",
+   "S3SecretKey": ""
+ }
```

## 3. Verification

```bash
dotnet build src/backend/NieTemplate.sln
grep -n "IFileStorageProvider" src/backend/API/Program.cs   # ≥1
grep -n '"FileStorage"' build/appsettings.api.json           # ≥1
```

Live smoke (Local mode):

```bash
mkdir -p /tmp/storage-test && rm -f /tmp/storage-test/probe.txt
# Drive whichever endpoint already uses IFileStorageProvider in your repo;
# expect a probe file in BasePath after the upload.
```

S3 smoke: configure `FileStorage:Provider=S3` against a non-prod bucket, repeat.

## 4. Rollback

```bash
git restore --staged --worktree \
  src/backend/Libraries/Services/Services/FileStorage/ \
  src/backend/API/Program.cs \
  build/appsettings.api.json
```

## Maintainer review checklist before promoting to a release

- [ ] S3 provider validated against AWS SDK 4.x (or pinned to current `AWSSDK.S3` in csproj)
- [ ] Server-side encryption (SSE-S3 or SSE-KMS) enabled in `S3FileStorageProvider.UploadAsync`
- [ ] Multipart upload threshold set for files > 8 MB
- [ ] `LocalFileStorageProvider` rejects path traversal (`..`, absolute paths) — covered by tests
- [ ] Existing direct `File.*` IO callers in the template migrated to `IFileStorageProvider` (audit grep)
