using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    public class JsonSerializer : IJsonSerializer
    {
        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public bool TryDeserialize<TObjectType>(string json, out TObjectType deserializedObject)
        {
            deserializedObject = default(TObjectType);
            try
            {
                deserializedObject = JsonConvert.DeserializeObject<TObjectType>(json);
                return true;
            }
            catch (Exception )
            {
                return false;
            }
        }
    }
}
