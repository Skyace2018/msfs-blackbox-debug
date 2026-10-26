# MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
# Run on the Windows PC that has the MSFS SDK. This never launches or changes the simulator.
param(
    [string]$SimConnectDll,
    [string]$SdkRoot
)

$ErrorActionPreference = 'Stop'
$repository = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$recorder = Join-Path $repository 'src\External_Recorder\External_Recorder.csproj'

if ([string]::IsNullOrWhiteSpace($SdkRoot) -and -not [string]::IsNullOrWhiteSpace($env:MSFS_SDK)) {
    $SdkRoot = $env:MSFS_SDK
}
if ([string]::IsNullOrWhiteSpace($SimConnectDll) -and -not [string]::IsNullOrWhiteSpace($SdkRoot)) {
    $SimConnectDll = Join-Path $SdkRoot 'SimConnect SDK\lib\managed\Microsoft.FlightSimulator.SimConnect.dll'
}
if ([string]::IsNullOrWhiteSpace($SimConnectDll) -or -not (Test-Path -LiteralPath $SimConnectDll -PathType Leaf)) {
    Write-Output 'BLOCKED: The official Microsoft.FlightSimulator.SimConnect.dll was not found.'
    Write-Output 'Install the MSFS SDK on the simulator PC, then pass -SimConnectDll with the exact managed DLL path.'
    exit 2
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Output 'BLOCKED: The .NET SDK is not installed or dotnet is not on PATH.'
    exit 2
}

$resolvedDll = (Resolve-Path -LiteralPath $SimConnectDll).Path
Write-Output ('Supplied SimConnect interop file: ' + $resolvedDll)
Write-Output 'Building recorder against the supplied DLL. This is a compile check, not a simulator test.'
& dotnet build $recorder -c Release '-p:EnableSimConnect=true' "-p:SimConnectInteropPath=$resolvedDll"
if ($LASTEXITCODE -ne 0) {
    Write-Output 'FAILED: SDK-enabled recorder build did not pass. Save the compiler output for the developer.'
    exit $LASTEXITCODE
}
Write-Output 'VERIFIED: recorder compiled against the supplied managed DLL. Confirm that the DLL came from the official MSFS SDK.'
Write-Output ('Recorder executable: ' + (Join-Path $repository 'src\External_Recorder\bin\Release\net8.0-windows\External_Recorder.exe'))
Write-Output 'UNKNOWN: WASM transport, live frames, aircraft values, event export, and timeout behavior require the simulator tests in docs/LIVE_TEST_PLAN.md.'
