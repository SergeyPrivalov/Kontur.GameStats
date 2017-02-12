using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server.Tests
{
    [TestClass]
    public class StatisticsTests
    {
        private AdvertiseQueryServer firstServer =
           new AdvertiseQueryServer("12.12.12.12-1333",
               new Information
               {
                   Name = "] My P3rfect Server [",
                   GameModes = new[] { "DM", "TDM" }
               });

        private GameServer gameServer1 =
            new GameServer("DM-HelloWorld", "DM", 20, 20, 12.345678,
                new[]
                {
                    new Player("Player20", 20, 21, 3),
                    new Player("Player1", 2, 2, 21)
                });

        private GameServer gameServer2 =
            new GameServer("DM-Hello", "TDM", 30, 30, 22.345678,
                new[]
                {
                    new Player("Player20", 20, 21, 3),
                    new Player("Player1", 2, 2, 21),
                    new Player("Player3", 2, 2, 21),
                    new Player("Player4", 2, 2, 21)
                });

        private GameServer gameServer3 =
            new GameServer("DM", "DM", 40, 40, 32.345678,
                new[]
                {
                    new Player("Player1", 20, 21, 3),
                    new Player("Player20", 2, 2, 21),
                    new Player("Player3", 2, 2, 21)
                });

        private readonly DateTime date1 = new DateTime(2020,01,22,15,16,22);
        private readonly DateTime date2 = new DateTime(2020,01,23,14,00,00);

        [TestMethod]
        public void GetServersStats()
        {
            var endpoint = "12.12.12.12-1333";
            QueryProcessor.AdvertiseServers.Add(firstServer);
            MultiAdd(endpoint, date1, 2, gameServer1);
            MultiAdd(endpoint, date1, 2, gameServer3);
            MultiAdd(endpoint, date2, 2, gameServer2);
            var answer = "{\"totalMatchesPlayed\":6," +
                         "\"maximumMatchesPerDay\":4," +
                         "\"averageMatchesPerDay\":3.0," +
                         "\"maximumPopulation\":4," +
                         "\"averagePopulation\":3.0," +
                         "\"top5GameModes\":[\"DM\",\"TDM\"]," +
                         "\"top5Maps\":[\"DM-HelloWorld\",\"DM\",\"DM-Hello\"]}";

            var result = QueryProcessor
                .ProcessGetRequest("/servers/12.12.12.12-1333/stats");

            Assert.AreEqual(answer, result);
        }

        [TestMethod]
        public void GetPlayerStats()
        {
            var answerString = "{\"totalMatchesPlayed\":6," +
                               "\"totalMatchesWon\":4," +
                               "\"favoriteServer\":\"12.12.12.12-1333\"," +
                               "\"uniqueServers\":1," +
                               "\"favoriteGameMode\":\"DM\"," +
                               "\"averageScoreboardPercent\":83.333333," +
                               "\"maximumMatchesPerDay\":4," +
                               "\"averageMatchesPerDay\":3.0," +
                               "\"lastMatchPlayed\":\"2020-1-23T14:0:0Z\"," +
                               "\"killToDeathRatio\":1.62963}";

            var result = QueryProcessor.ProcessGetRequest("/players/player20/stats");

            Assert.AreEqual(answerString,result);
        }

        [TestMethod]
        public void GetRecentMatches()
        {
            var answer = QueryProcessor.Json(QueryProcessor.GameServers
                .OrderByDescending(x => x.DateAndTime)
                .Take(10)
                .Select(x =>
                    new RecentMatch(x.Endpoint, GameStatistic.DateToString(x.DateAndTime), x))
                .ToArray());

            var result = QueryProcessor.ProcessGetRequest("/reports/recent-matches/10");

            Assert.AreEqual(answer,result);
        }

        [TestMethod]
        public void GetRecentMatchesWithoutCount()
        {
            var answer = QueryProcessor.Json(QueryProcessor.GameServers
                .OrderByDescending(x => x.DateAndTime)
                .Take(5)
                .Select(x =>
                    new RecentMatch(x.Endpoint,
                    GameStatistic.DateToString(x.DateAndTime), x))
                .ToArray());

            var result = QueryProcessor.ProcessGetRequest("/reports/recent-matches");

            Assert.AreEqual(answer, result);
        }

        [TestMethod]
        public void GetRecentMatchesMoreThan50()
        {
            var answer = QueryProcessor.Json(QueryProcessor.GameServers
                .OrderByDescending(x => x.DateAndTime)
                .Take(50)
                .Select(x =>
                    new RecentMatch(x.Endpoint,
                    GameStatistic.DateToString(x.DateAndTime), x))
                .ToArray());

            var result = QueryProcessor.ProcessGetRequest("/reports/recent-matches/100");

            Assert.AreEqual(answer, result);
        }

        [TestMethod]
        public void GetRecentMatchesLessThan0()
        {
            var answer = "[]";

            var result = QueryProcessor.ProcessGetRequest("/reports/recent-matches/-5");

            Assert.AreEqual(answer, result);
        }

        [TestMethod]
        public void GetPopularServer()
        {
            var answer = QueryProcessor.Json(QueryProcessor.AdvertiseServers
                .Select(x => new PopularServer(
                    x.Endpoint,
                    x.Info.Name,
                    GameStatistic.GetAverageMatchPerDay(x.Endpoint)))
                .OrderByDescending(x => x.AverageMatchPerDay)
                .Take(15)
                .ToArray());

            var result = QueryProcessor.ProcessGetRequest("/reports/popular-servers/15");
        }


        //[TestMethod]
        //public void GetBestPlayers()
        //{
        //    var endpoint = "7.7.7.7-1000";
        //    MultiAdd(endpoint,date1,4,gameServer1);
        //    var answer = QueryProcessor.Json();
        //}

        private static void MultiAdd(string endpoint, DateTime date, int n, GameServer gameServer)
        {
            gameServer.Endpoint = endpoint;
            gameServer.DateAndTime = date;
            for (var i = 0; i < n; i++)
            {
                    QueryProcessor.GameServers.Add(gameServer);
            }
        }
    }
}
