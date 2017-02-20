using System.Net;

namespace Kontur.GameStats.Server
{
    public class RequestHandlingResult
    {
        public byte[] Response { get; set; }
        public HttpStatusCode Status { get; set; }

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
