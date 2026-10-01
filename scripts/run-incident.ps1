param(
    [int]$Port = 5050,
    [int]$MemoryIterations = 5,
    [int]$MemoryMb = 1,
    [int]$WaitingThreads = 40,
    [int]$DbRequests = 20,
    [int]$DbSleepMs = 30000,
    [int]$DeadlockPayloadMb = 1,
    [int]$ThreadLocalMb = 1,
    [int]$QueueTasks = 5,
    [int]$QueueTaskMb = 1
)

$baseUrl = "http://localhost:$Port"

Write-Host "=== Starting .NET Incident Generation on $baseUrl (Target Heap <= 50MB) ==="

Write-Host "1. Generating managed memory retention ($MemoryIterations x ${MemoryMb}MB = $($MemoryIterations * $MemoryMb)MB)..."
1..$MemoryIterations | ForEach-Object {
    Invoke-RestMethod -Method Post -Uri "$baseUrl/incident/memory-leak?megabytes=$MemoryMb" | Out-Null
}

Write-Host "2. Generating $WaitingThreads waiting worker threads (5 min wait)..."
Invoke-RestMethod -Method Post -Uri "$baseUrl/incident/waiting-threads?count=$WaitingThreads&waitMs=300000" | Out-Null

Write-Host "3. Generating multithreaded Deadlock (IFE-DotNet-DeadlockWorker-A <-> IFE-DotNet-DeadlockWorker-B)..."
Invoke-RestMethod -Method Post -Uri "$baseUrl/incident/deadlock?payloadMb=$DeadlockPayloadMb" | Out-Null

Write-Host "4. Generating ThreadLocal memory retention across pool workers (8 x ${ThreadLocalMb}MB)..."
Invoke-RestMethod -Method Post -Uri "$baseUrl/incident/threadlocal-leak?count=8&megabytes=$ThreadLocalMb" | Out-Null

Write-Host "5. Generating queue backlog saturation ($QueueTasks x ${QueueTaskMb}MB)..."
Invoke-RestMethod -Method Post -Uri "$baseUrl/incident/queue-saturation?count=$QueueTasks&megabytes=$QueueTaskMb" | Out-Null

Write-Host "6. Generating $DbRequests concurrent resource contention requests..."
1..$DbRequests | ForEach-Object {
    Start-Job -ScriptBlock {
        param($url)
        try { Invoke-RestMethod -Method Post -Uri $url } catch {}
    } -ArgumentList "$baseUrl/incident/db-contention?sleepMs=$DbSleepMs" | Out-Null
}

$status = Invoke-RestMethod "$baseUrl/incident/status"
Write-Host "Incident workload started successfully."
Write-Host "Current status: $status"
Write-Host "Now capture dumps using scripts/capture-dumps.ps1."
