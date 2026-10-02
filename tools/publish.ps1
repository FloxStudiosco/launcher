param(
    [Parameter(Mandatory = $true)] [string]$BuildDir,
    [Parameter(Mandatory = $true)] [string]$Version,
    [Parameter(Mandatory = $true)] [string]$Exe,
    [string]$MinVersion,
    [string]$Screenshot,
    [string]$Changelog,
    [string]$LauncherExe,
    [string]$LauncherVersion,
    [string]$SiteDir,
    [string]$HostName = $env:LAUNCHER_SFTP_HOST,
    [string]$UserName = $env:LAUNCHER_SFTP_USER,
    [string]$Password = $env:LAUNCHER_SFTP_PASSWORD,
    [string]$PrivateKey = $env:LAUNCHER_SFTP_KEY,
    [string]$HostKey = $env:LAUNCHER_SFTP_HOSTKEY,
    [string]$RemoteDir = $env:LAUNCHER_SFTP_DIR,
    [switch]$NoUpload
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $SiteDir) { $SiteDir = Join-Path $repoRoot 'out\site' }
$SiteDir = [System.IO.Path]::GetFullPath($SiteDir)
if ($HostKey) { $HostKey = $HostKey -replace 'SHA256:', '' }

function Find-WinScp {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'WinSCP\WinSCP.com'),
        (Join-Path $env:ProgramFiles 'WinSCP\WinSCP.com'),
        (Join-Path $env:LOCALAPPDATA 'Programs\WinSCP\WinSCP.com')
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }
    $command = Get-Command 'WinSCP.com' -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    return $null
}

function Assert-Prerequisites {
    if (-not (Get-Command 'dotnet' -ErrorAction SilentlyContinue)) {
        throw 'dotnet not found. Install the .NET 8 SDK: winget install Microsoft.DotNet.SDK.8'
    }
    $sdks = & dotnet --list-sdks
    if (-not $sdks) {
        throw 'Only the .NET runtime is installed, the SDK is missing. Install it: winget install Microsoft.DotNet.SDK.8'
    }
    if ($NoUpload) { return }

    if (-not (Find-WinScp)) {
        throw 'WinSCP.com not found. Install it: winget install WinSCP.WinSCP'
    }
    foreach ($pair in @(@('LAUNCHER_SFTP_HOST', $HostName), @('LAUNCHER_SFTP_USER', $UserName),
                        @('LAUNCHER_SFTP_HOSTKEY', $HostKey), @('LAUNCHER_SFTP_DIR', $RemoteDir))) {
        if (-not $pair[1]) { throw "$($pair[0]) is not set" }
    }
    if (-not $Password -and -not $PrivateKey) { throw 'Set LAUNCHER_SFTP_PASSWORD or LAUNCHER_SFTP_KEY' }
}

function Invoke-ReleaseBuilder {
    $arguments = @('--build', $BuildDir, '--site', $SiteDir, '--version', $Version, '--exe', $Exe)
    if ($MinVersion) { $arguments += @('--min-version', $MinVersion) }
    if ($Screenshot) { $arguments += @('--screenshot', $Screenshot) }
    if ($Changelog) { $arguments += @('--changelog', $Changelog) }
    if ($LauncherExe) { $arguments += @('--launcher-exe', $LauncherExe, '--launcher-version', $LauncherVersion) }

    $project = Join-Path $repoRoot 'src\Launcher.Publish\Launcher.Publish.csproj'
    & dotnet run --project $project -c Release -- @arguments
    if ($LASTEXITCODE -ne 0) { throw "launcher-publish failed with exit code $LASTEXITCODE" }
}

function Invoke-Upload {
    $remote = $RemoteDir.TrimEnd('/')
    $auth = if ($PrivateKey) { "-privatekey=""$PrivateKey""" } else { '-password=%1%' }
    $script = @(
        'option batch abort',
        'option confirm off',
        "open sftp://$UserName@$HostName/ -hostkey=""$HostKey"" $auth",
        "synchronize remote -criteria=size -filemask=""|manifest.json;*.part"" ""$SiteDir"" ""$remote""",
        "put ""$(Join-Path $SiteDir 'manifest.json')"" ""$remote/""",
        'exit'
    )
    $scriptFile = Join-Path ([System.IO.Path]::GetTempPath()) "launcher-upload-$([guid]::NewGuid().ToString('N')).txt"
    Set-Content -Path $scriptFile -Value $script -Encoding UTF8
    try {
        $winscp = Find-WinScp
        $log = Join-Path $repoRoot 'out\winscp.log'
        if ($PrivateKey) {
            & $winscp /ini=nul /log=$log /script=$scriptFile
        } else {
            & $winscp /ini=nul /log=$log /script=$scriptFile /parameter // $Password
        }
        if ($LASTEXITCODE -ne 0) { throw "WinSCP failed with exit code $LASTEXITCODE, see $log" }
    }
    finally {
        Remove-Item $scriptFile -Force -ErrorAction SilentlyContinue
    }
}

Assert-Prerequisites
Invoke-ReleaseBuilder
if ($NoUpload) {
    Write-Host "Site prepared at $SiteDir (upload skipped)"
    return
}
Invoke-Upload
Write-Host "Published $Version to $HostName$RemoteDir"
