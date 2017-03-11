using System;
using Newtonsoft.Json;
using SQLite;

namespace Kontur.GameStats.Server
{
    [Table("GameServer")]
    public class GameServer
    {
        [JsonIgnore]
        private DateTime dateAndTime;

        public GameServer()
        {
        }

        public GameServer(
            string map,
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
        [Column("date")]
        public DateTime Date { get; private set; }

        [JsonIgnore]
        [Column("dateAndTime")]
        public DateTime DateAndTime
        {
            get { return dateAndTime; }
            set
            {
                dateAndTime = value;
                Date = new DateTime(DateAndTime.Year, DateAndTime.Month, DateAndTime.Day);
                //очень непредсказуемое поведение
                //я такой: хочу засеттить DateAndTime, а он еще и Date какой то сеттит - нехорошо
                //если так сильно надо менять Date, то лучше его и сделать вычисляемым полем
            }
        }

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
        //НАХЕРА!!!??? полный путь до класса, это же херня какая то!!!!
        [System.ComponentModel.DataAnnotations.Schema.Column("timeElapsed")]
        public double TimeElapsed { get; set; }

        [JsonProperty("scoreboard")]
        [Ignore]
        public Player[] Scoreboard { get; set; }
    }
}