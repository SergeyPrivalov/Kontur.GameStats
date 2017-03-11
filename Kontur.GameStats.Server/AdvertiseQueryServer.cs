using Newtonsoft.Json;
using SQLite;

namespace Kontur.GameStats.Server
{
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
            return Endpoint.GetHashCode() * 47;//грубая ошибка: Endpoint можно извне присвоить другое значение, тогда хэш код у объекта изменится
        }
    }

    public class Information
    {
        public Information()
        {
            GameModes = new string[0];
        }

        [JsonIgnore]
        public string Endpoint { get; set; }//уверен что это поле вообще хоть где то необходимо?

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("gameModes")]
        public string[] GameModes { get; set; }
    }
}