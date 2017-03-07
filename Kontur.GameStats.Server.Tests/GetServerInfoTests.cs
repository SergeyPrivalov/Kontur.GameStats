using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server.Tests
{
    [TestClass]
    public class GetServerInfoTests
    {
        private readonly AdvertiseQueryServer firstServer =
            new AdvertiseQueryServer("167.42.23.32-1337",
                new Information
                {
                    Name = "] My P3rfect Server [",
                    GameModes = new[] {"DM", "TDM"}
                });

        private readonly GameServer gameServer =
            new GameServer("DM-HelloWorld", "DM", 20, 20, 12.345678,
                new[]
                {
                    new Player("Player1", 20, 21, 3),
                    new Player("Player2", 2, 2, 21)
                });

        private readonly QueryProcessor queryProcessor = new QueryProcessor();

        private readonly AdvertiseQueryServer secondServer =
            new AdvertiseQueryServer("62.210.26.88-1337",
                new Information
                {
                    Name = ">> Sniper Heaven <<",
                    GameModes = new[] {"DM"}
                });

        private readonly JsonSerializer jsonSerializer = new JsonSerializer();


        [TestMethod]
        public void GetServerInfo()
        {
            queryProcessor.AdvertiseServers.Add(firstServer);
            queryProcessor.AdvertiseServers.Add(secondServer);
            var info = jsonSerializer.Serialize(queryProcessor.AdvertiseServers.ToArray());
            var result = queryProcessor.HandleGet(new Uri("http://localhost:8080/servers/info"));

            Assert.AreEqual(HttpStatusCode.Accepted, result.Status);
            Assert.AreEqual(info, queryProcessor.GetStringFromByteArray(result.Response));
        }

        [TestMethod]
        public void GetAdvertInfo()
        {
            var info = "{\"name\":\"] My P3rfect Server [\"," +
                       "\"gameModes\":[\"DM\",\"TDM\"]}";
            var uri = new Uri("http://localhost:8080/servers/167.42.23.32-1337/info");
            queryProcessor.HandlePut(uri, info);

            var result = queryProcessor.HandleGet(uri);

            Assert.AreEqual(HttpStatusCode.Accepted, result.Status);
            Assert.AreEqual(info, queryProcessor.GetStringFromByteArray(result.Response));
        }

        [TestMethod]
        public void GetNotAdvertInfo()
        {
            var result =
                queryProcessor.HandleGet(new Uri("http://localhost:8080/servers/17.42.3.3-1337/info"));

            Assert.AreEqual(HttpStatusCode.NotFound, result.Status);
            Assert.AreEqual("", queryProcessor.GetStringFromByteArray(result.Response));
        }

        [TestMethod]
        public void GetMatchInfo()
        {
            queryProcessor.AdvertiseServers.Add(firstServer);
            gameServer.Endpoint = "167.42.23.32-1337";
            gameServer.DateAndTime = new DateTime(2017, 11, 22, 20, 17, 00);
            queryProcessor.GameServers.Add(gameServer);
            var info = JsonConvert.SerializeObject(gameServer);


            var result = queryProcessor.HandleGet(
                new Uri("http://localhost:8080/servers/167.42.23.32-1337/matches/2017-11-22T15:17:00Z"));

            Assert.AreEqual(HttpStatusCode.Accepted, result.Status);
            Assert.AreEqual(info, queryProcessor.GetStringFromByteArray(result.Response));
        }

        [TestMethod]
        public void GetNoAdvertMatchInfo()
        {
            var result = queryProcessor.HandleGet(
                new Uri("http://localhost:8080/servers/1.2.2.8-1337/matches/2017-01-22T15:17:00Z"));

            Assert.AreEqual(HttpStatusCode.NotFound, result.Status);
            Assert.AreEqual("", queryProcessor.GetStringFromByteArray(result.Response));
        }

        [TestMethod]
        public void GetNotPutMatch()
        {
            var result = queryProcessor.HandleGet(new Uri("http://localhost:8080/servers/167.42.23.32-1337/matches/2017-01-22T15:17:00Z"));

            Assert.AreEqual(HttpStatusCode.NotFound,result.Status);
        }

        [TestMethod]
        public void WrongRequest()
        {
            var result = queryProcessor.HandleGet(new Uri("http://localhost:8080/servers/167.42.23.32-1337/matches"));

            Assert.AreEqual(HttpStatusCode.BadRequest, result.Status);
        }
    }
}