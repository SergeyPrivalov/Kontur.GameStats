using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server.Tests
{
    [TestClass]
    public class PutRequestTests
    {
        [TestMethod]
        public void AdvertiseRequest()
        {
            var requestString = "/servers/1.2.3.4-1111/info";
            var body = "{\"name\": \"] My P3rfect Server [\"," +
                       "\"gameModes\": [ \"DM\", \"TDM\" ]}";

            var result = QueryProcessor.ProcessPutRequest(requestString, body);

            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void SameAdvertiseRequest()
        {
            var requestString = "/servers/1.2.3.4-1111/info";
            var body = "{\"name\": \"] My P3rfect Server [\"," +
                       "\"gameModes\": [ \"DM\", \"TDM\" ]}";
            QueryProcessor.ProcessPutRequest(requestString, body);
            var length = QueryProcessor.AdvertiseServers.Count;

            QueryProcessor.ProcessPutRequest(requestString, body);

            Assert.AreEqual(length, QueryProcessor.AdvertiseServers.Count);
        }

        [TestMethod]
        public void EmtyAdvertiseRequest()
        {
            var requestString = "/servers//info";
            var body = "{\"name\": \"] My P3rfect Server [\"," +
                       "\"gameModes\": [ \"DM\", \"TDM\" ]}";

            var result = QueryProcessor.ProcessPutRequest(requestString, body);

            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void PutMatches()
        {
            QueryProcessor.ProcessPutRequest("/servers/blabla-8080/info",
                "{\"name\": \"] My P3rfect Server [\"," +
                "\"gameModes\": [ \"DM\", \"TDM\" ]}");

            var requestString = "/servers/blabla-8080/matches/<timestamp>";
            var body = "{\"map\": \"DM-HelloWorld\"," +
                       "\"gameMode\": \"DM\"," +
                       "\"fragLimit\": 20," +
                       "\"timeLimit\": 20," +
                       "\"timeElapsed\": 12.345678," +
                       "\"scoreboard\": [{\"name\": \"Player1\"," +
                       "\"frags\": 20,\"kills\": 21,\"deaths\": 3}," +
                       "{\"name\": \"Player2\",\"frags\": 2," +
                       "\"kills\": 2,\"deaths\": 21}]}";

            var result = QueryProcessor.ProcessPutRequest(requestString, body);

            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void PutNoAdveriseMatches()
        {
            var requestString = "/servers/1.1.1.1-1333/matches/<timestamp>";
            var body = "{\"map\": \"DM-HelloWorld\"," +
                       "\"gameMode\": \"DM\"," +
                       "\"fragLimit\": 20," +
                       "\"timeLimit\": 20," +
                       "\"timeElapsed\": 12.345678," +
                       "\"scoreboard\": [{\"name\": \"Player1\"," +
                       "\"frags\": 20,\"kills\": 21,\"deaths\": 3}," +
                       "{\"name\": \"Player2\",\"frags\": 2," +
                       "\"kills\": 2,\"deaths\": 21}]}";

            var result = QueryProcessor.ProcessPutRequest(requestString, body);

            Assert.AreEqual(false, result);
        }
    }
}
