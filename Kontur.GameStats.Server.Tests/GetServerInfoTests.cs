using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server.Tests
{
    [TestClass]
    public class GetServerInfoTests
    {
        private AdvertiseQueryServer firstServer =
            new AdvertiseQueryServer("167.42.23.32-1337",
                new Information
                {
                    Name = "] My P3rfect Server [",
                    GameModes = new[] {"DM", "TDM"}
                });

        private AdvertiseQueryServer secondServer =
            new AdvertiseQueryServer("62.210.26.88-1337",
                new Information
                {
                    Name = ">> Sniper Heaven <<",
                    GameModes = new[] {"DM"}
                });

        private GameServer gameServer =
           new GameServer("DM-HelloWorld", "DM", 20, 20, 12.345678,
               new[] {new Player("Player1", 20, 21, 3),
                    new Player("Player2", 2, 2, 21)});

        [TestMethod]
        public void GetServerInfo()
        {
            QueryProcessor.AdvertiseServers.Add(firstServer);
            QueryProcessor.AdvertiseServers.Add(secondServer);
            var info = QueryProcessor.Json(QueryProcessor.AdvertiseServers.ToArray());

            var result = QueryProcessor.ProcessGetRequest("/servers/info");

            Assert.AreEqual(2, QueryProcessor.AdvertiseServers.Count);
            Assert.AreEqual(info, result);
        }

        [TestMethod]
        public void GetAdvertInfo()
        {
            const string info = "{\"name\":\"] My P3rfect Server [\"," +
                                "\"gameModes\":[\"DM\",\"TDM\"]}";

            var result =
                QueryProcessor.ProcessGetRequest("/servers/167.42.23.32-1337/info");

            Assert.AreEqual(info, result);
        }

        [TestMethod]
        public void GetNotAdvertInfo()
        {
            const string info = "Not Found";

            var result =
                QueryProcessor.ProcessGetRequest("/servers/17.42.3.3-1337/info");

            Assert.AreEqual(info, result);
        }

        [TestMethod]
        public void GetMatchInfo()
        {
            QueryProcessor.AdvertiseServers.Add(firstServer);
            gameServer.Endpoint = "167.42.23.32-1337";
            gameServer.DateAndTime = new DateTime(2017,11,22,15,17,00);
            QueryProcessor.GameServers.Add(gameServer);
            var info = JsonConvert.SerializeObject(gameServer);

            var result = QueryProcessor.
                ProcessGetRequest("/servers/167.42.23.32-1337/matches/" +
                                  "2017-11-22T15:17:00Z");

            Assert.AreEqual(info, result);
        }

        [TestMethod]
        public void GetNoAdvertMatchInfo()
        {
            var info = "Not Found";

            var result =
                QueryProcessor.ProcessGetRequest("/servers/1.2.2.8-1337/matches/2017-01-22T15:17:00Z");

            Assert.AreEqual(info, result);
        }
    }
}
