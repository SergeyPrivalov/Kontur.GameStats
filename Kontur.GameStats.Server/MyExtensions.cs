using System.Text;

namespace ExtensionsMethods
{
    public static class MyExtensions
    {
        public static byte[] GetBytesInUnicode(this string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public static string GetUnicodeString(this byte[] arrayBytes)
        {
            return Encoding.UTF8.GetString(arrayBytes);
        }
    }
}
