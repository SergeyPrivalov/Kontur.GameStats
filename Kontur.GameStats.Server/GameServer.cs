using System;
using Newtonsoft.Json;
using SQLite;

namespace Kontur.GameStats.Server
{
    [System.ComponentModel.DataAnnotations.Schema.Table("GameServer")]
    public class GameServer
    {
        [JsonIgnore]
        private DateTime dateAndTime;

        public GameServer()
        {
        }

        public GameServer(string map, string gameMode, int fraglimit,
            int timeLimit, double timeElapsed, Player[] scoreboard)
        {
            Map = map;
            GameMode = gameMode;
            FragLimit = fraglimit;
            TimeLimit = timeLimit;
            TimeElapsed = Math.Round(timeElapsed, 6);
            Scoreboard = scoreboard;
        }

        [JsonIgnore]
        [System.ComponentModel.DataAnnotations.Schema.Column("date")]
        public DateTime Date { get; private set; }

        [JsonIgnore]
        [System.ComponentModel.DataAnnotations.Schema.Column("dateAndTime")]
        public DateTime DateAndTime
        {
            get { return dateAndTime; }
            set
            {
                dateAndTime = value;
                Date = new DateTime(DateAndTime.Year,
                    DateAndTime.Month, DateAndTime.Day);
            }
        }

        [JsonIgnore]
        [System.ComponentModel.DataAnnotations.Schema.Column("endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("map")]
        [System.ComponentModel.DataAnnotations.Schema.Column("map")]
        public string Map { get; set; }

        [JsonProperty("gameMode")]
        [System.ComponentModel.DataAnnotations.Schema.Column("gameMode")]
        public string GameMode { get; set; }

        [JsonProperty("fragLimit")]
        [System.ComponentModel.DataAnnotations.Schema.Column("fragLimit")]
        public int FragLimit { get; set; }

        [JsonProperty("timeLimit")]
        [System.ComponentModel.DataAnnotations.Schema.Column("timeLimit")]
        public int TimeLimit { get; set; }

        [JsonProperty("timeElapsed")]
        [System.ComponentModel.DataAnnotations.Schema.Column("timeElapsed")]
        public double TimeElapsed { get; set; }

        [JsonProperty("scoreboard")]
        [Ignore]
        public Player[] Scoreboard { get; set; }
    }
}