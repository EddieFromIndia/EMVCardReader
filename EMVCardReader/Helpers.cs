using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMVCardReader
{
    public static class Helpers
    {
        public static byte SFItoP2(int sfi)
        {
            return (byte)Convert.ToInt32(Convert.ToString(sfi, 2) + "100", 2);
        }

        public static byte ExtractSFI(byte sfi)
        {
            return (byte)Convert.ToInt32(Convert.ToString(sfi, 2).Substring(0, 5), 2);
        }

        public static byte[] GetDataObject(byte[] source, byte[] tag)
        {
            if (SearchTag(source, tag) < 0)
            {
                return null;
            }

            return source.Skip(SearchTag(source, tag) + tag.Length + 1).Take(source[SearchTag(source, tag) + tag.Length]).ToArray();
        }

        public static int SearchTag(byte[] source, byte[] tag)
        {
            int maxFirstCharSlot = source.Length - tag.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++)
            {
                if (source[i] != tag[0]) // compare only first byte
                {
                    continue;
                }

                // found a match on first byte, now try to match rest of the pattern
                for (int j = tag.Length - 1; j >= 1; j--)
                {
                    if (source[i + j] != tag[j])
                    {
                        break;
                    }

                    if (j == 1)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static byte[] AddBytesToArray(byte[] bArray, byte[] newBytes)
        {
            byte[] newArray = new byte[bArray.Length + newBytes.Length];
            bArray.CopyTo(newArray, newBytes.Length);
            for (int i = 0; i < newBytes.Length; i++)
            {
                newArray[i] = newBytes[i];
            }

            return newArray;
        }

        public static List<byte[]> SplitArray(byte[] source, int length)
        {
            List<byte[]> newArray = new List<byte[]>();

            for (int i = 0; i <= (source.Length / length); i++)
            {
                newArray.Add(source.Skip(length * i).Take(length).ToArray());
            }

            return newArray;
        }

        public static string HexToString(byte[] data)
        {
            return Encoding.ASCII.GetString(data);
        }

        public static string StringToHex(string str, bool hasSpaces)
        {
            string hexString = BitConverter.ToString(Encoding.Default.GetBytes(str));
            return hasSpaces ? hexString.Replace("-", " ") : hexString.Replace("-", "");
        }

        public static string ByteToHexString(byte ba)
        {
            StringBuilder hex = new StringBuilder(2);
            return hex.AppendFormat("{0:x2}", ba).ToString().ToUpper();
        }

        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString().ToUpper();
        }

        public static string ByteToBinaryString(byte b)
        {
            return Convert.ToString(b, 2).PadLeft(8, '0');
        }

        public static byte ByteToSFI(byte b)
        {
            return Convert.ToByte(Convert.ToString(b, 2).PadLeft(8, '0').Substring(0, 5), 2);
        }
    }
}
