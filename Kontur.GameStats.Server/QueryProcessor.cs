using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class QueryProcessor
    {
        private static Dictionary<string, AdvertiseQueryServer> _advertiseServers
            = new Dictionary<string, AdvertiseQueryServer>();
        //private static List<AdvertiseQueryServer> _advertiseServers 
        //    = new List<AdvertiseQueryServer>();
        private static Dictionary<string, GameServer> _gameServers 
            = new Dictionary<string, GameServer>();

        public static bool ProcessPutRequest(string requestString, string body)
        {
            var splitRequest = requestString.Split('/');
            const string pattern = "/(servers)/([.a-zA-Z0-9]+-[0-9]{1,4})/(info|matches)*";
            var splitedString = Regex.Split(requestString, pattern);

            if (splitedString.Length < 3) return false;

            switch (splitedString[3])
            {
                case "info":
                    return PutInfo(splitRequest[2], body);
                case "matches":
                    return PutMatches(splitRequest[2], body);
                default:
                    return false;
            }
        }

        private static bool PutInfo(string endpoint, string body)
        {
            var advertiseRequest = JsonConvert.DeserializeObject<Information>(body);
            if (_advertiseServers.ContainsKey(endpoint))
                _advertiseServers[endpoint] = 
                    new AdvertiseQueryServer(endpoint, advertiseRequest);
            else
                _advertiseServers
                    .Add(endpoint, new AdvertiseQueryServer(endpoint, advertiseRequest));
            return true;
        }

        private static bool PutMatches(string endpoint, string body)
        {
            if (!_advertiseServers.ContainsKey(endpoint)) return false;
            var gameServer = JsonConvert.DeserializeObject<GameServer>(body);
            _gameServers.Add(endpoint, gameServer);
            return true;
        }

        public static string ProcessGetRequest(string requestString)
        {
            return "";
        }

    }
}
