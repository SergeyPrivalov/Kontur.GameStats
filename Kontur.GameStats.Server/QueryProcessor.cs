﻿using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

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

        private readonly ServerDataBase dataBase;
        private readonly JsonSerializer jsonSerializer;
        private readonly GameStatistic statistic;

        public ConcurrentDictionary<string, AdvertiseQueryServer> AdvertiseServers { get; }

        public BlockingCollection<GameServer> GameServers { get; }

        public QueryProcessor()
        {
            dataBase = new ServerDataBase();
            statistic = new GameStatistic();
            jsonSerializer = new JsonSerializer();
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

            switch (splitedRequest[1])
            {
                case "servers":
                    return GetServerInformation(splitedRequest[2]);
                case "players":
                    return GetPlayersStatistic(splitedRequest[2]);
                case "reports":
                    return GetReport(splitedRequest[2]);
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
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
            return RequestHandlingResult.Successfull(
                GetBytes(jsonSerializer.Serialize(statistic.GetPlayerStatistic(name, games))));
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
                return RequestHandlingResult.Successfull(GetBytes(jsonSerializer.Serialize(new string[0])));
            object reportResult;
            switch (splitRequest[0])
            {
                case "recent-matches":
                    reportResult = statistic.GetRecentMatches(n, GameServers);
                    break;
                case "best-players":
                    reportResult = statistic.GetBestPlayers(n, GameServers);
                    break;
                case "popular-servers":
                    reportResult = statistic.GetPopularServers(n, AdvertiseServers, GameServers);
                    break;
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
            return RequestHandlingResult.Successfull(GetBytes(jsonSerializer.Serialize(reportResult)));
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
                return RequestHandlingResult.Successfull(GetBytes(jsonSerializer.Serialize(AdvertiseServers.ToArray())));
            var endpoint = splitedRequest[1];
            var neededGames = GameServers.Where(x => x.Endpoint == endpoint).ToArray();
            var neededAdvertServer = AdvertiseServers.Where(x => x.Key == endpoint).ToArray();
            if (!neededAdvertServer.Any() && neededGames.Length == 0)
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            switch (splitedRequest[2])
            {
                case "info":
                    return RequestHandlingResult.Successfull(GetBytes(GetAdvertServer(neededAdvertServer[0].Value)));
                case "matches":
                    return GetAdvertMatch(splitedRequest[3], neededGames);
                case "stats":
                    return
                        RequestHandlingResult.Successfull(
                            GetBytes(jsonSerializer.Serialize(statistic.GetServerStatistic(neededGames))));
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
        }

        private string GetAdvertServer(AdvertiseQueryServer advertServer)
        {
            return jsonSerializer.Serialize(advertServer.Info);
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
            if (GameServers.All(x => x.DateAndTime != dateTime))
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            return
                RequestHandlingResult.Successfull(
                    GetBytes(jsonSerializer.Serialize(games.First(x => x.DateAndTime == dateTime))));
        }

        private byte[] GetBytes(string str)
        {
            return Encoding.Unicode.GetBytes(str);
        }

        public string GetStringFromByteArray(byte[] bytes)
        {
            return Encoding.Unicode.GetString(bytes);
        }
    }
}