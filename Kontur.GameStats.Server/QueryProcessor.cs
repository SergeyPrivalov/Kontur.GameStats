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

        private static readonly Dictionary<string, GameServer> GameServers 
            = new Dictionary<string, GameServer>();

        public static bool ProcessPutRequest(string requestString, string body)
        {
            const string pattern = "/(servers)/([\\.a-zA-Z0-9]+-[0-9]{1,4})/(info|matches)";
            var splitedString = Regex.Split(requestString, pattern);

            if (splitedString.Length < 3) return false;

            switch (splitedString[3])
            {
                case "info":
                    return PutInfo(splitedString[2], body);
                case "matches":
                    return PutMatches(splitedString[2], body);
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

        private static bool PutMatches(string endpoint, string body)
        {
            if (AdvertiseServers.All(x => x.Endpoint != endpoint)) return false;
            var gameServer = JsonConvert.DeserializeObject<GameServer>(body);
            GameServers.Add(endpoint, gameServer);
            return true;
        }

        public static string ProcessGetRequest(string requestString)
        {
            const string pattern = "/(servers|players|reports)/" +
                                   "([\\.a-zA-Z0-9]+-[0-9]{1,4}|info)/?" +
                                   "(info|matches|stats)?";
            var splitedRequest = Regex.Split(requestString, pattern);

            if (splitedRequest.Length < 2) return "Bad Request";
      
            if (splitedRequest[1] == "info") return Json(AdvertiseServers.ToArray());

            switch (splitedRequest[0])
            {
                case "servers":
                    return GetServerInformation(splitedRequest[1],splitedRequest[2]);
                case "players":
                case "reports":
                default:
                    return "Bad Request";
            }
        }

        private static string GetServerInformation(string endpoint, string kindOfInfo)
        {
            switch (kindOfInfo)
            {
                case "info":
                    return GetAdvertServer(endpoint);
                case "matches":
                    return GetAdvertMathc(endpoint);
                default:
                    return "bbbb";
            }
        }

        private static string GetAdvertServer(string enpoint)
        {
            if (AdvertiseServers.All(x => x.Endpoint != enpoint)) return "Not Found";
            return Json(AdvertiseServers
                .Where(x => x.Endpoint == enpoint)
                .Select(x => x.Info));
        }

        private static string GetAdvertMathc(string endpoint)
        {
            return GameServers.ContainsKey(endpoint) ? Json(GameServers[endpoint]) : "Not Found";
        }

        private static string Json(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
