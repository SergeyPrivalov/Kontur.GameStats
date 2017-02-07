using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kontur.GameStats.Server
{
    public class AdvertiseQueryServer
    {
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }
        [JsonProperty("info")]
        public Information Info { get; set; }

        public AdvertiseQueryServer(string endpoint, Information info)
        {
            Endpoint = endpoint;
            Info = info;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AdvertiseQueryServer)) return false;
            var advertQuery = (AdvertiseQueryServer) obj;
            return Endpoint == advertQuery.Endpoint;
        }
    }

    public class Information
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("gameModes")]
        public string[] GameModes { get; set; }
    }
}
