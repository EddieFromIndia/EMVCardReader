using System.Collections.Generic;
using EMVCardReader.Models;

namespace EMVCardReader
{
    public static class CardData
    {
        public static byte[] ColdATR;
        public static List<byte[]> AvailableAIDs = new List<byte[]>();
        public static List<ADFModel> AvailableADFs = new List<ADFModel>();
        public static byte[] FCIofDDF;

        public static byte[] FCIofDDFContactless;
        public static byte[] CPLC;
    }
}
