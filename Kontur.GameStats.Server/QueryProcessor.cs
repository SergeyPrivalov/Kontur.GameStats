using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class QueryProcessor
    {
        public static List<AdvertiseQueryServer> AdvertiseServers { get; }
            = new List<AdvertiseQueryServer>();

        public static List<GameServer> GameServers { get; } = new List<GameServer>();

        public static bool ProcessPutRequest(string requestString, string body)
        {
            const string pattern = "/(servers)/" +
                                   "([\\.a-zA-Z0-9]+-[0-9]{1,4})/" +
                                   "(info|matches)/?" +
                                   "([0-9]{4}-[0-9]{2}-[0-9]{2})?T?" +
                                   "([0-9]{2}:[0-9]{2}:[0-9]{2})?Z?";
            var splitedString = Regex.Split(requestString, pattern);

            if (splitedString.Length <= 3) return false;

            switch (splitedString[3])
            {
                case "info":
                    return PutInfo(splitedString[2], body);
                case "matches":
                    return PutMatches(splitedString[2], body, splitedString[4],splitedString[5]);
                default:
                    return false;
            }
        }

        private static bool PutInfo(string endpoint, string body)
        {
            var info = JsonConvert.DeserializeObject<Information>(body);
            var advertRequest = new AdvertiseQueryServer(endpoint, info);
            var index = AdvertiseServers.IndexOf(advertRequest);
            if (index >= 0)
                AdvertiseServers[index] = advertRequest;
            else
                AdvertiseServers.Add(advertRequest);
            return true;
        }

        private static bool PutMatches(string endpoint, string body,
            string date,string time)
        {
            if (AdvertiseServers.All(x => x.Endpoint != endpoint)) return false;
            var gameServer = JsonConvert.DeserializeObject<GameServer>(body);
            gameServer.Endpoint = endpoint;
            gameServer.DateAndTime = GetDateTime(date, time);
            GameServers.Add(gameServer);
            return true;
        }

        private static DateTime GetDateTime(string date,string time)
        {
            var splitDate = date.Split('-').Select(int.Parse).ToArray();
            var splitTime = time.Split(':').Select(int.Parse).ToArray();
            return new DateTime(splitDate[0], splitDate[1], splitDate[2],
                splitTime[0],splitTime[1],splitTime[2]);
        }

        public static string ProcessGetRequest(string requestString)
        {
            const string pattern = "/(servers|players|reports)/";
            var splitedRequest = Regex.Split(requestString, pattern);

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

        private static string GetPlayersStatistic(string request)
        {
            var splitRequest = request.Split('/');
            return Json(GameStatistic.GetPlayerStatistic(splitRequest[0].ToLower()));
        }

        private static string GetReport(string request)
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
                    return Json(GameStatistic.GetRecentMatches(n));
                case "best-players":
                    return Json(GameStatistic.GetBestPlayers(n));
                case "popular-servers":
                    return Json(GameStatistic.GetPopularServers(n));
                default:
                    return "Bad Request";
            }
        }

        private static int DefineN(int n)
        {
            if (n <= 0) return 0;
            return n >= 50 ? 50 : n;
        }

        private static string GetServerInformation(string request)
        {
            var pattern = "([\\.a-zA-Z0-9]+-?[0-9]{1,4}|info)/?" +
                          "(info|matches|stats)?/?" +
                          "([0-9]{4}-[0-9]{2}-[0-9]{2})?T?" +
                          "([0-9]{2}:[0-9]{2}:[0-9]{2})?Z?";

            var splitedRequest = Regex.Split(request, pattern);
            if (splitedRequest[1] == "info") return Json(AdvertiseServers.ToArray());
            switch (splitedRequest[2])
            {
                case "info":
                    return GetAdvertServer(splitedRequest[1]);
                case "matches":
                {
                    var dateAndTime = GetDateTime(splitedRequest[3], splitedRequest[4]);
                    return GetAdvertMatch(splitedRequest[1], dateAndTime);
                }
                case "stats":
                    return Json(GameStatistic.GetServerStatistic(splitedRequest[1]));
                default:
                    return "Bad Request";
            }
        }

        private static string GetAdvertServer(string enpoint)
        {
            if (AdvertiseServers.All(x => x.Endpoint != enpoint)) return "Not Found";
            return Json(AdvertiseServers
                .Where(x => x.Endpoint == enpoint)
                .Select(x => x.Info)
                .ToArray()[0]);
        }

        private static string GetAdvertMatch(string endpoint, DateTime date)
        {
            if (GameServers.All(x => x.Endpoint != endpoint)
                || GameServers.All(x => x.DateAndTime != date))
                return "Not Found";
            return Json(GameServers
                .Where(x => x.Endpoint == endpoint)
                .Where(x => x.DateAndTime == date).ToArray()[0]);
        }

        public static string Json(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
