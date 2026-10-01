using System;
using System.Threading;

namespace DotNetSimulator.Services
{
    /// <summary>
    /// Deliberate lock contention on a shared simulated resource gate.
    /// Request threads queue and block on monitor entry.
    /// </summary>
    public class ResourceContentionService
    {
        private readonly object dbGate = new object();

        public string Saturate(int sleepMs)
        {
            lock (dbGate)
            {
                Console.WriteLine($"[INFO] Thread {Thread.CurrentThread.Name ?? "HttpWorker"} acquired dbGate, simulating DB operation for {sleepMs}ms");
                Thread.Sleep(sleepMs);
                return $"Resource operation completed after {sleepMs} ms";
            }
        }
    }
}
