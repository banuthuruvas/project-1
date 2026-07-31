param(
    [int]$AuthPort = 5001,
    [int]$ExchangePort = 5210,
    [string]$ReturnUrl = "http://localhost:8002/",
    [string]$PortalIssuer = "https://portal.local",
    [string]$PortalAudience = "nietemplate-auth",
    [string]$SourceSystemId = "portal-app",
    [string]$SourceUrl = "https://portal.local/apps/nietemplate"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\\..\\..")).Path
$keysDir = (Join-Path $PSScriptRoot "..\\.dev-keys")
$senderProject = Join-Path $repoRoot "tools\\portal-sso-sender\\PortalSsoSender.csproj"
$senderConfigPath = Join-Path $repoRoot "tools\\portal-sso-sender\\appsettings.json"
$exchangeProject = Join-Path $repoRoot "tools\\mock-sso-exchange\\MockSsoExchange.csproj"
$authProject = Join-Path $repoRoot "src\\backend\\Auth\\Auth.csproj"
$logDir = Join-Path $PSScriptRoot "..\\.logs"
$null = New-Item -ItemType Directory -Force -Path $logDir

$exchangeLog = Join-Path $logDir "mock-sso-exchange.log"
$exchangeErrorLog = Join-Path $logDir "mock-sso-exchange.err.log"
$authLog = Join-Path $logDir "auth-sso-test.log"
$authErrorLog = Join-Path $logDir "auth-sso-test.err.log"
$powershellExe = (Get-Process -Id $PID).Path

if (-not (Test-Path (Join-Path $keysDir "portal-signing-private.pem"))) {
    & (Join-Path $PSScriptRoot "generate-dev-keys.ps1") -OutputDir $keysDir
}

$portalPrivateKeyPath = Join-Path $keysDir "portal-signing-private.pem"
$portalPublicKeyPath = Join-Path $keysDir "portal-signing-public.pem"
$authPrivateKeyPath = Join-Path $keysDir "auth-decryption-private.pem"
$authPublicKeyPath = Join-Path $keysDir "auth-decryption-public.pem"

$exchangeProcess = $null
$authProcess = $null

function Get-FreeTcpPort {
    param([int]$StartPort)

    $portsInUse = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().
        GetActiveTcpListeners().
        Port

    $port = $StartPort
    while ($portsInUse -contains $port) {
        $port++
    }

    return $port
}

function Wait-ForTcpPort {
    param(
        [int]$Port,
        [int]$Attempts = 40
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        $client = $null
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $client.Connect("127.0.0.1", $Port)
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
        finally {
            if ($client) {
                $client.Dispose()
            }
        }
    }

    throw "Timed out waiting for TCP port $Port"
}

try {
    $ExchangePort = Get-FreeTcpPort -StartPort $ExchangePort
    $AuthPort = Get-FreeTcpPort -StartPort $AuthPort

    Write-Host "Using Exchange port $ExchangePort"
    Write-Host "Using Auth port $AuthPort"
    Write-Host "Starting mock exchange API..."
    Remove-Item $exchangeLog -ErrorAction SilentlyContinue
    Remove-Item $exchangeErrorLog -ErrorAction SilentlyContinue
    Remove-Item $authLog -ErrorAction SilentlyContinue
    Remove-Item $authErrorLog -ErrorAction SilentlyContinue

    $exchangeProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $exchangeProject, "--urls", "http://localhost:$ExchangePort") `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $exchangeLog `
        -RedirectStandardError $exchangeErrorLog `
        -PassThru

    Wait-ForTcpPort -Port $ExchangePort

    Write-Host "Starting Auth API with Portal SSO enabled..."
    $authCommand = @"
`$env:PortalSso__Enabled = 'true'
`$env:PortalSso__LaunchUrlTemplate = 'https://portal.local/sso/launch?state={state}&nonce={nonce}&returnUrl={returnUrl}&callbackUrl={callbackUrl}'
`$env:PortalSso__DefaultReturnUrl = '$ReturnUrl'
`$env:PortalSso__CallbackUrl = 'http://localhost:$AuthPort/api/Auth/SsoCallback'
`$env:PortalSso__Issuer = '$PortalIssuer'
`$env:PortalSso__Audience = '$PortalAudience'
`$env:PortalSso__SourceSystemId = '$SourceSystemId'
`$env:PortalSso__AllowedSourceUrls__0 = '$SourceUrl'
`$env:PortalSso__Crypto__DecryptionPrivateKeyPath = '$authPrivateKeyPath'
`$env:PortalSso__Crypto__SigningPublicKeyPath = '$portalPublicKeyPath'
`$env:PortalSso__ExchangeApi__BaseUrl = 'http://localhost:$ExchangePort'
`$env:PortalSso__ExchangeApi__Path = '/api/sso/exchange'
dotnet run --project '$authProject' --urls 'http://localhost:$AuthPort'
"@

    $authProcess = Start-Process `
        -FilePath $powershellExe `
        -ArgumentList @("-NoProfile", "-Command", $authCommand) `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $authLog `
        -RedirectStandardError $authErrorLog `
        -PassThru

    Wait-ForTcpPort -Port $AuthPort

    Write-Host "Calling Auth/SsoStart..."
    $startResponse = Invoke-RestMethod -Uri "http://localhost:$AuthPort/api/Auth/SsoStart?returnUrl=$([Uri]::EscapeDataString($ReturnUrl))"

    Write-Host "Sending signed and encrypted callback..."
    $senderConfig = [ordered]@{
        CallbackUrl = "http://localhost:$AuthPort/api/Auth/SsoCallback"
        ReturnUrl = $ReturnUrl
        Issuer = $PortalIssuer
        Audience = $PortalAudience
        SourceSystemId = $SourceSystemId
        SourceUrl = $SourceUrl
        State = $startResponse.state
        Nonce = $startResponse.nonce
        ExchangeToken = "local-sso-test-token"
        UserName = "portal.user"
        Email = "portal.user@nie.edu.sg"
        Subject = "portal.user"
        LifetimeMinutes = 5
        PortalSigningPrivateKeyPath = $portalPrivateKeyPath
        AuthEncryptionPublicKeyPath = $authPublicKeyPath
    }

    $senderConfig | ConvertTo-Json -Depth 5 | Set-Content -Path $senderConfigPath -Encoding ascii

    try {
        dotnet run --project $senderProject --no-build -- --config $senderConfigPath
        if ($LASTEXITCODE -ne 0) {
            throw "Portal SSO sender failed."
        }
    }
    finally {
        Remove-Item $senderConfigPath -ErrorAction SilentlyContinue
    }

    Write-Host "Finalizing SSO login..."
    $finalizeResponse = Invoke-RestMethod -Uri "http://localhost:$AuthPort/api/Auth/SsoFinalize?state=$([Uri]::EscapeDataString($startResponse.state))"
    if ($finalizeResponse.isAuthenticated -ne $true) {
        throw "SSO finalize did not return an authenticated login."
    }

    $verifyResponse = Invoke-RestMethod -Uri "http://localhost:$AuthPort/api/Auth/Verify?sessionToken=$([Uri]::EscapeDataString($finalizeResponse.sessionToken))"
    if ($verifyResponse.isValid -ne $true) {
        throw "Issued session token failed verification."
    }

    Write-Host ""
    Write-Host "Local SSO test passed."
    Write-Host "  UserId      : $($finalizeResponse.userId)"
    Write-Host "  SessionToken: $($finalizeResponse.sessionToken)"
}
catch {
    Write-Error $_
    throw
}
finally {
    if ($authProcess -and -not $authProcess.HasExited) {
        Stop-Process -Id $authProcess.Id -Force
    }

    if ($exchangeProcess -and -not $exchangeProcess.HasExited) {
        Stop-Process -Id $exchangeProcess.Id -Force
    }
}
