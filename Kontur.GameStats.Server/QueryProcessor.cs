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
        private readonly GameStatistic statistic = new GameStatistic();

        private readonly ServerDataBase dataBase;

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
            new Regex("{\"name\": \"[.]+\"," +"\"gameModes\": [.]+}", RegexOptions.Compiled);

        private static readonly Regex MatchBodyRegex =
            new Regex("{\"map\": \"[\\.]+\"," +
                      "\"gameMode\": \"[A-Z]+\"," +
                      "\"fragLimit\": [0-9]+," +
                      "\"timeLimit\": [0-9]+," +
                      "\"timeElapsed\": [0-9]+.[0-9]+," +
                      "\"scoreboard\": [\\.]+}", RegexOptions.Compiled);

        public QueryProcessor()
        {
            dataBase = new ServerDataBase();
        }

        public static List<AdvertiseQueryServer> AdvertiseServers { get; set; }
            = new List<AdvertiseQueryServer>();

        public static List<GameServer> GameServers { get; set; } = new List<GameServer>();

        public RequestHandlingResult HandleGet(Uri uri)
        {
            var requestAnswer = ProcessGetRequest(uri.LocalPath);
            var result = new RequestHandlingResult();
            switch (requestAnswer)
            {
                case "Bad Request":
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
                case "Not Found":
                    return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
                default:
                    return RequestHandlingResult.Successfull(Encoding.ASCII.GetBytes(requestAnswer));
            }
        }

        public RequestHandlingResult HandlePut(Uri uri, string body)
        {
            var requestAnswer = ProcessPutRequest(uri.LocalPath, body);
            return requestAnswer
                ? RequestHandlingResult.Successfull(new byte[0])
                : RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
        }

        public bool ProcessPutRequest(string requestString, string body)
        {
            var splitedString = PutRequestRegex.Split(requestString);

            if (splitedString.Length <= 3)// ||
                //!InfoBodyRegex.IsMatch(body) || !MatchBodyRegex.IsMatch(body))
                return false;

            switch (splitedString[3])
            {
                case "info":
                    return PutInfo(splitedString[2], body);
                case "matches":
                    return PutMatches(splitedString[2], body, splitedString[4]);
                default:
                    return false;
            }
        }

        private bool PutInfo(string endpoint, string body)
        {
            var info = JsonConvert.DeserializeObject<Information>(body);
            var advertRequest = new AdvertiseQueryServer(endpoint, info);
            var index = AdvertiseServers.IndexOf(advertRequest);
            if (index >= 0)
                AdvertiseServers[index] = advertRequest;
            else
                AdvertiseServers.Add(advertRequest);
            dataBase.AddAdvertServer(advertRequest);
            return true;
        }

        private bool PutMatches(string endpoint, string body, string date)
        {
            if (AdvertiseServers.All(x => x.Endpoint != endpoint)) return false;
            var gameServer = JsonConvert.DeserializeObject<GameServer>(body);
            gameServer.Endpoint = endpoint;
            gameServer.DateAndTime = DateTime.Parse(date);
            GameServers.Add(gameServer);
            dataBase.AddGameServer(gameServer);
            return true;
        }

        public string ProcessGetRequest(string requestString)
        {
            var splitedRequest = GetRequestRegex.Split(requestString);

            if (splitedRequest.Length < 2) return "Bad Request";

            switch (splitedRequest[1])
            {
                case "servers":
                    return GetServerInformation(splitedRequest[2]);
                case "players":
                    return GetPlayersStatistic(splitedRequest[2]);
                case "reports":
                    return GetReport(splitedRequest[2]);
                default:
                    return "Bad Request";
            }
        }

        private string GetPlayersStatistic(string request)
        {
            var splitRequest = request.Split('/');
            return Json(statistic.GetPlayerStatistic(splitRequest[0].ToLower()));
        }

        private string GetReport(string request)
        {
            const string pattern = "^(recent-matches|best-players|popular-servers)/?";
            var splitRequest = Regex.Split(request, pattern)
                .Where(x => x != "")
                .ToArray();
            var n = 5;
            if (splitRequest.Length > 1) n = DefineN(int.Parse(splitRequest[1]));
            if (n == 0) return Json(new string[] {});
            switch (splitRequest[0])
            {
                case "recent-matches":
                    return Json(statistic.GetRecentMatches(n));
                case "best-players":
                    return Json(statistic.GetBestPlayers(n));
                case "popular-servers":
                    return Json(statistic.GetPopularServers(n));
                default:
                    return "Bad Request";
            }
        }

        private static int DefineN(int n)
        {
            if (n <= 0) return 0;
            return n >= 50 ? 50 : n;
        }

        private string GetServerInformation(string request)
        {
            var splitedRequest = ServerInfoRegex.Split(request);
            if (splitedRequest[1] == "info") return Json(AdvertiseServers.ToArray());
            switch (splitedRequest[2])
            {
                case "info":
                    return GetAdvertServer(splitedRequest[1]);
                case "matches":
                {
                    var dateAndTime = DateTime.Parse(splitedRequest[3]);
                    return GetAdvertMatch(splitedRequest[1], dateAndTime);
                }
                case "stats":
                    return Json(statistic.GetServerStatistic(splitedRequest[1]));
                default:
                    return "Bad Request";
            }
        }

        private string GetAdvertServer(string enpoint)
        {
            if (AdvertiseServers.All(x => x.Endpoint != enpoint)) return "Not Found";
            return Json(AdvertiseServers
                .Where(x => x.Endpoint == enpoint)
                .Select(x => x.Info)
                .ToArray()[0]);
        }

        private string GetAdvertMatch(string endpoint, DateTime date)
        {
            if (GameServers.All(x => x.Endpoint != endpoint)
                || GameServers.All(x => x.DateAndTime != date))
                return "Not Found";
            return Json(GameServers
                .Where(x => x.Endpoint == endpoint)
                .Where(x => x.DateAndTime == date).ToArray()[0]);
        }

        public string Json(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}