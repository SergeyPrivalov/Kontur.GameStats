using System;
using Newtonsoft.Json;
using SQLite;

namespace Kontur.GameStats.Server
{
    [Table("GameServer")]
    public class GameServer
    {
        public GameServer()
        {
        }

        public GameServer(string map,
            string gameMode,
            int fraglimit,
            int timeLimit,
            double timeElapsed,
            Player[] scoreboard)
        {
            Map = map;
            GameMode = gameMode;
            FragLimit = fraglimit;
            TimeLimit = timeLimit;
            TimeElapsed = Math.Round(timeElapsed, 6);
            Scoreboard = scoreboard;
        }

        [JsonIgnore]
        [Column("dateAndTime")]
        public DateTime DateAndTime { get; set; }

        [JsonIgnore]
        [Column("endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("map")]
        [Column("map")]
        public string Map { get; set; }

        [JsonProperty("gameMode")]
        [Column("gameMode")]
        public string GameMode { get; set; }

        [JsonProperty("fragLimit")]
        [Column("fragLimit")]
        public int FragLimit { get; set; }

        [JsonProperty("timeLimit")]
        [Column("timeLimit")]
        public int TimeLimit { get; set; }

        [JsonProperty("timeElapsed")]
        [Column("timeElapsed")]
        public double TimeElapsed { get; set; }

        [JsonProperty("scoreboard")]
        [Ignore]
        public Player[] Scoreboard { get; set; }
    }
}