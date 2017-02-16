using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class PopularServer
    {
        [JsonProperty("endpoint")]
        public string Enpoint { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("averageMatchPerDay")]
        public double AverageMatchPerDay { get; set; }
    }
}