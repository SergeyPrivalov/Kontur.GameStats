using System;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class GameServer
    {
        [JsonIgnore] private DateTime dateAndTime;

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
        public DateTime Date { get; private set; }

        [JsonIgnore]
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
        public string Endpoint { get; set; }

        [JsonProperty("map")]
        public string Map { get; set; }

        [JsonProperty("gameMode")]
        public string GameMode { get; set; }

        [JsonProperty("fragLimit")]
        public int FragLimit { get; set; }

        [JsonProperty("timeLimit")]
        public int TimeLimit { get; set; }

        [JsonProperty("timeElapsed")]
        public double TimeElapsed { get; set; }

        [JsonProperty("scoreboard")]
        public Player[] Scoreboard { get; set; }
    }
}