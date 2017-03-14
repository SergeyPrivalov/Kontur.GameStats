using System.Text;

namespace ExtensionsMethods
{
    public static class MyExtensions
    {
        public static byte[] GetBytesInAscii(this string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }
    }
}