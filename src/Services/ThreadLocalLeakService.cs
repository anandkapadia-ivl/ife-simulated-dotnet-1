using System;
using System.Collections.Generic;
using System.Threading;

namespace DotNetSimulator.Services
{
    /// <summary>
    /// Deliberate ThreadLocal memory retention defect.
    /// ThreadLocal instances allocated on thread pool / worker threads are not cleaned up.
    /// </summary>
    public class ThreadLocalLeakService
    {
        private static readonly ThreadLocal<ThreadLocalSessionContext> SessionContext = new ThreadLocal<ThreadLocalSessionContext>(true);
        private readonly List<Thread> poolWorkers = new List<Thread>();

        public class ThreadLocalSessionContext
        {
            public string SessionId { get; }
            public byte[] Buffer { get; }

            public ThreadLocalSessionContext(string sessionId, int mb)
            {
                SessionId = sessionId;
                Buffer = new byte[Math.Max(1, mb) * 1024 * 1024];
            }
        }

        public string Leak(int count, int megabytes)
        {
            int numWorkers = Math.Max(1, count);
            for (int i = 0; i < numWorkers; i++)
            {
                int index = i + 1;
                var t = new Thread(() =>
                {
                    string sessionId = $"NET-SESSION-{DateTime.UtcNow.Ticks}-{index}";
                    SessionContext.Value = new ThreadLocalSessionContext(sessionId, megabytes);
                    Console.WriteLine($"[INFO] Assigned {megabytes} MB ThreadLocal context to {Thread.CurrentThread.Name}");
                    Thread.Sleep(300000); // Keep alive to hold ThreadLocal state in memory dump
                })
                {
                    Name = $"IFE-DotNet-ThreadLocalWorker-{index}",
                    IsBackground = true
                };

                poolWorkers.Add(t);
                t.Start();
            }

            return $"Dispatched ThreadLocal leak across {numWorkers} workers ({megabytes} MB each)";
        }
    }
}
