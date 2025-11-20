using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Представляет информацию о недавно сыгранном матче.
    /// </summary>
    public class RecentMatch
    {
        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("results")]
        public GameServer Result { get; set; }
    }
}