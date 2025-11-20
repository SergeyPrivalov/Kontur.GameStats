using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Реализует HTTP-сервер статистики, принимающий и обрабатывающий входящие запросы.
    /// </summary>
    internal class StatServer : IDisposable
    {
        private readonly IStatServerRequestHandler handler;
        private readonly HttpListener listener;
        private bool disposed;
        private volatile bool isRunning;

        private Thread listenerThread;

        /// <summary>
        /// Инициализирует новый экземпляр класса StatServer с указанным обработчиком запросов.
        /// </summary>
        /// <param name="handler">Обработчик запросов для обработки входящих HTTP-запросов.</param>
        public StatServer(IStatServerRequestHandler handler)
        {
            this.handler = handler;
            listener = new HttpListener();
        }

        /// <summary>
        /// Освобождает ресурсы, используемые сервером. Использует Stop для остановки сервера перед закрытием.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }

        /// <summary>
        /// Запускает HTTP-сервер с указанным префиксом URL и начинает прослушивание входящих запросов в отдельном потоке. Использует Listen для обработки запросов.
        /// </summary>
        /// <param name="prefix">Префикс URL для прослушивания (например, "http://+:8080/").</param>
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

        /// <summary>
        /// Останавливает HTTP-сервер и завершает поток прослушивания.
        /// </summary>
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

        /// <summary>
        /// Основной цикл прослушивания входящих HTTP-запросов. Для каждого запроса создает асинхронную задачу обработки. Использует HandleContextAsync для асинхронной обработки каждого запроса.
        /// </summary>
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

        /// <summary>
        /// Обрабатывает HTTP-запрос в зависимости от метода (GET или PUT). Использует HandleGet для обработки GET-запросов и HandlePut для обработки PUT-запросов.
        /// </summary>
        /// <param name="request">HTTP-запрос для обработки.</param>
        /// <param name="response">HTTP-ответ для установки заголовков и статуса.</param>
        /// <returns>Результат обработки запроса от соответствующего обработчика.</returns>
        private RequestHandlingResult ProcessRequest(
            HttpListenerRequest request,
            HttpListenerResponse response)
        {
            var requestHandlingResult = new RequestHandlingResult();
            if (request.HttpMethod == "PUT")
            {
                var sr = new StreamReader(request.InputStream);
                requestHandlingResult = handler.HandlePut(request.Url, sr.ReadToEnd());
                if (requestHandlingResult.Status != HttpStatusCode.Accepted)
                    response.StatusCode = (int) HttpStatusCode.BadRequest;
                response.ContentType = "message/http";
            }
            else if (request.HttpMethod == "GET")
            {
                requestHandlingResult = handler.HandleGet(request.Url);
                if (requestHandlingResult.Status != HttpStatusCode.Accepted)
                    response.StatusCode = (int) requestHandlingResult.Status;
                response.ContentType = "application/json";
            }
            response.ContentLength64 = requestHandlingResult.Response.Length;
            return requestHandlingResult;
        }

        /// <summary>
        /// Асинхронно обрабатывает HTTP-контекст запроса. Использует ProcessRequest для обработки запроса и записи ответа в поток вывода.
        /// </summary>
        /// <param name="listenerContext">HTTP-контекст запроса для обработки.</param>
        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            var request = listenerContext.Request;
            var response = listenerContext.Response;
            response.StatusCode = (int) HttpStatusCode.OK;
            var requestHandlingResult = ProcessRequest(request, response);
            using (var outputStream = response.OutputStream)
            {
                await outputStream.WriteAsync(requestHandlingResult.Response, 0, requestHandlingResult.Response.Length);
            }
        }
    }
}