using System;
using System.Collections.Generic;
using System.Threading;

namespace DotNetSimulator.Services
{
    /// <summary>
    /// Spawns named background worker threads parked on wait events.
    /// Simulates thread saturation in thread dumps.
    /// </summary>
    public class ThreadSaturationService
    {
        private readonly List<Thread> workers = new List<Thread>();
        private readonly object syncLock = new object();

        public string CreateWaitingThreads(int count, int waitMs)
        {
            lock (syncLock)
            {
                for (int i = 0; i < count; i++)
                {
                    int index = workers.Count + 1;
                    var evt = new ManualResetEventSlim(false);
                    var t = new Thread(() =>
                    {
                        try
                        {
                            evt.Wait(waitMs);
                        }
                        catch (ThreadInterruptedException)
                        {
                        }
                    })
                    {
                        Name = $"IFE-DotNet-WaitingWorker-{index}",
                        IsBackground = true
                    };

                    workers.Add(t);
                    t.Start();
                }
                return $"Created {count} waiting worker threads (total={workers.Count})";
            }
        }

        public int WorkerCount
        {
            get
            {
                lock (syncLock)
                {
                    return workers.Count;
                }
            }
        }
    }
}
