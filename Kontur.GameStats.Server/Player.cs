﻿using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class Player
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("frags")]
        public int Frags { get; set; }
        [JsonProperty("kills")]
        public int Kills { get; set; }
        [JsonProperty("deaths")]
        public int Deaths { get; set; }

        public Player(string name, int frags, int kills, int deaths)
        {
            Name = name;
            Frags = frags;
            Kills = kills;
            Deaths = deaths;
        }
    }
}
