using System;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Реализует сериализацию и десериализацию объектов в формат JSON.
    /// </summary>
    public class JsonSerializer : IJsonSerializer
    {
        /// <summary>
        /// Сериализует объект в строку JSON.
        /// </summary>
        /// <param name="obj">Объект для сериализации.</param>
        /// <returns>Строка JSON, представляющая сериализованный объект.</returns>
        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// Пытается десериализовать строку JSON в объект указанного типа.
        /// </summary>
        /// <typeparam name="TObjectType">Тип объекта для десериализации.</typeparam>
        /// <param name="json">Строка JSON для десериализации.</param>
        /// <param name="deserializedObject">Десериализованный объект, если операция успешна; иначе значение по умолчанию.</param>
        /// <returns>true, если десериализация успешна; иначе false.</returns>
        public bool TryDeserialize<TObjectType>(string json, out TObjectType deserializedObject)
        {
            deserializedObject = default(TObjectType);
            try
            {
                deserializedObject = JsonConvert.DeserializeObject<TObjectType>(json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}