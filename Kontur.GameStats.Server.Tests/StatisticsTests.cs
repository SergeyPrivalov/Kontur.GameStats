using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                    new Player("Player1", 20, 21, 3),
                    new Player("Player2", 2, 2, 21)
                });

        private GameServer gameServer2 =
            new GameServer("DM-Hello", "TDM", 30, 30, 22.345678,
                new[]
                {
                    new Player("Player1", 20, 21, 3),
                    new Player("Player2", 2, 2, 21),
                    new Player("Player3", 2, 2, 21),
                    new Player("Player4", 2, 2, 21)
                });

        private GameServer gameServer3 =
            new GameServer("DM", "DM", 40, 40, 32.345678,
                new[]
                {
                    new Player("Player1", 20, 21, 3),
                    new Player("Player2", 2, 2, 21),
                    new Player("Player3", 2, 2, 21)
                });

        DateTime date1 = new DateTime(2020,01,22,15,16,22);
        DateTime date2 = new DateTime(2020,01,23,14,00,00);
        DateTime date3 = new DateTime(2020,01,24,00,00,00);

        [TestMethod]
        public void GetServersStats()
        {
            var endpoint = "12.12.12.12-1333";
            QueryProcessor.AdvertiseServers.Add(firstServer);
            MultiAdd(endpoint, date1, 1, gameServer1);
            MultiAdd(endpoint, date1, 1, gameServer3);
            MultiAdd(endpoint, date2, 1, gameServer2);
            var answer = "{\"totalMatchesPlayed\":3," +
                         "\"maximumMatchesPerDay\":2," +
                         "\"averageMatchesPerDay\":1.5," +
                         "\"maximumPopulation\":4," +
                         "\"averagePopulation\":3.0," +
                         "\"top5GameModes\":[\"DM\",\"TDM\"]," +
                         "\"top5Maps\":[\"DM-HelloWorld\",\"DM\",\"DM-Hello\"]}";

            var result = QueryProcessor
                .ProcessGetRequest("/servers/12.12.12.12-1333/stats");

            Assert.AreEqual(answer, result);
        }

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
