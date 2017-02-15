using System;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace Kontur.GameStats.Server
{
    internal class StatServer : IDisposable
    {
        public StatServer()
        {
            listener = new HttpListener();
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

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }
        
        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error)
                {
                    // TODO: log errors
                    Console.WriteLine(error.StackTrace);
                }
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
                var answer = QueryProcessor.ProcessPutRequest(requestString,
                    sr.ReadToEnd());
                if (!answer)
                    response.StatusCode = (int) HttpStatusCode.BadRequest;
            }
            else if (request.HttpMethod == "GET")
            {
                {
                    var answer = QueryProcessor.ProcessGetRequest(requestString);
                    switch (answer)
                    {
                        case "Not Found":
                            response.StatusCode = (int) HttpStatusCode.NotFound;
                            break;
                        case "Bad Request":
                            response.StatusCode = (int) HttpStatusCode.BadRequest;
                            break;
                        default:
                            var buffer = System.Text.Encoding.UTF8.GetBytes(answer);
                            response.ContentLength64 = buffer.Length;
                            response.ContentType = "application/json";
                            var output = response.OutputStream;
                            output.Write(buffer, 0, buffer.Length);
                            output.Close();
                            break;
                    }
                }
            }
            using (var writer = new StreamWriter(response.OutputStream))
                writer.WriteLine("Hello world!!!");
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
    }
}