using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class BestPlayer
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("killToDeathRatio")]
        public double KillToDeathRatio { get; set; }
    }
}