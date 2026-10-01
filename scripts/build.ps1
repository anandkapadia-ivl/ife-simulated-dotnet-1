$ErrorActionPreference = "Stop"

Write-Host "=== Building .NET Simulator Application ==="

$srcDir = Join-Path $PSScriptRoot "..\src"
$binDir = Join-Path $PSScriptRoot "..\bin\Release"
New-Item -ItemType Directory -Force -Path $binDir | Out-Null

$roslynCsc = "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
$fallbackCsc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

$compiler = if (Test-Path $roslynCsc) { $roslynCsc } else { $fallbackCsc }
Write-Host "Using C# compiler: $compiler"

$files = Get-ChildItem -Recurse "$srcDir\*.cs" | Select-Object -ExpandProperty FullName
& $compiler /target:exe /out:"$binDir\DotNetSimulator.exe" /optimize+ /debug:pdbonly $files

if (Test-Path "$binDir\DotNetSimulator.exe") {
    Write-Host "Build SUCCESS: $binDir\DotNetSimulator.exe created successfully"
} else {
    Write-Error "Build failed: DotNetSimulator.exe not found"
}
