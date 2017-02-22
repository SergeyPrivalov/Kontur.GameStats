using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.GameStats.Server
{
    public interface IJsonSerializer
    {
        string Serialize(object obj);
        bool TryDeserialize<TObjectType>(string json, out TObjectType deserializedObject);
    }
}
