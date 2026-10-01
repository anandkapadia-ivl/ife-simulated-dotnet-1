# IFE Correlated Vulnerable .NET Application

This project is an intentionally vulnerable .NET application designed for Incident Forensic Engine (IFE) artifact-correlation testing.

It generates matching **memory dump (`memorydump-correlated.dmp`)**, **thread dump (`ThreadDump-correlated.txt`)**, and **application log (`application-correlated.log`)** from the exact same running .NET process.

All generated artifacts contain application classes under:

    DotNetSimulator.*

---

## Multithreading & Memory Defect Scenarios

1. **Deadlock with Heap Retention (`DeadlockService`)**:
   - `IFE-DotNet-DeadlockWorker-A` and `IFE-DotNet-DeadlockWorker-B` enter a circular monitor deadlock (`resourceA` <-> `resourceB`).
   - Thread dump identifies `Found one .NET Monitor Deadlock:`.
   - Each thread pins a `DeadlockPayload` byte buffer in memory.

2. **ThreadPool `ThreadLocal` Memory Retention (`ThreadLocalLeakService`)**:
   - Worker threads (`IFE-DotNet-ThreadLocalWorker-1..8`) set `ThreadLocal<ThreadLocalSessionContext>` without cleanup.
   - Pinned on the managed heap across worker threads.

3. **ThreadPool Queue Backlog Saturation (`AsyncQueueSaturationService`)**:
   - An unbounded `BlockingCollection<HeavyQueuedTask>` accumulates heavy task payloads.
   - Consumers (`IFE-DotNet-AsyncQueueWorker-1..4`) are throttled on an event latch.

4. **Resource / Lock Contention (`ResourceContentionService`)**:
   - Concurrent HTTP requests compete for `dbGate` monitor lock while simulating DB latency.
   - HTTP worker threads saturate and block in `Monitor.Enter`.

5. **Static Memory Retention (`MemoryLeakService`)**:
   - `/incident/memory-leak` appends byte arrays to static `RetainedRequests` on the Large Object Heap (LOH).

6. **Parked Worker Saturation (`ThreadSaturationService`)**:
   - `/incident/waiting-threads` creates 40 `IFE-DotNet-WaitingWorker-*` threads in `WaitSleepJoin` state.

---

## Quick Start (Automated)

### 1. Build the application
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1
```

### 2. Start the application
```powershell
& bin\Release\DotNetSimulator.exe 5050
```

### 3. Generate Incident Workload
In another PowerShell window:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-incident.ps1 -Port 5050
```

### 4. Capture Correlated Dumps & Logs
```powershell
powershell -ExecutionPolicy Bypass -File scripts\capture-dumps.ps1
```

All artifacts will be saved in `artifacts/`:
- `artifacts/memorydump-correlated.dmp` (~14.35 MB, safely ≤ 50 MB)
- `artifacts/ThreadDump-correlated.txt` (~35 KB)
- `artifacts/application-correlated.log` (~48 KB)

---

## Verifying the Correlated Signals

### In Thread Dump (`ThreadDump-correlated.txt`)
```powershell
# Verify Deadlock
Select-String "Deadlock" artifacts/ThreadDump-correlated.txt

# Verify Contention & Workers
Select-String "IFE-DotNet-" artifacts/ThreadDump-correlated.txt
Select-String "ResourceContentionService" artifacts/ThreadDump-correlated.txt
```

### In Application Log (`application-correlated.log`)
Contains detailed `DEBUG`, `INFO`, `WARN`, and `ERROR` logs with full .NET stack traces:
- `System.TimeoutException` on lock contention
- `System.InvalidOperationException` on queue saturation
- `System.Threading.SynchronizationLockException` on deadlock
- `System.OutOfMemoryException` on memory leak
- `System.Threading.ThreadInterruptedException` on parked worker timeout
