using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class PlayerStats
    {
        [JsonProperty("totalMatchesPlayed")]
        public int TotalMatchesPlayed { get; set; }

        [JsonProperty("totalMatchesWon")]
        public int TotalMatchesWon { get; set; }

        [JsonProperty("favoriteServer")]
        public string FavoriteServer { get; set; }

        [JsonProperty("uniqueServers")]
        public int UniqueServers { get; set; }

        [JsonProperty("favoriteGameMode")]
        public string FavoriteGameMode { get; set; }

        [JsonProperty("averageScoreboardPercent")]
        public double AverageScoreboardPercent { get; set; }

        [JsonProperty("maximumMatchesPerDay")]
        public int MaximumMatchesPerDay { get; set; }

        [JsonProperty("averageMatchesPerDay")]
        public double AverageMatchesPerDay { get; set; }

        [JsonProperty("lastMatchPlayed")]
        public string LastMatchPlayed { get; set; }

        [JsonProperty("killToDeathRatio")]
        public double KillToDeathRatio { get; set; }
    }
}