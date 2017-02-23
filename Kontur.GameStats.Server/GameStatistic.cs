using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Kontur.GameStats.Server
{
    public class GameStatistic
    {
        public ServerStats GetServerStatistic(string endpoint)
        {
            var games = QueryProcessor.GameServers
                .Where(x => x.Endpoint == endpoint).ToArray();
            var groupByDate = GroupByDate(games);
            return new ServerStats
            {
                TotalMatchesPlaed = games.Length,
                MaximumMatchesPerDay = groupByDate.Max(x => x.Count()),
                AverageMatchesPerDay = GetDivision(games.Length, groupByDate.Length),
                MaximumPopulation = games.Max(x => x.Scoreboard.Length),
                AveragePopulation = GetDivision(games.Sum(x => x.Scoreboard.Length), games.Length),
                Top5GameModes = GetTopN(5, games.GroupBy(x => x.GameMode)),
                Top5Maps = GetTopN(5, games.GroupBy(x => x.Map))
            };
        }

        private double GetDivision(double x, double y)
        {
            return Math.Round(x / y, 6);
        }

        private string[] GetTopN(int n, IEnumerable<IGrouping<string, GameServer>> game)
        {
            return game.OrderByDescending(x => x.Count())
                .Select(x => x.Key).Take(n).ToArray();
        }

        private IGrouping<DateTime, GameServer>[] GroupByDate(IEnumerable<GameServer> gameServers)
        {
            return gameServers.GroupBy(x => x.Date).ToArray();
        }

        public PlayerStats GetPlayerStatistic(string name)
        {
            var games = QueryProcessor.GameServers
                .Where(x => x.Scoreboard.Any(y => string.Equals(y.Name, name, StringComparison.CurrentCultureIgnoreCase)))
                .ToArray();
            var groupByEndpoint = games.GroupBy(x => x.Endpoint).ToArray();
            var groupByDate = GroupByDate(games);
            return new PlayerStats
            {
                TotalMatchesPlayed = games.Length,
                TotalMatchesWon = games.Count(x => x.Scoreboard[0].Name == name),
                FavoriteServer = GetTopN(1, groupByEndpoint)[0],
                UniqueServers = groupByEndpoint.Count(),
                FavoriteGameMode = GetTopN(1, games.GroupBy(x => x.GameMode))[0],
                AverageScoreboardPercent = GetAverageScoreboardPercent(games, name),
                MaximumMatchesPerDay = groupByDate.Max(x => x.Count()),
                AverageMatchesPerDay = GetDivision(games.Length, groupByDate.Length),
                LastMatchPlayed = DateToString(games.Max(x => x.DateAndTime)),
                KillToDeathRatio = GetKillToDeathRatio(GetPlayerStats(games, name))
            };
        }

        private Player[] GetPlayerStats(IEnumerable<GameServer> gameServers, string name)
        {
            return gameServers
                .SelectMany(x => x.Scoreboard.Where(y => y.Name == name))
                .ToArray();
        }

        private double GetKillToDeathRatio(Player[] stats)
        {
            return GetDivision(stats.Sum(x => x.Kills), stats.Sum(x => x.Deaths));
        }

        private double GetAverageScoreboardPercent(GameServer[] gameServers, string name)
        {
            var averageScoreboard = new double[gameServers.Length];
            for (var j = 0; j < gameServers.Length; ++j)
            {
                var scoreboardLength = gameServers[j].Scoreboard.Length;
                for (var i = 0; i < scoreboardLength; ++i)
                    if (gameServers[j].Scoreboard[i].Name == name)
                        averageScoreboard[j] = (double)(scoreboardLength - (i + 1))
                                               / (scoreboardLength - 1) * 100;
            }
            return Math.Round(averageScoreboard.Average(), 6);
        }

        public string DateToString(DateTime date)
        {
            return $"{date.Year}-{date.Month}-{date.Day}T" +
                   $"{date.Hour}:{date.Minute}:{date.Second}Z";
        }

        public RecentMatch[] GetRecentMatches(int n)
        {
            return QueryProcessor.GameServers
                .OrderByDescending(x => x.DateAndTime)
                .Take(n)
                .Select(x =>
                    new RecentMatch
                    {
                        Server = x.Endpoint,
                        Timestamp = DateToString(x.DateAndTime),
                        Result = x
                    })
                .ToArray();
        }

        public BestPlayer[] GetBestPlayers(int n)
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
                    listOfPlayers.Add(new BestPlayer
                    {
                        Name = name,
                        KillToDeathRatio = GetKillToDeathRatio(stats)
                    });
            }
            return listOfPlayers
                .OrderByDescending(x => x.KillToDeathRatio)
                .Take(n)
                .ToArray();
        }

        public PopularServer[] GetPopularServers(int n)
        {
            return QueryProcessor.AdvertiseServers
                .Select(x => new PopularServer
                {
                    Enpoint = x.Endpoint,
                    Name = x.Info.Name,
                    AverageMatchPerDay = GetAverageMatchPerDay(x.Endpoint)
                })
                .OrderByDescending(x => x.AverageMatchPerDay)
                .Take(n)
                .ToArray();
        }

        public double GetAverageMatchPerDay(string enpoint)
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