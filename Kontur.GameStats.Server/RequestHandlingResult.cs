using System.Net;

namespace Kontur.GameStats.Server
{
    public struct RequestHandlingResult
    {
        public byte[] Response { get; private set; }
        public HttpStatusCode Status { get; private set; }

        public static RequestHandlingResult Successfull(byte[] response)
        {
            return new RequestHandlingResult
            {
                Status = HttpStatusCode.Accepted,
                Response = response
            };
        }

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
