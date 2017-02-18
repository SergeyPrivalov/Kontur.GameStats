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

        private readonly IStatServerRequestHandler handler;

        internal class RequestHandlingResult
        {
            public byte[] Response { get; set; }
            public HttpStatusCode Status { get; set; }
        }

        internal interface IStatServerRequestHandler
        {
            RequestHandlingResult HandleGet(Uri uri);
            RequestHandlingResult HandlePut(Uri uri, string body);
        }

        public StatServer(IStatServerRequestHandler handler)
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
            response.StatusCode = (int)HttpStatusCode.OK;
            var requestHandlingResult = new RequestHandlingResult();
            if (request.HttpMethod == "PUT")
            {
                var sr = new StreamReader(request.InputStream);
                requestHandlingResult = handler.HandlePut(request.Url, sr.ReadToEnd());
                if (requestHandlingResult.Status != HttpStatusCode.Accepted)
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.ContentType = "message/http";
            }
            else if (request.HttpMethod == "GET")
            {
                requestHandlingResult = handler.HandleGet(request.Url);
                if (requestHandlingResult.Status != HttpStatusCode.Accepted)
                    response.StatusCode = (int)requestHandlingResult.Status;
                response.ContentType = "application/json";
            }
            response.ContentLength64 = requestHandlingResult.Response.Length;
            using (var outputStream = response.OutputStream)
            {
                await outputStream.WriteAsync(requestHandlingResult.Response, 0,
                    requestHandlingResult.Response.Length);
            }
        }
    }
}