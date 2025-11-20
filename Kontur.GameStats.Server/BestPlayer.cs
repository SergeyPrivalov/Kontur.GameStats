using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Представляет игрока с наилучшим соотношением убийств к смертям.
    /// </summary>
    public class BestPlayer
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("killToDeathRatio")]
        public double KillToDeathRatio { get; set; }
    }
}