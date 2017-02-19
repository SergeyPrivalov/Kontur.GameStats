using System.Net;

namespace Kontur.GameStats.Server
{
    public class RequestHandlingResult
    {
        public byte[] Response { get; set; }
        public HttpStatusCode Status { get; set; }
    }
}
