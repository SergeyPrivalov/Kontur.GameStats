﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.GameStats.Server
{
    public class GameStatistic
    {
        public ServerStats GetServerStatistic(GameServer[] games)
        {
            var groupByDate = games.GroupBy(x => x.Date).ToArray();
            return new ServerStats
            {
                TotalMatchesPlayed = games.Length,
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
            //че за n - непонятно
            return game
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Take(n)
                .ToArray();
        }

        public PlayerStats GetPlayerStatistic(string name, GameServer[] games)
        {
            var groupByEndpoint = games.GroupBy(x => x.Endpoint).ToArray();
            var groupByDate = games.GroupBy(x => x.Date).ToArray();
            return new PlayerStats
            {
                TotalMatchesPlayed = games.Length,
                TotalMatchesWon = games.Count(x => x.Scoreboard[0].Name.ToLower() == name),
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
                //опять же сравнение с ToLower.. 
                .SelectMany(x => x.Scoreboard.Where(y => y.Name.ToLower() == name))
                //.SelectMany(x => x.Scoreboard.Where(y => y.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))) лучше так
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
                    if (gameServers[j].Scoreboard[i].Name.ToLower() == name)
                        averageScoreboard[j] = (double) (scoreboardLength - (i + 1))
                                               / (scoreboardLength - 1) * 100;
            }
            return Math.Round(averageScoreboard.Average(), 6);
        }

        public string DateToString(DateTime date)
        {
            return $"{date.Year}-{date.Month}-{date.Day}T" +
                   $"{date.Hour - 5}:{date.Minute}:{date.Second}Z";
            //запустят в другом часовом поясе и все эти - 5 буту ошибочными
            //лучше использовать date.ToString(и здесь указать формат)
        }

        public RecentMatch[] GetRecentMatches(int n, IEnumerable<GameServer> gameServers)
        {
            return gameServers
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

        public BestPlayer[] GetBestPlayers(int n, BlockingCollection<GameServer> gameServers)
        {
            var playersNames = gameServers
                .SelectMany(x => x.Scoreboard)
                .GroupBy(x => x.Name)
                .Where(x => x.Count() >= 10)
                .Select(x => x.Key)
                .ToArray();//зачем ToArray? он перечисляет коллекцию. Притом сразу снизу ты снова перечисляешь ту же самую коллекцию
            var listOfPlayers = new List<BestPlayer>();
            foreach (var name in playersNames)
            {
                var stats = GetPlayerStats(gameServers.ToArray(), name.ToLower());
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

        public PopularServer[] GetPopularServers(
            int n, 
            ConcurrentDictionary<string, AdvertiseQueryServer> advertiseServers,
            IEnumerable<GameServer> gameServers)
        {
            return advertiseServers
                .Select(x => new PopularServer
                {
                    Enpoint = x.Key,
                    Name = x.Value.Info.Name,
                    AverageMatchPerDay = GetAverageMatchPerDay(x.Key, gameServers)
                })
                .OrderByDescending(x => x.AverageMatchPerDay)
                .Take(n) //что такое n, дай ему нормальное название
                .ToArray();
        }

        public double GetAverageMatchPerDay(string enpoint, IEnumerable<GameServer> gameServers)
        {
            //решарпер ругается, что здесь коллекция серверов может уже начать что то перечислять
            //так что лучше все таки здесь принимать массив ну или IList если нарвится
            return GetDivision(
                gameServers
                    .GroupBy(x => x.Endpoint)
                    .Count(x => x.Key == enpoint),
                gameServers
                    .Where(x => x.Endpoint == enpoint)
                    .GroupBy(x => x.Date).Count());
        }
    }
}