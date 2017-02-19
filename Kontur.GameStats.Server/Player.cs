using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    [Table("players")]
    public class Player
    {
        public Player()
        {
        }

        public Player(string name, int frags, int kills, int deaths)
        {
            Name = name.ToLower();
            Frags = frags;
            Kills = kills;
            Deaths = deaths;
        }

        [JsonIgnore]
        [Column("ednpoint")]
        public string Endpoint { get; set; }

        [JsonIgnore]
        [Column("date")]
        public DateTime Date { get; set; }

        [JsonIgnore]
        [Column("place")]
        public int Place { get; set; }

        [JsonProperty("name")]
        [Column("name")]
        public string Name { get; set; }

        [JsonProperty("frags")]
        [Column("frags")]
        public int Frags { get; set; }

        [JsonProperty("kills")]
        [Column("kills")]
        public int Kills { get; set; }

        [JsonProperty("deaths")]
        [Column("deaths")]
        public int Deaths { get; set; }
    }
}