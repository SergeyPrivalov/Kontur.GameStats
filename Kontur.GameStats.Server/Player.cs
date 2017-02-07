using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
    }
}
