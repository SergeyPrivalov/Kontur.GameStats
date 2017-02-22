using System;

namespace Kontur.GameStats.Server
{
    public interface IStatServerRequestHandler
    {
        RequestHandlingResult HandleGet(Uri uri);
        RequestHandlingResult HandlePut(Uri uri, string body);
    }
}
