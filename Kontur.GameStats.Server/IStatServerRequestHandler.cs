using System;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Определяет интерфейс для обработки HTTP-запросов статистики (GET и PUT).
    /// </summary>
    public interface IStatServerRequestHandler
    {
        RequestHandlingResult HandleGet(Uri uri);
        RequestHandlingResult HandlePut(Uri uri, string body);
    }
}