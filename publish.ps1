param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'PresenceJiggler\PresenceJiggler.csproj'
$output = Join-Path $PSScriptRoot "publish\$Runtime"

dotnet publish $project -c Release -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=embedded `
    -o $output

if ($LASTEXITCODE -ne 0) {
    throw "Falló dotnet publish (código $LASTEXITCODE)."
}

Write-Host "Ejecutable: $(Join-Path $output 'PresenceJiggler.exe')"
