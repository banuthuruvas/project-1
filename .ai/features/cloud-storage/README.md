# Cloud Storage (S3 + Local)

> **Status:** `optional` | **Source:** code-sentinel S3Service

## Overview

Pluggable file storage — local disk or AWS S3, switchable via `FileStorage:Provider` in appsettings.json. Uses `IFileStorageProvider` abstraction with `LocalFileStorageProvider` and `S3FileStorageProvider` implementations.

## Key Files

| Layer     | Path                                                               |
| --------- | ------------------------------------------------------------------ |
| Interface | `Services/FileStorage/IFileStorageProvider.cs`                     |
| Local     | `Services/FileStorage/LocalFileStorageProvider.cs`                 |
| S3        | `Services/FileStorage/S3FileStorageProvider.cs`                    |
| Config    | `build/appsettings.api.json` — `FileStorage:Provider` (Local / S3) |

## Config

```json
"FileStorage": {
  "Provider": "Local",      // "Local" or "S3"
  "BasePath": "/app/files",
  "S3BucketName": "...",
  "S3Region": "ap-southeast-1",
  "S3AccessKey": "...",
  "S3SecretKey": "..."
}
```
