namespace Kontur.GameStats.Server
{
    public interface IJsonSerializer
    {
        string Serialize(object obj);
        bool TryDeserialize<TObjectType>(string json, out TObjectType deserializedObject);
    }
}