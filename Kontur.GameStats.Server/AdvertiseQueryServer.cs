using Newtonsoft.Json;
using SQLite;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Представляет информацию об игровом сервере, зарегистрированном в системе статистики.
    /// </summary>
    [Table("AdvertiseQueryServer")]
    public class AdvertiseQueryServer
    {
        public AdvertiseQueryServer()
        {
            Info = new Information();
        }

        public AdvertiseQueryServer(string endpoint, Information info)
        {
            Endpoint = endpoint;
            Name = info.Name;
            Info = info;
        }

        [JsonProperty("endpoint")]
        [Column("endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("info")]
        [Ignore]
        public Information Info { get; set; }

        [JsonIgnore]
        [Column("name")]
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is AdvertiseQueryServer)) return false;
            var advertQuery = (AdvertiseQueryServer) obj;
            return Endpoint == advertQuery.Endpoint;
        }

        public override int GetHashCode()
        {
            return Endpoint.GetHashCode() * 47;
        }
    }

    /// <summary>
    /// Представляет основную информацию о сервере: название и поддерживаемые игровые режимы.
    /// </summary>
    public class Information
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("gameModes")]
        public string[] GameModes { get; set; }
    }
}