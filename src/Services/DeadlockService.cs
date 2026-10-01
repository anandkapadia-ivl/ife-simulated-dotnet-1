using System;
using System.Threading;

namespace DotNetSimulator.Services
{
    /// <summary>
    /// Creates intentional circular monitor deadlock with retained heap payloads.
    /// Thread A acquires ResourceA, then waits for ResourceB.
    /// Thread B acquires ResourceB, then waits for ResourceA.
    /// </summary>
    public class DeadlockService
    {
        private readonly object resourceA = new object();
        private readonly object resourceB = new object();

        public class DeadlockPayload
        {
            public string ThreadName { get; }
            public byte[] Buffer { get; }

            public DeadlockPayload(string threadName, int mb)
            {
                ThreadName = threadName;
                Buffer = new byte[Math.Max(1, mb) * 1024 * 1024];
            }
        }

        public string TriggerDeadlock(int payloadMb)
        {
            var startEvent = new ManualResetEventSlim(false);

            var threadA = new Thread(() =>
            {
                startEvent.Wait();
                lock (resourceA)
                {
                    Console.WriteLine("[DEBUG] DeadlockWorker-A acquired resourceA");
                    var payload = new DeadlockPayload("IFE-DotNet-DeadlockWorker-A", payloadMb);
                    Thread.Sleep(100);
                    Console.WriteLine("[DEBUG] DeadlockWorker-A waiting for resourceB...");
                    lock (resourceB)
                    {
                        Console.WriteLine($"[DEBUG] DeadlockWorker-A acquired resourceB, payload size: {payload.Buffer.Length}");
                    }
                }
            })
            {
                Name = "IFE-DotNet-DeadlockWorker-A",
                IsBackground = true
            };

            var threadB = new Thread(() =>
            {
                startEvent.Wait();
                lock (resourceB)
                {
                    Console.WriteLine("[DEBUG] DeadlockWorker-B acquired resourceB");
                    var payload = new DeadlockPayload("IFE-DotNet-DeadlockWorker-B", payloadMb);
                    Thread.Sleep(100);
                    Console.WriteLine("[DEBUG] DeadlockWorker-B waiting for resourceA...");
                    lock (resourceA)
                    {
                        Console.WriteLine($"[DEBUG] DeadlockWorker-B acquired resourceA, payload size: {payload.Buffer.Length}");
                    }
                }
            })
            {
                Name = "IFE-DotNet-DeadlockWorker-B",
                IsBackground = true
            };

            threadA.Start();
            threadB.Start();
            startEvent.Set();

            return $"Deadlock triggered between IFE-DotNet-DeadlockWorker-A and IFE-DotNet-DeadlockWorker-B with {payloadMb}MB payloads";
        }
    }
}
