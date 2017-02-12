using System;
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

        public PlayerStats(int TMP, int TMW, string FS, int US, string FGM,
            double ASP, int MMPD, double AMPD, string LMP, double KTDR)
        {
            TotalMatchesPlayed = TMP;
            TotalMatchesWon = TMW;
            FavoriteServer = FS;
            UniqueServers = US;
            FavoriteGameMode = FGM;
            AverageScoreboardPercent = Math.Round(ASP, 6);
            MaximumMatchesPerDay = MMPD;
            AverageMatchesPerDay = Math.Round(AMPD, 6);
            LastMatchPlayed = LMP;
            KillToDeathRatio = Math.Round(KTDR, 6);
        }
    }
}
