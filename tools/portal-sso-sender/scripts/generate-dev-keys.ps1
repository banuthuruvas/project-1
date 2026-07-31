param(
    [string]$OutputDir = (Join-Path $PSScriptRoot "..\\.dev-keys")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targetDir = New-Item -ItemType Directory -Force -Path $OutputDir
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\\..\\..")).Path
$keygenProject = Join-Path $repoRoot "tools\\portal-sso-keygen\\PortalSsoKeygen.csproj"

dotnet run --project $keygenProject -- $targetDir.FullName
