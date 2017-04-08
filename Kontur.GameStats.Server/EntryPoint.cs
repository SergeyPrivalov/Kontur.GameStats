using System;
using Fclp;
using Ninject;

namespace Kontur.GameStats.Server
{
    public class EntryPoint
    {
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

        private static void RunServer(Options options)
        {
            var container = new StandardKernel();
            container.Bind<IServerDataBase>().To<ServerDataBase>();
            container.Bind<IGameStatistic>().To<GameStatistic>();
            container.Bind<IJsonSerializer>().To<JsonSerializer>();
            container.Bind<QueryProcessor>().ToSelf();
            var processor = container.Get<QueryProcessor>();//new QueryProcessor(new ServerDataBase(), new GameStatistic(), new JsonSerializer());
            using (var server = new StatServer(processor))
            {
                server.Start(options.Prefix);

                Console.ReadKey(true);
            }
        }

        private class Options
        {
            public string Prefix { get; set; }
        }
    }
}