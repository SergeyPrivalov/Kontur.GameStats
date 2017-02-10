using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class ServerStats
    {
        [JsonProperty("totalMatchesPlayed")]
        public int TotalMatchesPlaed { get; set; }
        [JsonProperty("maximumMatchesPerDay")]
        public int MaximumMatchesPerDay { get; set; }
        [JsonProperty("averageMatchesPerDay")]
        public double AverageMatchesPerDay { get; set; }
        [JsonProperty("maximumPopulation")]
        public int MaximumPopulation { get; set; }
        [JsonProperty("averagePopulation")]
        public double AveragePopulation { get; set; }
        [JsonProperty("top5GameModes")]
        public string[] Top5GameModes { get; set; }
        [JsonProperty("top5Maps")]
        public string[] Top5Maps { get; set; }

        public ServerStats(int totalMatch, int maxMatch, double avergeMatch,
            int maxPopulation, double averagePopulation, string[] gameModes,
            string[] maps)
        {
            TotalMatchesPlaed = totalMatch;
            MaximumMatchesPerDay = maxMatch;
            AverageMatchesPerDay = avergeMatch;
            MaximumPopulation = maxPopulation;
            AveragePopulation = averagePopulation;
            Top5GameModes = gameModes;
            Top5Maps = maps;
        }
    }
}
