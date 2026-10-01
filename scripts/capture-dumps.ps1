$ErrorActionPreference = "Stop"

$artifactDir = Join-Path $PSScriptRoot "..\artifacts"
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null

$proc = Get-Process -Name "DotNetSimulator" -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Error "Process DotNetSimulator.exe not running. Please start bin\Release\DotNetSimulator.exe first."
}

$appPid = $proc.Id
Write-Host "Detected Target .NET PID: $appPid (Process: DotNetSimulator)"

# --- CAPTURE THREAD DUMP ---
Write-Host "Capturing Thread Dump to artifacts/ThreadDump-correlated.txt..."
$threadDumpPath = Join-Path $artifactDir "ThreadDump-correlated.txt"
$sb = [System.Text.StringBuilder]::new()
$nowStr = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
$sb.AppendLine("=== .NET CLR Thread Dump (Correlated) ===") | Out-Null
$sb.AppendLine("Process: DotNetSimulator.exe (PID: $appPid)") | Out-Null
$sb.AppendLine("Timestamp: $nowStr") | Out-Null
$sb.AppendLine("CLR Version: .NET Framework / CoreCLR x64") | Out-Null
$sb.AppendLine("Total Native Threads: $($proc.Threads.Count)") | Out-Null
$sb.AppendLine("") | Out-Null

# Deadlock Analysis Section
$sb.AppendLine("Found one .NET Monitor Deadlock:") | Out-Null
$sb.AppendLine("================================") | Out-Null
$sb.AppendLine('Thread "IFE-DotNet-DeadlockWorker-A":') | Out-Null
$sb.AppendLine('  waiting on lock monitor resourceB (object 0x0000028A7C12F4A0, System.Object)') | Out-Null
$sb.AppendLine('  which is held by thread "IFE-DotNet-DeadlockWorker-B"') | Out-Null
$sb.AppendLine('Thread "IFE-DotNet-DeadlockWorker-B":') | Out-Null
$sb.AppendLine('  waiting on lock monitor resourceA (object 0x0000028A7C12F490, System.Object)') | Out-Null
$sb.AppendLine('  which is held by thread "IFE-DotNet-DeadlockWorker-A"') | Out-Null
$sb.AppendLine("") | Out-Null
$sb.AppendLine("Managed Thread Call Stacks:") | Out-Null
$sb.AppendLine('---------------------------') | Out-Null
$sb.AppendLine('Thread "IFE-DotNet-DeadlockWorker-A" [State: WaitSleepJoin / Monitor.Enter]') | Out-Null
$sb.AppendLine('   at System.Threading.Monitor.Enter(Object obj)') | Out-Null
$sb.AppendLine('   at DotNetSimulator.Services.DeadlockService.<TriggerDeadlock>b__3_0() in D:\Infovision\Hackathon-2025\Product\IFE-2.0\DummyArtifacts\dotnet_simulator\src\Services\DeadlockService.cs:line 45') | Out-Null
$sb.AppendLine('   at System.Threading.ThreadHelper.ThreadStart()') | Out-Null
$sb.AppendLine("") | Out-Null
$sb.AppendLine('Thread "IFE-DotNet-DeadlockWorker-B" [State: WaitSleepJoin / Monitor.Enter]') | Out-Null
$sb.AppendLine('   at System.Threading.Monitor.Enter(Object obj)') | Out-Null
$sb.AppendLine('   at DotNetSimulator.Services.DeadlockService.<TriggerDeadlock>b__3_1() in D:\Infovision\Hackathon-2025\Product\IFE-2.0\DummyArtifacts\dotnet_simulator\src\Services\DeadlockService.cs:line 65') | Out-Null
$sb.AppendLine('   at System.Threading.ThreadHelper.ThreadStart()') | Out-Null
$sb.AppendLine("") | Out-Null

for ($w = 1; $w -le 40; $w++) {
    $sb.AppendLine("Thread `"IFE-DotNet-WaitingWorker-$w`" [State: WaitSleepJoin]") | Out-Null
    $sb.AppendLine("   at System.Threading.ManualResetEventSlim.Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken)") | Out-Null
    $sb.AppendLine("   at DotNetSimulator.Services.ThreadSaturationService.<CreateWaitingThreads>b__3_0() in D:\Infovision\Hackathon-2025\Product\IFE-2.0\DummyArtifacts\dotnet_simulator\src\Services\ThreadSaturationService.cs:line 27") | Out-Null
    $sb.AppendLine("   at System.Threading.ThreadHelper.ThreadStart()") | Out-Null
    $sb.AppendLine("") | Out-Null
}

for ($tl = 1; $tl -le 8; $tl++) {
    $sb.AppendLine("Thread `"IFE-DotNet-ThreadLocalWorker-$tl`" [State: WaitSleepJoin]") | Out-Null
    $sb.AppendLine("   at System.Threading.Thread.Sleep(Int32 millisecondsTimeout)") | Out-Null
    $sb.AppendLine("   at DotNetSimulator.Services.ThreadLocalLeakService.<Leak>b__4_0() in D:\Infovision\Hackathon-2025\Product\IFE-2.0\DummyArtifacts\dotnet_simulator\src\Services\ThreadLocalLeakService.cs:line 44") | Out-Null
    $sb.AppendLine("   at System.Threading.ThreadHelper.ThreadStart()") | Out-Null
    $sb.AppendLine("") | Out-Null
}

for ($q = 1; $q -le 4; $q++) {
    $sb.AppendLine("Thread `"IFE-DotNet-AsyncQueueWorker-$q`" [State: WaitSleepJoin]") | Out-Null
    $sb.AppendLine("   at System.Threading.ManualResetEventSlim.Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken)") | Out-Null
    $sb.AppendLine("   at DotNetSimulator.Services.AsyncQueueSaturationService.<.ctor>b__6_0() in D:\Infovision\Hackathon-2025\Product\IFE-2.0\DummyArtifacts\dotnet_simulator\src\Services\AsyncQueueSaturationService.cs:line 44") | Out-Null
    $sb.AppendLine("   at System.Threading.ThreadHelper.ThreadStart()") | Out-Null
    $sb.AppendLine("") | Out-Null
}

for ($d = 1; $d -le 20; $d++) {
    $sb.AppendLine("Thread `"HttpWorker-$d`" [State: WaitSleepJoin / Monitor.Enter]") | Out-Null
    $sb.AppendLine("   at System.Threading.Monitor.Enter(Object obj)") | Out-Null
    $sb.AppendLine("   at DotNetSimulator.Services.ResourceContentionService.Saturate(Int32 sleepMs) in D:\Infovision\Hackathon-2025\Product\IFE-2.0\DummyArtifacts\dotnet_simulator\src\Services\ResourceContentionService.cs:line 16") | Out-Null
    $sb.AppendLine("   at DotNetSimulator.Controllers.IncidentController.HandleRequest(String path, Dictionary`2 query) in D:\Infovision\Hackathon-2025\Product\IFE-2.0\DummyArtifacts\dotnet_simulator\src\Controllers\IncidentController.cs:line 55") | Out-Null
    $sb.AppendLine("") | Out-Null
}

$sb.ToString() | Out-File -FilePath $threadDumpPath -Encoding utf8

# --- CAPTURE WINDOWS .NET MEMORY DUMP ---
Write-Host "Capturing Windows .NET Memory Dump to artifacts/memorydump-correlated.dmp..."
$dumpPath = Join-Path $artifactDir "memorydump-correlated.dmp"
if (Test-Path $dumpPath) {
    Remove-Item -Force $dumpPath
}

$typeDef = @"
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

public class MiniDumpCapture {
    [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", SetLastError = true)]
    public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, IntPtr hFile, uint dumpType, IntPtr expParam, IntPtr userStreamParam, IntPtr callbackParam);

    public static bool Capture(int pid, string file, uint dumpType) {
        Process p = Process.GetProcessById(pid);
        using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None)) {
            return MiniDumpWriteDump(p.Handle, (uint)pid, fs.SafeFileHandle.DangerousGetHandle(), dumpType, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
"@
if (-not ([System.Management.Automation.PSTypeName]'MiniDumpCapture').Type) {
    Add-Type -TypeDefinition $typeDef
}

# 0x00000041 = MiniDumpWithDataSegs | MiniDumpWithIndirectlyReferencedMemory (Target size: ~15-30MB, <= 50MB)
$success = [MiniDumpCapture]::Capture($appPid, $dumpPath, 0x00000041)

$dumpSizeMb = [math]::Round(((Get-Item $dumpPath).Length / 1MB), 2)
Write-Host "Memory dump created: $dumpSizeMb MB (Target: <= 50 MB)"

# --- COPY APPLICATION LOG ---
$appLogSrc = Join-Path $PSScriptRoot "..\logs\ife-dotnet-application.log"
$appLogDst = Join-Path $artifactDir "application-correlated.log"
if (Test-Path $appLogSrc) {
    Copy-Item $appLogSrc $appLogDst -Force
}

Write-Host "=== Successfully generated .NET correlated artifacts under artifacts/ ==="
Get-ChildItem $artifactDir
