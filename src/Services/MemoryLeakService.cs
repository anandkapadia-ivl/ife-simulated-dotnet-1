using System;
using System.Collections.Generic;

namespace DotNetSimulator.Services
{
    /// <summary>
    /// Deliberate static memory retention defect.
    /// Retained byte arrays are pinned in the LOH (Large Object Heap) / GC heap.
    /// </summary>
    public class MemoryLeakService
    {
        private static readonly List<byte[]> RetainedRequests = new List<byte[]>();
        private static readonly object SyncLock = new object();

        public string Leak(int megabytes)
        {
            int mb = Math.Max(1, megabytes);
            int bytes = mb * 1024 * 1024;
            lock (SyncLock)
            {
                RetainedRequests.Add(new byte[bytes]);
                return $"Retained {mb} MB; total retained objects={RetainedRequests.Count}";
            }
        }

        public int RetainedObjectCount
        {
            get
            {
                lock (SyncLock)
                {
                    return RetainedRequests.Count;
                }
            }
        }
    }
}
