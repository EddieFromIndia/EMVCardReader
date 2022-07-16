using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMVCardReader
{
    public static class DataProcessor
    {
        /// <summary>
        /// Converts SFI to P2 to be sent in the READ RECORD APDU.
        /// </summary>
        /// <param name="sfi"></param>
        /// <returns>The byte for P2</returns>
        public static byte SFItoP2(int sfi)
        {
            return (byte)Convert.ToInt32(Convert.ToString(sfi, 2) + "100", 2);
        }

        /// <summary>
        /// Extracts the SFI from the byte. The first 5 bits of a byte represent the SFI.
        /// </summary>
        /// <param name="sfi"></param>
        /// <returns>The SFI as a byte</returns>
        public static byte ExtractSFI(byte sfi)
        {
            return (byte)Convert.ToInt32(Convert.ToString(sfi, 2).PadLeft(8, '0').Substring(0, 5), 2);
        }

        /// <summary>
        /// Searches for the tag in the source.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="tag"></param>
        /// <returns>Returns the data object without the tag and the length from the source</returns>
        public static byte[] GetDataObject(byte[] source, byte[] tag)
        {
            if (SearchTag(source, tag) < 0)
            {
                return null;
            }

            return source.Skip(SearchTag(source, tag) + tag.Length + 1).Take(source[SearchTag(source, tag) + tag.Length]).ToArray();
        }

        /// <summary>
        /// Searches for the index of the tag in the source.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="tag"></param>
        /// <returns>Index of the tag if present, else -1</returns>
        public static int SearchTag(byte[] source, byte[] tag)
        {
            int maxFirstCharSlot = source.Length - tag.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++)
            {
                if (source[i] != tag[0]) // compare only first byte
                {
                    continue;
                }

                if (tag.Length == 1)
                {
                    return i;
                }

                for (int j = 1; j < tag.Length; j++)
                {
                    if (source[i + j] != tag[j])
                    {
                        break;
                    }

                    if (tag.Length == j + 1)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Adds a byte array in the end of another byte array.
        /// </summary>
        /// <param name="bArray"></param>
        /// <param name="newBytes"></param>
        /// <returns>The added byte array</returns>
        public static byte[] AddBytesToArrayEnd(byte[] bArray, byte[] newBytes)
        {
            byte[] newArray = new byte[bArray.Length + newBytes.Length];
            bArray.CopyTo(newArray, 0);
            for (int i = 0; i < newBytes.Length; i++)
            {
                newArray[bArray.Length + i] = newBytes[i];
            }

            return newArray;
        }

        /// <summary>
        /// Splits a byte array to chunks of the given length.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="length"></param>
        /// <returns>List of byte arrays after splitting</returns>
        public static List<byte[]> SplitArray(byte[] source, int length)
        {
            List<byte[]> newArray = new List<byte[]>();

            for (int i = 0; i <= (source.Length / length); i++)
            {
                newArray.Add(source.Skip(length * i).Take(length).ToArray());
            }

            return newArray;
        }

        /// <summary>
        /// Converts a byte array to ASCII string
        /// </summary>
        /// <param name="data"></param>
        /// <returns>ASCII string for the byte array</returns>
        public static string ByteArrayToAsciiString(byte[] data)
        {
            return Encoding.ASCII.GetString(data);
        }

        /// <summary>
        /// Converts ASCII string to Hex string with or without spaces.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="hasSpaces"></param>
        /// <returns>Hex string equivalent of the ASCII string with or without spaces</returns>
        public static string AsciiStringToHexString(string str, bool hasSpaces = false)
        {
            string hexString = BitConverter.ToString(Encoding.Default.GetBytes(str.Trim().Replace(" ", "")));
            return hasSpaces ? hexString.Replace("-", " ") : hexString.Replace("-", "");
        }

        /// <summary>
        /// Converts a byte array to hex string.
        /// </summary>
        /// <param name="ba"></param>
        /// <param name="addCommaAndSpace"></param>
        /// <returns>Hex string equivalent of the byte array</returns>
        public static string ByteArrayToHexString(byte[] ba, bool addCommaAndSpace = false)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat(addCommaAndSpace ? "{0:x2}, " : "{0:x2}", b);
            }

            string formattedString = hex.ToString().ToUpper();

            return addCommaAndSpace ? formattedString.Substring(0, formattedString.Length - 2) : formattedString;
        }

        /// <summary>
        /// Converts an ASCII string to byte array
        /// </summary>
        /// <param name="str"></param>
        /// <returns>The converted byte array</returns>
        public static byte[] AsciiStringToByteArray(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        /// <summary>
        /// Converts a hex string to byte array.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns>The converted byte array</returns>
        public static byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Replace(",", "");
            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        /// <summary>
        /// Returns the integer equivalent of a hex character.
        /// For example, it returns 10 for A, 11 for B, etc.
        /// Helper function for HexStringToByteArray.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns>Integer equivalent of the hex character</returns>
        public static int GetHexVal(char hex)
        {
            int val = hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        /// <summary>
        /// Converts a byte array to an equivalent integer.
        /// For example: 0x00, 0x0B returns 11.
        /// </summary>
        /// <param name="ba"></param>
        /// <returns>Integer equivalent of the byte array as a string</returns>
        public static string ByteArrayToIntString(byte[] ba)
        {
            return Convert.ToInt32(ByteArrayToHexString(ba), 16).ToString();
        }
    }
}
