using System;
using Fclp;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Точка входа приложения, инициализирует и запускает HTTP-сервер статистики.
    /// </summary>
    public class EntryPoint
    {
        /// <summary>
        /// Точка входа приложения. Парсит аргументы командной строки и запускает сервер. Использует RunServer для запуска HTTP-сервера статистики.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        public static void Main(string[] args)
        {
            var commandLineParser = new FluentCommandLineParser<Options>();

            commandLineParser
                .Setup(options => options.Prefix)
                .As("prefix")
                .SetDefault("http://+:8080/")
                .WithDescription("HTTP prefix to listen on");

            commandLineParser
                .SetupHelp("h", "help")
                .WithHeader($"{AppDomain.CurrentDomain.FriendlyName} [--prefix <prefix>]")
                .Callback(text => Console.WriteLine(text));

            if (commandLineParser.Parse(args).HelpCalled)
                return;

            RunServer(commandLineParser.Object);
        }

        /// <summary>
        /// Запускает HTTP-сервер статистики с указанными параметрами. Создает QueryProcessor и StatServer, затем запускает сервер. Использует Start для запуска сервера.
        /// </summary>
        /// <param name="options">Параметры запуска сервера, включая префикс URL.</param>
        private static void RunServer(Options options)
        {
            var processor = new QueryProcessor();
            using (var server = new StatServer(processor))
            {
                server.Start(options.Prefix);

                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// Представляет параметры командной строки для настройки сервера.
        /// </summary>
        private class Options
        {
            public string Prefix { get; set; }
        }
    }
}