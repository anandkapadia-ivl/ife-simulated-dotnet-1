using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace DotNetSimulator.Services
{
    /// <summary>
    /// Deliberate task queue saturation with thread bottleneck.
    /// Consumers are throttled while heavy task payloads queue in memory.
    /// </summary>
    public class AsyncQueueSaturationService
    {
        private readonly BlockingCollection<HeavyQueuedTask> taskQueue = new BlockingCollection<HeavyQueuedTask>();
        private readonly ManualResetEventSlim throttleLatch = new ManualResetEventSlim(false);
        private readonly List<Thread> workers = new List<Thread>();

        public class HeavyQueuedTask
        {
            public string TaskId { get; }
            public byte[] Payload { get; }

            public HeavyQueuedTask(string taskId, int mb)
            {
                TaskId = taskId;
                Payload = new byte[Math.Max(1, mb) * 1024 * 1024];
            }
        }

        public AsyncQueueSaturationService()
        {
            for (int i = 0; i < 4; i++)
            {
                int index = i + 1;
                var t = new Thread(() =>
                {
                    foreach (var task in taskQueue.GetConsumingEnumerable())
                    {
                        Console.WriteLine($"[INFO] Task {task.TaskId} picked up by {Thread.CurrentThread.Name}, waiting on throttle latch...");
                        throttleLatch.Wait(300000);
                    }
                })
                {
                    Name = $"IFE-DotNet-AsyncQueueWorker-{index}",
                    IsBackground = true
                };

                workers.Add(t);
                t.Start();
            }
        }

        public string EnqueueHeavyTasks(int count, int megabytes)
        {
            for (int i = 0; i < count; i++)
            {
                var task = new HeavyQueuedTask($"NET-TASK-{i + 1}", megabytes);
                taskQueue.Add(task);
            }

            return $"Enqueued {count} heavy tasks ({megabytes} MB each) into queue; backlog={taskQueue.Count}";
        }

        public int BacklogCount => taskQueue.Count;
    }
}
