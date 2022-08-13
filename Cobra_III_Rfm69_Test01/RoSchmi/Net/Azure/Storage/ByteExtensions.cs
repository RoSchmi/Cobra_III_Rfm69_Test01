using System;
using Microsoft.SPOT;

namespace RoSchmi.Net.Azure.Storage
{
    public static class ByteExtensions
    {
        private static char[] _hexCharacterTable = new char[]
                { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string ToHexString(this byte[] array, string delimiter = "-")
        {
            if (array.Length > 0)
            {
                // it's faster to concatenate inside a char array than to
                // use string concatenation
                char[] delimeterArray = delimiter.ToCharArray();
                char[] chars = new char[array.Length * 2 + delimeterArray.Length * (array.Length - 1)];
 
                int j = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    chars[j++] = (char)_hexCharacterTable[(array[i] & 0xF0) >> 4];
                    chars[j++] = (char)_hexCharacterTable[array[i] & 0x0F];
 
                    if (i != array.Length - 1)
                    {
                        foreach (char c in delimeterArray)
                        {
                            chars[j++] = c;
                        }
 
                    }
                }
 
                return new string(chars);
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
