using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kontur.GameStats.Server.Tests
{
    [TestClass]
    public class PutRequestTests
    {
        private readonly QueryProcessor queryProcessor = new QueryProcessor();

        [TestMethod]
        public void AdvertiseRequest()
        {
            var requestString = new Uri("http://localhost:8080/servers/1.2.3.4-1111/info");
            var body = "{\"name\": \"] My P3rfect Server [\"," +
                       "\"gameModes\": [ \"DM\", \"TDM\" ]}";

            var result = queryProcessor.HandlePut(requestString, body);

            Assert.AreEqual(HttpStatusCode.Accepted, result.Status);
        }

        [TestMethod]
        public void SameAdvertiseRequest()
        {
            var requestString = new Uri("http://localhost:8080/servers/1.2.3.4-1111/info");
            var body = "{\"name\": \"] My P3rfect Server [\"," +
                       "\"gameModes\": [ \"DM\", \"TDM\" ]}";
            queryProcessor.HandlePut(requestString, body);
            var length = QueryProcessor.AdvertiseServers.Count;

            queryProcessor.HandlePut(requestString, body);

            Assert.AreEqual(length, QueryProcessor.AdvertiseServers.Count);
        }

        [TestMethod]
        public void EmtyAdvertiseRequest()
        {
            var requestString = new Uri("http://localhost:8080/servers//info");
            var body = "{\"name\": \"] My P3rfect Server [\"," +
                       "\"gameModes\": [ \"DM\", \"TDM\" ]}";

            var result = queryProcessor.HandlePut(requestString, body);

            Assert.AreEqual(HttpStatusCode.BadRequest, result.Status);
        }

        [TestMethod]
        public void PutMatches()
        {
            queryProcessor.HandlePut(new Uri("http://localhost:8080/servers/blabla-8080/info"),
                "{\"name\": \"] My P3rfect Server [\"," +
                "\"gameModes\": [ \"DM\", \"TDM\" ]}");

            var requestString = new Uri("http://localhost:8080/servers/blabla-8080/matches/2000-01-01T10:00:00Z");
            var body = "{\"map\": \"DM-HelloWorld\"," +
                       "\"gameMode\": \"DM\"," +
                       "\"fragLimit\": 20," +
                       "\"timeLimit\": 20," +
                       "\"timeElapsed\": 12.345678," +
                       "\"scoreboard\": [{\"name\": \"Player1\"," +
                       "\"frags\": 20,\"kills\": 21,\"deaths\": 3}," +
                       "{\"name\": \"Player2\",\"frags\": 2," +
                       "\"kills\": 2,\"deaths\": 21}]}";

            var result = queryProcessor.HandlePut(requestString, body);

            Assert.AreEqual(HttpStatusCode.Accepted, result.Status);
        }

        [TestMethod]
        public void PutNoAdveriseMatches()
        {
            var requestString = new Uri("http://localhost:8080/servers/1.1.1.1-1333/matches/2012-12-12T12:12:12Z");
            var body = "{\"map\": \"DM-HelloWorld\"," +
                       "\"gameMode\": \"DM\"," +
                       "\"fragLimit\": 20," +
                       "\"timeLimit\": 20," +
                       "\"timeElapsed\": 12.345678," +
                       "\"scoreboard\": [{\"name\": \"Player1\"," +
                       "\"frags\": 20,\"kills\": 21,\"deaths\": 3}," +
                       "{\"name\": \"Player2\",\"frags\": 2," +
                       "\"kills\": 2,\"deaths\": 21}]}";

            var result = queryProcessor.HandlePut(requestString, body);

            Assert.AreEqual(HttpStatusCode.BadRequest, result.Status);
        }
    }
}