using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.GameStats.Server
{
    public class GameStatistic
    {
        public ServerStats GetServerStatistic(GameServer[] games)
        {
            var groupByDate = games.GroupBy(x => x.DateAndTime.Date).ToArray();
            return new ServerStats
            {
                TotalMatchesPlayed = games.Length,
                MaximumMatchesPerDay = groupByDate.Max(x => x.Count()),
                AverageMatchesPerDay = GetDivision(games.Length, groupByDate.Length),
                MaximumPopulation = games.Max(x => x.Scoreboard.Length),
                AveragePopulation = GetDivision(games.Sum(x => x.Scoreboard.Length), games.Length),
                Top5GameModes = GetTopElements(5, games.GroupBy(x => x.GameMode)),
                Top5Maps = GetTopElements(5, games.GroupBy(x => x.Map))
            };
        }

        private double GetDivision(double x, double y)
        {
            return Math.Round(x / y, 6);
        }

        private string[] GetTopElements(int countOfElements, IEnumerable<IGrouping<string, GameServer>> game)
        {
            return game
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Take(countOfElements)
                .ToArray();
        }

        public PlayerStats GetPlayerStatistic(string name, GameServer[] games)
        {
            var groupByEndpoint = games.GroupBy(x => x.Endpoint).ToArray();
            var groupByDate = games.GroupBy(x => x.DateAndTime.Date).ToArray();
            return new PlayerStats
            {
                TotalMatchesPlayed = games.Length,
                TotalMatchesWon =
                    games.Count(x => x.Scoreboard[0].Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)),
                FavoriteServer = GetTopElements(1, groupByEndpoint)[0],
                UniqueServers = groupByEndpoint.Count(),
                FavoriteGameMode = GetTopElements(1, games.GroupBy(x => x.GameMode))[0],
                AverageScoreboardPercent = GetAverageScoreboardPercent(games, name),
                MaximumMatchesPerDay = groupByDate.Max(x => x.Count()),
                AverageMatchesPerDay = GetDivision(games.Length, groupByDate.Length),
                LastMatchPlayed = games.Max(x => x.DateAndTime).ToString("yyyy-MM-dTHH:mm:ssZ"),
                KillToDeathRatio = GetKillToDeathRatio(GetPlayerStats(games, name))
            };
        }

        private Player[] GetPlayerStats(IEnumerable<GameServer> gameServers, string name)
        {
            return gameServers
                .SelectMany(x => x.Scoreboard
                    .Where(y => y.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();
        }

        private double GetKillToDeathRatio(Player[] stats)
        {
            return GetDivision(stats.Sum(x => x.Kills), stats.Sum(x => x.Deaths));
        }

        private double GetAverageScoreboardPercent(IReadOnlyList<GameServer> gameServers, string name)
        {
            var averageScoreboard = new double[gameServers.Count];
            for (var j = 0; j < gameServers.Count; ++j)
            {
                var scoreboardLength = gameServers[j].Scoreboard.Length;
                for (var i = 0; i < scoreboardLength; ++i)
                    if (gameServers[j].Scoreboard[i].Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        averageScoreboard[j] = (double) (scoreboardLength - (i + 1))
                                               / (scoreboardLength - 1) * 100;
            }
            return Math.Round(averageScoreboard.Average(), 6);
        }

        public RecentMatch[] GetRecentMatches(int countOfMatches, IEnumerable<GameServer> gameServers)
        {
            return gameServers
                .OrderByDescending(x => x.DateAndTime)
                .Take(countOfMatches)
                .Select(x =>
                    new RecentMatch
                    {
                        Server = x.Endpoint,
                        Timestamp = x.DateAndTime.ToString("yyyy-MM-dTHH:mm:ssZ"),
                        Result = x
                    })
                .ToArray();
        }

        public BestPlayer[] GetBestPlayers(int countOfPlayers, BlockingCollection<GameServer> gameServers)
        {
            var playersNames = gameServers
                .SelectMany(x => x.Scoreboard)
                .GroupBy(x => x.Name)
                .Where(x => x.Count() >= 10)
                .Select(x => x.Key);
            var listOfPlayers = new List<BestPlayer>();
            foreach (var name in playersNames)
            {
                var stats = GetPlayerStats(gameServers.ToArray(), name);
                if (stats.Sum(x => x.Deaths) != 0)
                    listOfPlayers.Add(new BestPlayer
                    {
                        Name = name,
                        KillToDeathRatio = GetKillToDeathRatio(stats)
                    });
            }
            return listOfPlayers
                .OrderByDescending(x => x.KillToDeathRatio)
                .Take(countOfPlayers)
                .ToArray();
        }

        public PopularServer[] GetPopularServers(
            int countOfServers,
            ConcurrentDictionary<string, AdvertiseQueryServer> advertiseServers,
            IEnumerable<GameServer> gameServers)
        {
            return advertiseServers
                .Select(x => new PopularServer
                {
                    Enpoint = x.Key,
                    Name = x.Value.Info.Name,
                    AverageMatchPerDay = GetAverageMatchPerDay(x.Key, gameServers.ToArray())
                })
                .OrderByDescending(x => x.AverageMatchPerDay)
                .Take(countOfServers)
                .ToArray();
        }

        public double GetAverageMatchPerDay(string enpoint, GameServer[] gameServers)
        {
            return GetDivision(
                gameServers
                    .GroupBy(x => x.Endpoint)
                    .Count(x => x.Key == enpoint),
                gameServers
                    .Where(x => x.Endpoint == enpoint)
                    .GroupBy(x => x.DateAndTime.Date).Count());
        }
    }
}