# Expected RCA / Validation Contract (.NET Application)

## 1. Multi-Threaded Deadlock (Thread Dump + Memory Dump)
- **Defect**: Deterministic circular monitor dependency in `DotNetSimulator.Services.DeadlockService`.
- **Thread Dump Evidence (`ThreadDump-correlated.txt`)**:
  - `Found one .NET Monitor Deadlock:`
  - `IFE-DotNet-DeadlockWorker-A` is waiting on lock monitor `resourceB` (object `0x0000028A7C12F4A0`, `System.Object`) held by `IFE-DotNet-DeadlockWorker-B`.
  - `IFE-DotNet-DeadlockWorker-B` is waiting on lock monitor `resourceA` (object `0x0000028A7C12F490`, `System.Object`) held by `IFE-DotNet-DeadlockWorker-A`.
  - Stack traces point directly to `DotNetSimulator.Services.DeadlockService.<TriggerDeadlock>b__3_0()` and `b__3_1()`.
- **Memory Dump Evidence (`memorydump-correlated.dmp`)**:
  - `DotNetSimulator.Services.DeadlockService+DeadlockPayload` pinned in memory by active worker thread stacks.
  - Retained `byte[]` buffers associated with each deadlocked thread.

## 2. ThreadPool `ThreadLocal` Memory Retention (Thread Pool + Memory Dump)
- **Defect**: Storing `ThreadLocalSessionContext` in `ThreadLocal<T>` without invoking `Dispose()` or value cleanup across worker threads in `ThreadLocalLeakService`.
- **Thread Dump Evidence**:
  - Pool worker threads `IFE-DotNet-ThreadLocalWorker-1` through `8` alive in `WaitSleepJoin` state.
- **Memory Dump Evidence**:
  - Retained instances of `ThreadLocalLeakService+ThreadLocalSessionContext` holding `byte[]` arrays on the managed heap.

## 3. Unbounded Queue Saturation with Throttled Consumers
- **Defect**: `BlockingCollection<HeavyQueuedTask>` in `AsyncQueueSaturationService` where background consumer threads are throttled on a manual reset event.
- **Thread Dump Evidence**:
  - Worker threads `IFE-DotNet-AsyncQueueWorker-1` through `4` waiting on `ManualResetEventSlim.Wait()`.
- **Memory Dump Evidence**:
  - `HeavyQueuedTask` backlog retained within `BlockingCollection` nodes on the managed heap.

## 4. Resource / DB Contention
- **Defect**: Monopolized synchronization on `ResourceContentionService.dbGate` with simulated latency.
- **Thread Dump Evidence**:
  - HTTP request worker threads `HttpWorker-1..20` blocked in `Monitor.Enter` waiting for `dbGate`.

## 5. Managed Static Memory Retention
- **Defect**: Static accumulation list in `MemoryLeakService.RetainedRequests`.
- **Memory Dump Evidence**:
  - Pinned `byte[]` arrays in the Large Object Heap (LOH) / managed heap retained by static `RetainedRequests`.

## 6. Parked Worker Saturation
- **Defect**: Spawned worker threads in `ThreadSaturationService`.
- **Thread Dump Evidence**:
  - 40 threads named `IFE-DotNet-WaitingWorker-1` through `40` in `WaitSleepJoin` on `ManualResetEventSlim`.

## Correlation Requirement
All artifacts (`memorydump-correlated.dmp`, `ThreadDump-correlated.txt`, `application-correlated.log`) are generated from the same running .NET process PID under active workload.
Memory dump is calibrated to stay safely under 50 MB (actual: ~14.35 MB).
