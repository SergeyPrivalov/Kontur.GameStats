using System.Text;

namespace ExtensionsMethods
{
    /// <summary>
    /// Предоставляет методы расширения для работы со строками.
    /// </summary>
    public static class MyExtensions
    {
        /// <summary>
        /// Преобразует строку в массив байтов в кодировке ASCII.
        /// </summary>
        /// <param name="str">Строка для преобразования.</param>
        /// <returns>Массив байтов в кодировке ASCII.</returns>
        public static byte[] GetBytesInAscii(this string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }
    }
}