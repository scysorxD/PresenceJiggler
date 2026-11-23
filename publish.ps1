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

$executable = Join-Path $output 'PresenceJiggler.exe'

# Explorer can keep the previous icon when an executable is overwritten in place.
if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    if (-not ('PresenceJiggler.PublishShell' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace PresenceJiggler
{
    public static class PublishShell
    {
        private const int SHCNE_UPDATEITEM = 0x00002000;
        private const uint SHCNF_PATHW = 0x0005;
        private const uint SHCNF_FLUSHNOWAIT = 0x2000;

        [DllImport("shell32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern void SHChangeNotify(int eventId, uint flags, string path, IntPtr unused);

        public static void RefreshExecutable(string path)
        {
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, path, IntPtr.Zero);
        }
    }
}
'@
    }

    [PresenceJiggler.PublishShell]::RefreshExecutable($executable)
}

Write-Host "Ejecutable: $executable"
