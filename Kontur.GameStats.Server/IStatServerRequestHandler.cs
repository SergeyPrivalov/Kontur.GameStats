using System;

namespace Kontur.GameStats.Server
{
    internal interface IStatServerRequestHandler
    {
        RequestHandlingResult HandleGet(Uri uri);
        RequestHandlingResult HandlePut(Uri uri, string body);
    }
}
