using System.Net;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Представляет результат обработки HTTP-запроса, включая статус ответа и данные ответа.
    /// </summary>
    public struct RequestHandlingResult
    {
        public byte[] Response { get; private set; }
        public HttpStatusCode Status { get; private set; }

        /// <summary>
        /// Создает успешный результат обработки запроса со статусом Accepted.
        /// </summary>
        /// <param name="response">Данные ответа в виде массива байтов.</param>
        /// <returns>Результат обработки запроса со статусом Accepted и указанными данными ответа.</returns>
        public static RequestHandlingResult Successfull(byte[] response)
        {
            return new RequestHandlingResult
            {
                Status = HttpStatusCode.Accepted,
                Response = response
            };
        }

        /// <summary>
        /// Создает неуспешный результат обработки запроса с указанным статусом ошибки.
        /// </summary>
        /// <param name="httpStatusCode">HTTP-статус код ошибки.</param>
        /// <returns>Результат обработки запроса с указанным статусом ошибки и пустым массивом байтов в качестве ответа.</returns>
        public static RequestHandlingResult Fail(HttpStatusCode httpStatusCode)
        {
            return new RequestHandlingResult
            {
                Status = httpStatusCode,
                Response = new byte[0]
            };
        }
    }
}