using System;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class BestPlayer
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("killToDeathRatio")]
        public double KillToDeathRatio { get; set; }

        public BestPlayer(string name, double killToDeathRatio)
        {
            Name = name;
            KillToDeathRatio = Math.Round(killToDeathRatio,6);
        }
    }
}
