using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using DotNetSimulator.Controllers;
using DotNetSimulator.Services;

namespace DotNetSimulator
{
    public class Program
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\logs\ife-dotnet-application.log");
        private static readonly object LogLock = new object();

        public static void Log(string level, string logger, string message)
        {
            string threadName = Thread.CurrentThread.Name ?? $"Thread-{Thread.CurrentThread.ManagedThreadId}";
            string line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {level,-5} {Process.GetCurrentProcess().Id} --- [{threadName,-26}] {logger,-38} : {message}";
            Console.WriteLine(line);

            try
            {
                lock (LogLock)
                {
                    string dir = Path.GetDirectoryName(LogFilePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                }
            }
            catch { }
        }

        public static void Main(string[] args)
        {
            int port = 5050;
            if (args.Length > 0 && int.TryParse(args[0], out int p)) port = p;

            int pid = Process.GetCurrentProcess().Id;
            Log("INFO", "DotNetSimulator.Program", $"Starting DotNetSimulator v1.0.0 on .NET Runtime with PID {pid}");
            Log("INFO", "DotNetSimulator.Program", $"Environment: 64-bit OS, MachineName={Environment.MachineName}");

            var memory = new MemoryLeakService();
            var deadlock = new DeadlockService();
            var threads = new ThreadSaturationService();
            var threadLocal = new ThreadLocalLeakService();
            var queue = new AsyncQueueSaturationService();
            var contention = new ResourceContentionService();

            var controller = new IncidentController(memory, deadlock, threads, threadLocal, queue, contention);

            string prefix = $"http://localhost:{port}/";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            Log("INFO", "DotNetSimulator.Program", $"HttpListener started and listening on {prefix}");
            Log("INFO", "DotNetSimulator.Program", "Application initialized and ready for incident simulation");

            while (listener.IsListening)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem((state) =>
                    {
                        var ctx = (HttpListenerContext)state;
                        try
                        {
                            var req = ctx.Request;
                            string path = req.Url.AbsolutePath;

                            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            foreach (string key in req.QueryString.AllKeys)
                            {
                                if (key != null) query[key] = req.QueryString[key];
                            }

                            Log("DEBUG", "DotNetSimulator.HttpServer", $"{req.HttpMethod} '{path}', query={req.Url.Query}");

                            string responseString = controller.HandleRequest(path, query);
                            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                            ctx.Response.StatusCode = 200;
                            ctx.Response.ContentType = "application/json";
                            ctx.Response.ContentLength64 = buffer.Length;
                            ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                            ctx.Response.OutputStream.Close();

                            Log("DEBUG", "DotNetSimulator.HttpServer", $"Completed 200 OK for {req.HttpMethod} {path}");
                        }
                        catch (Exception ex)
                        {
                            Log("ERROR", "DotNetSimulator.HttpServer", $"Error handling request: {ex.Message}");
                            try
                            {
                                ctx.Response.StatusCode = 500;
                                ctx.Response.Close();
                            }
                            catch { }
                        }
                    }, context);
                }
                catch (Exception ex)
                {
                    if (!listener.IsListening) break;
                    Log("ERROR", "DotNetSimulator.HttpServer", $"Listener exception: {ex.Message}");
                }
            }
        }
    }
}
