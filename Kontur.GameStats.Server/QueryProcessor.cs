using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

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

        private static readonly Regex InfoBodyRegex =
            new Regex("{\"name\": \"[.]+\"," + "\"gameModes\": [.]+}", RegexOptions.Compiled);

        private static readonly Regex MatchBodyRegex =
            new Regex("{\"map\": \"[\\.]+\"," +
                      "\"gameMode\": \"[A-Z]+\"," +
                      "\"fragLimit\": [0-9]+," +
                      "\"timeLimit\": [0-9]+," +
                      "\"timeElapsed\": [0-9]+.[0-9]+," +
                      "\"scoreboard\": [\\.]+}", RegexOptions.Compiled);

        private readonly ServerDataBase dataBase;
        private readonly GameStatistic statistic = new GameStatistic();

        public QueryProcessor()
        {
            dataBase = new ServerDataBase();
        }

        public static List<AdvertiseQueryServer> AdvertiseServers { get; set; }
            = new List<AdvertiseQueryServer>();

        public static List<GameServer> GameServers { get; set; } = new List<GameServer>();

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

            if (splitedString.Length <= 3) // ||
                //!InfoBodyRegex.IsMatch(body) || !MatchBodyRegex.IsMatch(body))
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
            var info = JsonConvert.DeserializeObject<Information>(body);
            var advertRequest = new AdvertiseQueryServer(endpoint, info);
            var index = AdvertiseServers.IndexOf(advertRequest);
            if (index >= 0)
                AdvertiseServers[index] = advertRequest;
            else
                AdvertiseServers.Add(advertRequest);
            dataBase.AddAdvertServer(advertRequest);
            return RequestHandlingResult.Successfull(new byte[0]);
        }

        private RequestHandlingResult PutMatches(string endpoint, string body, string date)
        {
            if (AdvertiseServers.All(x => x.Endpoint != endpoint))
                return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            var gameServer = JsonConvert.DeserializeObject<GameServer>(body);
            gameServer.Endpoint = endpoint;
            gameServer.DateAndTime = DateTime.Parse(date);
            GameServers.Add(gameServer);
            dataBase.AddGameServer(gameServer);
            return RequestHandlingResult.Successfull(new byte[0]);
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
            return GameServers.Any(x => x.Scoreboard.Any(y => y.Name == name))
                ? RequestHandlingResult.Successfull(GetBytes(Json(statistic.GetPlayerStatistic(splitRequest[0].ToLower()))))
                : RequestHandlingResult.Fail(HttpStatusCode.NotFound);
        }

        private RequestHandlingResult GetReport(string request)
        {
            const string pattern = "^(recent-matches|best-players|popular-servers)/?";
            var splitRequest = Regex.Split(request, pattern)
                .Where(x => x != "")
                .ToArray();
            var n = 5;
            if (splitRequest.Length > 1)
                n = DefineN(int.Parse(splitRequest[1]));
            if (n == 0 || GameServers.Count == 0)
                return RequestHandlingResult.Successfull(GetBytes(Json(new string[] {})));
            switch (splitRequest[0])
            {
                case "recent-matches":
                    return RequestHandlingResult.Successfull(GetBytes(Json(statistic.GetRecentMatches(n))));
                case "best-players":
                    return RequestHandlingResult.Successfull(GetBytes(Json(statistic.GetBestPlayers(n))));
                case "popular-servers":
                    return RequestHandlingResult.Successfull(GetBytes(Json(statistic.GetPopularServers(n))));
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
        }

        private static int DefineN(int n)
        {
            if (n <= 0) return 0;
            return n >= 50 ? 50 : n;
        }

        private RequestHandlingResult GetServerInformation(string request)
        {
            var splitedRequest = ServerInfoRegex.Split(request);
            if (splitedRequest[1] == "info")
                return RequestHandlingResult.Successfull(GetBytes(Json(AdvertiseServers.ToArray())));
            var endpoint = splitedRequest[1];
            if (AdvertiseServers.All(x => x.Endpoint != endpoint) &&
                GameServers.All(x => x.Endpoint != endpoint))
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            switch (splitedRequest[2])
            {
                case "info":
                    return RequestHandlingResult.Successfull(GetBytes(GetAdvertServer(endpoint)));
                case "matches":
                    var dateAndTime = DateTime.Parse(splitedRequest[3]);
                    return GetAdvertMatch(endpoint, dateAndTime);
                case "stats":
                    return RequestHandlingResult.Successfull(GetBytes(Json(statistic.GetServerStatistic(endpoint))));
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
        }

        private string GetAdvertServer(string enpoint)
        {
            return Json(AdvertiseServers
                .Where(x => x.Endpoint == enpoint)
                .Select(x => x.Info)
                .ToArray()[0]);
        }

        private RequestHandlingResult GetAdvertMatch(string endpoint, DateTime date)
        {
            if (GameServers.All(x => x.DateAndTime != date))
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            return RequestHandlingResult.Successfull(GetBytes(Json(GameServers
                .Where(x => x.Endpoint == endpoint)
                .Where(x => x.DateAndTime == date).ToArray()[0])));
        }

        public string Json(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public byte[] GetBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public string GetStringFromByteArray(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}