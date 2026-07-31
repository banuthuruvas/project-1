#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

test -f src/backend/Libraries/Services/Services/FileStorage/IFileStorageProvider.cs \
  || { echo "FAIL: IFileStorageProvider.cs missing"; exit 1; }
test -f src/backend/Libraries/Services/Services/FileStorage/LocalFileStorageProvider.cs \
  || { echo "FAIL: LocalFileStorageProvider.cs missing"; exit 1; }
test -f src/backend/Libraries/Services/Services/FileStorage/S3FileStorageProvider.cs \
  || { echo "FAIL: S3FileStorageProvider.cs missing"; exit 1; }

grep -q "IFileStorageProvider" src/backend/API/Program.cs \
  || { echo "FAIL: IFileStorageProvider not registered"; exit 1; }
grep -q '"FileStorage"' build/appsettings.api.json \
  || { echo "FAIL: FileStorage config section missing"; exit 1; }

dotnet build src/backend/NieTemplate.sln

echo "verify.sh: OK (file-storage feature wired)"
