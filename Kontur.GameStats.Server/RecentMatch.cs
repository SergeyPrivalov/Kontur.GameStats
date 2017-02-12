using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class RecentMatch
    {
        [JsonProperty("server")]
        public string Server { get; set; }
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
        [JsonProperty("result")]
        public GameServer Result { get; set; }

        public RecentMatch(string endpoint,string date,GameServer gameServer)
        {
            Server = endpoint;
            Timestamp = date;
            Result = gameServer;
        }
    }
}
