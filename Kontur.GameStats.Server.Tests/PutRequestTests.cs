using System;
using System.Net;
using ExtensionsMethods;
using FluentAssertions;
using NUnit.Framework;

namespace Kontur.GameStats.Server.Tests
{
    [TestFixture]
    public class PutRequestTests
    {
        private QueryProcessor queryProcessor;

        [SetUp]
        public void SetUp()
        {
            queryProcessor = new QueryProcessor();
        }

        [Test]
        public void PutAdvertiseRequest()
        {
            var requestString = new Uri("http://localhost:8080/servers/1.2.3.4-1111/info");
            var body = "{\"name\": \"] My P3rfect Server [\"," +
                       "\"gameModes\": [ \"DM\", \"TDM\" ]}";

            var result = queryProcessor.HandlePut(requestString, body);

            result.Status.Should().Be(HttpStatusCode.Accepted);
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii());
        }

        [Test]
        public void SameAdvertiseRequest()
        {
            var requestString = new Uri("http://localhost:8080/servers/1.2.3.4-1111/info");
            var body = "{\"name\": \"] My P3rfect Server [\"," +
                       "\"gameModes\": [ \"DM\", \"TDM\" ]}";
            queryProcessor.HandlePut(requestString, body);
            var length = queryProcessor.AdvertiseServers.Count;

            var result = queryProcessor.HandlePut(requestString, body);

            result.Status.Should().Be(HttpStatusCode.Accepted);
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii());
            queryProcessor.AdvertiseServers.Count.Should().Be(length);
        }

        [Test]
        public void EmtyAdvertiseRequest()
        {
            var requestString = new Uri("http://localhost:8080/");
            var body = "{\"name\": \"] My P3rfect Server [\"," +
                       "\"gameModes\": [ \"DM\", \"TDM\" ]}";

            var result = queryProcessor.HandlePut(requestString, body);

            result.Status.Should().Be(HttpStatusCode.BadRequest);
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii());
        }

        [Test]
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

            result.Status.Should().Be(HttpStatusCode.Accepted);
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii());
        }

        [Test]
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

            result.Status.Should().Be(HttpStatusCode.BadRequest);
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii());
        }

        [Test]
        public void PutEmptyBody()
        {
            var requestString = new Uri("http://localhost:8080/servers/1.1.1.1-1333/matches/2012-12-12T12:12:12Z");

            var result = queryProcessor.HandlePut(requestString, "");

            result.Status.Should().Be(HttpStatusCode.BadRequest);
            result.Response.ShouldAllBeEquivalentTo("".GetBytesInAscii());
        }
    }
}