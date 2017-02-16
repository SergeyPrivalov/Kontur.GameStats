using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kontur.GameStats.Server
{
    internal class StatServer : IDisposable
    {
        private readonly HttpListener listener;
        private bool disposed;
        private volatile bool isRunning;

        private Thread listenerThread;

        private readonly QueryProcessor queryProcessor = new QueryProcessor();

        public StatServer()
        {
            listener = new HttpListener();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }

        public void Start(string prefix)
        {
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();

                    isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();

                isRunning = false;
            }
        }

        private void Listen()
        {
            while (true)
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    else
                    {
                        Thread.Sleep(0);
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error)
                {
                    // TODO: log errors
                    File.AppendAllText("log.txt", error.StackTrace);
                }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            var request = listenerContext.Request;
            var response = listenerContext.Response;
            response.StatusCode = (int) HttpStatusCode.OK;
            var requestString = request.Url.LocalPath;
            if (request.HttpMethod == "PUT")
            {
                var sr = new StreamReader(request.InputStream);
                var answer = queryProcessor.ProcessPutRequest(requestString,
                    sr.ReadToEnd());
                if (!answer)
                    response.StatusCode = (int) HttpStatusCode.BadRequest;
            }
            else if (request.HttpMethod == "GET")
            {
                {
                    var requestHandlingResult = queryProcessor.ProcessGetRequest(requestString);
                    switch (requestHandlingResult)
                    {
                        case "Not Found":
                            response.StatusCode = (int) HttpStatusCode.NotFound;
                            break;
                        case "Bad Request":
                            response.StatusCode = (int) HttpStatusCode.BadRequest;
                            break;
                        default:
                            var buffer = Encoding.UTF8.GetBytes(requestHandlingResult);
                            response.ContentLength64 = buffer.Length;
                            response.ContentType = "application/json";
                            using (var outputStream = response.OutputStream)
                            {
                                await outputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                            break;
                    }
                }
            }
        }
    }
}