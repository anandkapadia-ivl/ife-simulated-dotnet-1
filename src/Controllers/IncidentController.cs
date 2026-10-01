using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using DotNetSimulator.Services;

namespace DotNetSimulator.Controllers
{
    public class IncidentController
    {
        private readonly MemoryLeakService memory;
        private readonly DeadlockService deadlock;
        private readonly ThreadSaturationService threads;
        private readonly ThreadLocalLeakService threadLocal;
        private readonly AsyncQueueSaturationService queue;
        private readonly ResourceContentionService contention;

        public IncidentController(
            MemoryLeakService memory,
            DeadlockService deadlock,
            ThreadSaturationService threads,
            ThreadLocalLeakService threadLocal,
            AsyncQueueSaturationService queue,
            ResourceContentionService contention)
        {
            this.memory = memory;
            this.deadlock = deadlock;
            this.threads = threads;
            this.threadLocal = threadLocal;
            this.queue = queue;
            this.contention = contention;
        }

        public string HandleRequest(string path, Dictionary<string, string> query)
        {
            switch (path.ToLowerInvariant())
            {
                case "/incident/memory-leak":
                    int mb = query.ContainsKey("megabytes") ? int.Parse(query["megabytes"]) : 1;
                    return memory.Leak(mb);

                case "/incident/waiting-threads":
                    int count = query.ContainsKey("count") ? int.Parse(query["count"]) : 40;
                    int waitMs = query.ContainsKey("waitms") ? int.Parse(query["waitms"]) : 300000;
                    return threads.CreateWaitingThreads(count, waitMs);

                case "/incident/deadlock":
                    int payloadMb = query.ContainsKey("payloadmb") ? int.Parse(query["payloadmb"]) : 1;
                    return deadlock.TriggerDeadlock(payloadMb);

                case "/incident/threadlocal-leak":
                    int tlMb = query.ContainsKey("megabytes") ? int.Parse(query["megabytes"]) : 1;
                    int tlCount = query.ContainsKey("count") ? int.Parse(query["count"]) : 8;
                    return threadLocal.Leak(tlCount, tlMb);

                case "/incident/queue-saturation":
                    int qCount = query.ContainsKey("count") ? int.Parse(query["count"]) : 5;
                    int qMb = query.ContainsKey("megabytes") ? int.Parse(query["megabytes"]) : 1;
                    return queue.EnqueueHeavyTasks(qCount, qMb);

                case "/incident/db-contention":
                    int sleepMs = query.ContainsKey("sleepms") ? int.Parse(query["sleepms"]) : 30000;
                    return contention.Saturate(sleepMs);

                case "/incident/status":
                    return $"{{\"retainedMemoryObjects\":{memory.RetainedObjectCount},\"waitingWorkers\":{threads.WorkerCount},\"queueBacklog\":{queue.BacklogCount},\"status\":\"UP\"}}";

                case "/actuator/health":
                    return "{\"status\":\"UP\"}";

                default:
                    return "{\"status\":\"UNKNOWN_ENDPOINT\"}";
            }
        }
    }
}
