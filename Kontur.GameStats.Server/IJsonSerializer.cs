namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Определяет интерфейс для сериализации и десериализации объектов в формат JSON.
    /// </summary>
    public interface IJsonSerializer
    {
        string Serialize(object obj);
        bool TryDeserialize<TObjectType>(string json, out TObjectType deserializedObject);
    }
}