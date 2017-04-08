using System;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using ExtensionsMethods;
using FluentAssertions;
using NUnit.Framework;

namespace Kontur.GameStats.Server.Tests
{
    [TestFixture]
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

        private readonly AdvertiseQueryServer secondServer =
            new AdvertiseQueryServer("62.210.26.88-1337",
                new Information
                {
                    Name = ">> Sniper Heaven <<",
                    GameModes = new[] {"DM"}
                });

        private JsonSerializer jsonSerializer;
        private QueryProcessor queryProcessor;

        [SetUp]
        public void SetUp()
        {
            jsonSerializer = new JsonSerializer();
            queryProcessor = new QueryProcessor(new ServerDataBase(), new GameStatistic(), jsonSerializer);
        }

        [Test]
        public void GetServerInfo()
        {
            queryProcessor.AdvertiseServers.AddOrUpdate(firstServer.Endpoint, firstServer, (s, server) => firstServer);
            queryProcessor.AdvertiseServers.AddOrUpdate(secondServer.Endpoint, secondServer, (s, server) => secondServer);
            var info = jsonSerializer.Serialize(queryProcessor.AdvertiseServers.Values.ToArray());

            var result = queryProcessor.HandleGet(new Uri("http://localhost:8080/servers/info"));


            result.Status.Should().Be(HttpStatusCode.Accepted);
            result.Response.ShouldAllBeEquivalentTo(info.GetBytesInAscii());
        }

        [Test]
        public void GetAdvertInfo()
        {
            var info = "{\"name\":\"] My P3rfect Server [\"," +
                       "\"gameModes\":[\"DM\",\"TDM\"]}";
            var uri = new Uri("http://localhost:8080/servers/167.42.23.32-1337/info");
            queryProcessor.HandlePut(uri, info);

            var result = queryProcessor.HandleGet(uri);

            result.Status.Should().Be(HttpStatusCode.Accepted);
            result.Response.ShouldAllBeEquivalentTo(info.GetBytesInAscii());
        }

        [Test]
        public void GetNotAdvertInfo()
        {
            var result = queryProcessor.HandleGet(new Uri("http://localhost:8080/servers/17.42.3.3-1337/info"));

            result.Status.Should().Be(HttpStatusCode.NotFound);
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii(),
                "because this server didn't send advertise request");
        }

        [Test]
        public void GetMatchInfo()
        {
            queryProcessor.AdvertiseServers.AddOrUpdate(firstServer.Endpoint, firstServer, (s, server) => firstServer);
            gameServer.Endpoint = "167.42.23.32-1337";
            gameServer.DateAndTime = new DateTime(2017, 11, 22, 20, 17, 00);
            queryProcessor.GameServers.Add(gameServer);
            var info = JsonConvert.SerializeObject(gameServer);

            var result = queryProcessor.HandleGet(
                new Uri("http://localhost:8080/servers/167.42.23.32-1337/matches/2017-11-22T20:17:00Z"));

            result.Status.Should().Be(HttpStatusCode.Accepted);
            result.Response.ShouldAllBeEquivalentTo(info.GetBytesInAscii());
        }

        [Test]
        public void GetNoAdvertMatchInfo()
        {
            var result = queryProcessor.HandleGet(
                new Uri("http://localhost:8080/servers/1.2.2.8-1337/matches/2017-01-22T15:17:00Z"));

            result.Status.Should().Be(HttpStatusCode.NotFound, "because not advert server send statistic");
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii());
        }

        [Test]
        public void GetNotPutMatch()
        {
            var result =
                queryProcessor.HandleGet(
                    new Uri("http://localhost:8080/servers/167.42.23.32-1337/matches/2017-01-22T15:17:00Z"));

            result.Status.Should().Be(HttpStatusCode.NotFound, "because this match didn't put");
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii());
        }

        [Test]
        public void WrongRequest()
        {
            var result = queryProcessor.HandleGet(new Uri("http://localhost:8080/servers/167.42.23.32-1337/matches"));

            result.Status.Should().Be(HttpStatusCode.BadRequest);
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii());
        }
    }
}