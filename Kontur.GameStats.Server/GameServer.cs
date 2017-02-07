using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class GameServer
    {
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
