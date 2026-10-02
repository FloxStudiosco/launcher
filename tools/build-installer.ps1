param(
    [string]$ReleaseUrl,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $OutputDir) { $OutputDir = Join-Path $repoRoot 'out\installer' }
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
$project = Join-Path $repoRoot 'src\Launcher.App\Launcher.App.csproj'
$script = Join-Path $repoRoot 'installer\FloxLauncher.iss'

function Find-Iscc {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }
    $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    return $null
}

$iscc = Find-Iscc
if (-not $iscc) { throw 'ISCC.exe not found. Install Inno Setup 6: winget install JRSoftware.InnoSetup' }
if (-not (Get-Command 'dotnet' -ErrorAction SilentlyContinue)) {
    throw 'dotnet not found. Install the .NET 8 SDK: winget install Microsoft.DotNet.SDK.8'
}

$buildArguments = @('build', $project, '-c', 'Release', '-nologo', '-v', 'q')
if ($ReleaseUrl) { $buildArguments += "-p:LauncherReleaseUrl=$ReleaseUrl" }
& dotnet @buildArguments
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

$exe = Join-Path $repoRoot 'src\Launcher.App\bin\Release\net48\FloxLauncher.exe'
$version = (Get-Item $exe).VersionInfo.ProductVersion.Split('+')[0]

& $iscc "/DAppVersion=$version" "/DSourceExe=$exe" "/DOutputDir=$OutputDir" $script
if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE" }

Join-Path $OutputDir "KintsugiMaster-Setup-$version.exe"
