using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ExtensionsMethods;

namespace Kontur.GameStats.Server
{
    public class QueryProcessor : IStatServerRequestHandler
    {
        private static readonly Regex PutRequestRegex =
            new Regex("/(servers)/" +
                      "([\\.a-zA-Z0-9]+-[0-9]{1,4})/" +
                      "(info|matches)/?" +
                      "([0-9]{4}-[0-9]{2}-[0-9]{2}T" +
                      "[0-9]{2}:[0-9]{2}:[0-9]{2}Z)?", RegexOptions.Compiled);

        private static readonly Regex GetRequestRegex =
            new Regex("/(servers|players|reports)/", RegexOptions.Compiled);

        private static readonly Regex ServerInfoRegex =
            new Regex("([\\.a-zA-Z0-9]+-?[0-9]{1,4}|info)/?" +
                      "(info|matches|stats)?/?" +
                      "([0-9]{4}-[0-9]{2}-[0-9]{2}T" +
                      "[0-9]{2}:[0-9]{2}:[0-9]{2}Z)?", RegexOptions.Compiled);

        private static readonly Regex ReportRegex =
            new Regex("^(recent-matches|best-players|popular-servers)/?", RegexOptions.Compiled);

        private readonly IServerDataBase dataBase;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IGameStatistic statistic;

        public ConcurrentDictionary<string, AdvertiseQueryServer> AdvertiseServers { get; }

        public BlockingCollection<GameServer> GameServers { get; }

        public QueryProcessor(IServerDataBase dataBase, IGameStatistic gameStatistic, IJsonSerializer serializer)
        {
            this.dataBase = dataBase;
            statistic = gameStatistic;
            jsonSerializer = serializer;
            AdvertiseServers = dataBase.ReadAdvertServers();
            GameServers = dataBase.ReadGameServers();
        }

        public RequestHandlingResult HandleGet(Uri uri)
        {
            return ProcessGetRequest(uri.LocalPath);
        }

        public RequestHandlingResult HandlePut(Uri uri, string body)
        {
            return ProcessPutRequest(uri.LocalPath, body);
        }

        private RequestHandlingResult ProcessPutRequest(string requestString, string body)
        {
            var splitedString = PutRequestRegex.Split(requestString);

            if (splitedString.Length <= 3 || body.Length == 0)
                return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);

            switch (splitedString[3])
            {
                case "info":
                    return PutInfo(splitedString[2], body);
                case "matches":
                    return PutMatches(splitedString[2], body, splitedString[4]);
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
        }

        private RequestHandlingResult PutInfo(string endpoint, string body)
        {
            Information info;
            if (!jsonSerializer.TryDeserialize(body, out info) || !CheckInfoField(body))
                return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            var advertRequest = new AdvertiseQueryServer(endpoint, info);
            if (AdvertiseServers.ContainsKey(endpoint))
                dataBase.UpdateAdvertServer(advertRequest);
            else
                dataBase.AddAdvertServer(advertRequest);
            AdvertiseServers.AddOrUpdate(endpoint, advertRequest, ((s, server) => advertRequest));
            return RequestHandlingResult.Successfull(new byte[0]);
        }

        private RequestHandlingResult PutMatches(string endpoint, string body, string date)
        {
            GameServer gameServer;
            if (!AdvertiseServers.ContainsKey(endpoint) ||
                !CheckMatchField(body) ||
                !jsonSerializer.TryDeserialize(body, out gameServer) ||
                !CheckGameMode(endpoint, gameServer.GameMode))
                return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            gameServer.Endpoint = endpoint;
            gameServer.DateAndTime = DateTime.Parse(date, null, DateTimeStyles.RoundtripKind);
            GameServers.Add(gameServer);
            dataBase.AddGameServer(gameServer);
            return RequestHandlingResult.Successfull(new byte[0]);
        }

        private bool CheckInfoField(string infoBody)
        {
            return infoBody.Contains("\"name\":") &&
                   infoBody.Contains("\"gameModes\":");
        }

        private bool CheckMatchField(string matchBody)
        {
            return matchBody.Contains("\"map\":") &&
                   matchBody.Contains("\"gameMode\":") &&
                   matchBody.Contains("\"fragLimit\":") &&
                   matchBody.Contains("\"timeLimit\":") &&
                   matchBody.Contains("\"timeElapsed\":") &&
                   matchBody.Contains("\"scoreboard\":");
        }

        private bool CheckGameMode(string endpoint, string gameMode)
        {
            return AdvertiseServers.Where(x => x.Key == endpoint)
                .Select(x => x.Value.Info.GameModes).Any(x => x.Contains(gameMode));
        }

        private RequestHandlingResult ProcessGetRequest(string requestString)
        {
            var splitedRequest = GetRequestRegex.Split(requestString);
            if (splitedRequest.Length < 2) return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);

            var typeOfRequest = new Dictionary<string, Func<string, RequestHandlingResult>>
            {
                {"servers", GetServerInformation},
                {"players", GetPlayersStatistic},
                {"reports", GetReport}
            };

            return typeOfRequest.ContainsKey(splitedRequest[1])
                ? typeOfRequest[splitedRequest[1]](splitedRequest[2])
                : RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
        }

        private RequestHandlingResult GetPlayersStatistic(string request)
        {
            var splitRequest = request.Split('/');
            var name = splitRequest[0];
            var games = GameServers
                .Where(x => x.Scoreboard.Any(y => y.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();
            if (games.Length == 0)
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            var serializStatistic = jsonSerializer.Serialize(statistic.GetPlayerStatistic(name, games));
            return RequestHandlingResult.Successfull(serializStatistic.GetBytesInAscii());
        }

        private RequestHandlingResult GetReport(string request)
        {
            var splitRequest = ReportRegex.Split(request)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
            var n = 5;
            if (splitRequest.Length > 1)
                n = GetRightCountOfItems(int.Parse(splitRequest[1]));
            if (n == 0 || GameServers.Count == 0)
            {
                var emptyAnswer = jsonSerializer.Serialize(new string[0]);
                return RequestHandlingResult.Successfull(emptyAnswer.GetBytesInAscii());
            }
            object reportResult;
            var methods = new Dictionary<string, Func<int, BlockingCollection<GameServer>, object>>
            {
                {"recent-matches", (count, games) => statistic.GetRecentMatches(count, games)},
                {"best-players", (count, games) => statistic.GetBestPlayers(count, games)}
            };
            switch (splitRequest[0])
            {
                case "recent-matches":
                    reportResult = methods[splitRequest[0]](n, GameServers);
                    break;
                case "best-players":
                    reportResult = methods[splitRequest[0]](n, GameServers);
                    break;
                case "popular-servers":
                    reportResult = statistic.GetPopularServers(n, AdvertiseServers, GameServers);
                    break;
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
            var serializeReport = jsonSerializer.Serialize(reportResult);
            return RequestHandlingResult.Successfull(serializeReport.GetBytesInAscii());
        }

        private static int GetRightCountOfItems(int countOfItems)
        {
            if (countOfItems <= 0) return 0;
            return countOfItems >= 50 ? 50 : countOfItems;
        }

        private RequestHandlingResult GetServerInformation(string request)
        {
            var splitedRequest = ServerInfoRegex.Split(request);
            if (splitedRequest[1] == "info")
            {
                var serializeServers = jsonSerializer.Serialize(AdvertiseServers.Values.ToArray());
                return RequestHandlingResult.Successfull(serializeServers.GetBytesInAscii());
            }
            var endpoint = splitedRequest[1];
            var neededGames = GameServers.Where(x => x.Endpoint == endpoint).ToArray();
            var neededAdvertServer = AdvertiseServers.Where(x => x.Key == endpoint).ToArray();
            if (!neededAdvertServer.Any() && neededGames.Length == 0)
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            object informations;
            switch (splitedRequest[2])
            {
                case "info":
                    informations = neededAdvertServer[0].Value.Info;
                    break;
                case "matches":
                    return GetAdvertMatch(splitedRequest[3], neededGames);
                case "stats":
                    informations = statistic.GetServerStatistic(neededGames);
                    break;
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
            var serializeInformation = jsonSerializer.Serialize(informations);
            return RequestHandlingResult.Successfull(serializeInformation.GetBytesInAscii());
        }

        private RequestHandlingResult GetAdvertMatch(string date, GameServer[] games)
        {
            DateTime dateTime;
            try
            {
                dateTime = DateTime.Parse(date, null, DateTimeStyles.RoundtripKind);
            }
            catch (Exception)
            {
                return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
            var match = games.Where(x => x.DateAndTime == dateTime).ToArray();
            if (match.Length == 0)
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            var serializeMatch = jsonSerializer.Serialize(match[0]);
            return RequestHandlingResult.Successfull(serializeMatch.GetBytesInAscii());
        }
    }
}