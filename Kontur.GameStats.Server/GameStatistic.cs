using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.GameStats.Server
{
    public class GameStatistic
    {
        public static ServerStats GetServerStatistic(string endpoint)
        {
            var games = QueryProcessor.GameServers
                .Where(x => x.Endpoint == endpoint).ToArray();
            var groupByDate = GroupByDate(games);
            return new ServerStats(
                games.Length,
                groupByDate.Max(x => x.Count()),
                GetDivision(games.Length,groupByDate.Length),
                games.Max(x => x.Scoreboard.Length),
                GetDivision(games.Sum(x => x.Scoreboard.Length), games.Length), 
                GetTopN(5, games.GroupBy(x => x.GameMode)),
                GetTopN(5, games.GroupBy(x => x.Map)));
        }

        private static double GetDivision(double x, double y)
        {
            return x / y;
        }

        private static string[] GetTopN(int n,
            IEnumerable<IGrouping<string,GameServer>> game)
        {
            return game.OrderByDescending(x => x.Count())
                .Select(x => x.Key).Take(n).ToArray();
        }

        private static IGrouping<DateTime,GameServer>[] GroupByDate(IEnumerable<GameServer> gameServers)
        {
            return gameServers.GroupBy(x => x.Date).ToArray();
        }

        public static PlayerStats GetPlayerStatistic(string name)
        {
            var games = QueryProcessor.GameServers
                .Where(x => x.Scoreboard.Any(y => y.Name == name)).ToArray();
            var groupByEndpoint = games.GroupBy(x => x.Endpoint).ToArray();
            var groupByDate = GroupByDate(games);
            return new PlayerStats(
                games.Length,
                games.Count(x => x.Scoreboard[0].Name == name),
                GetTopN(1, groupByEndpoint)[0],
                groupByEndpoint.Count(),
                GetTopN(1, games.GroupBy(x => x.GameMode))[0],
                GetAverageScoreboardPercent(games, name),
                groupByDate.Max(x => x.Count()),
                GetDivision(games.Length, groupByDate.Length),
                DateToString(games.Max(x => x.DateAndTime)),
                GetKillToDeathRatio(GetPlayerStats(games,name)));
        }

        private static Player[] GetPlayerStats(IEnumerable<GameServer> gameServers,
            string name)
        {
            return gameServers
                .SelectMany(x => x.Scoreboard.Where(y => y.Name == name))
                .ToArray();
        }

        private static double GetKillToDeathRatio(Player[] stats)
        {
            return GetDivision(stats.Sum(x => x.Kills), stats.Sum(x => x.Deaths));
        }

        private static double GetAverageScoreboardPercent(GameServer[] gameServers,
            string name)
        {
            var averageScoreboard = new double[gameServers.Length];
            for (var j = 0; j < gameServers.Length; ++j)
            {
                var scoreboardLength = gameServers[j].Scoreboard.Length;
                for (var i = 0; i < scoreboardLength; ++i)
                {
                    if (gameServers[j].Scoreboard[i].Name == name)
                        averageScoreboard[j] = ((double) (scoreboardLength - (i + 1))
                                                / (scoreboardLength - 1)) * 100;
                }
            }
            return averageScoreboard.Average();
        }

        public static string DateToString(DateTime date)
        {
            return $"{date.Year}-{date.Month}-{date.Day}T" +
                   $"{date.Hour}:{date.Minute}:{date.Second}Z";
        }

        public static RecentMatch[] GetRecentMatches(int n)
        {
            return QueryProcessor.GameServers
                .OrderByDescending(x => x.DateAndTime)
                .Take(n)
                .Select(x =>
                    new RecentMatch(x.Endpoint, DateToString(x.DateAndTime), x))
                .ToArray();
        }

        public static BestPlayer[] GetBestPlayers(int n)
        {
            var playersNames = QueryProcessor
                .GameServers.SelectMany(x => x.Scoreboard)
                .GroupBy(x => x.Name)
                .Where(x => x.Count() >= 10)
                .Select(x => x.Key)
                .ToArray();
            var listOfPlayers = new List<BestPlayer>();
            foreach (var name in playersNames)
            {
                var stats = GetPlayerStats(QueryProcessor.GameServers.ToArray(), name);
                if (stats.Sum(x => x.Deaths) != 0)
                    listOfPlayers.Add(new BestPlayer(name, GetKillToDeathRatio(stats)));
            }
            return listOfPlayers
                .OrderByDescending(x => x.KillToDeathRatio)
                .Take(n)
                .ToArray();
        }

        public static PopularServer[] GetPopularServers(int n)
        {
            return QueryProcessor.AdvertiseServers
                .Select(x => new PopularServer(
                    x.Endpoint,
                    x.Info.Name,
                    GetAverageMatchPerDay(x.Endpoint)))
                .OrderByDescending(x => x.AverageMatchPerDay)
                .Take(n)
                .ToArray();
        }

        public static double GetAverageMatchPerDay(string enpoint)
        {
            return GetDivision(
                QueryProcessor.GameServers
                    .GroupBy(x => x.Endpoint)
                    .Count(x => x.Key == enpoint),
                QueryProcessor.GameServers
                    .Where(x => x.Endpoint == enpoint)
                    .GroupBy(x => x.Date).Count());
        }
    }
}
