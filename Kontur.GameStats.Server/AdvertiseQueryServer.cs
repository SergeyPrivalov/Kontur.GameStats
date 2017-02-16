using System.Linq;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class AdvertiseQueryServer
    {
        public AdvertiseQueryServer(string endpoint, Information info)
        {
            Endpoint = endpoint;
            Info = info;
        }

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("info")]
        public Information Info { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is AdvertiseQueryServer)) return false;
            var advertQuery = (AdvertiseQueryServer) obj;
            return Endpoint == advertQuery.Endpoint;
        }

        public override int GetHashCode()
        {
            return Endpoint.Select(x => x * 47).Sum();
        }
    }

    public class Information
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("gameModes")]
        public string[] GameModes { get; set; }
    }
}