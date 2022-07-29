using System.Collections.Generic;

namespace EMVCardReader.Database
{
    public static class EidTags
    {
        public const byte ADDRESS_STREET_NUMBER = 0x01;
        public const byte ADDRESS_ZIP_CODE = 0x02;
        public const byte ADDRESS_MUNICIPALITY = 0x03;

        public const byte ID_NATIONAL_NUMBER = 0x06;
        public const byte ID_LAST_NAME = 0x07;
        public const byte ID_FIRST_NAME = 0x08;
        public const byte ID_NATIONALITY = 0x0A;
        public const byte ID_BIRTH_LOCATION = 0x0B;
        public const byte ID_BIRTH_DATE = 0x0C;
        public const byte ID_SEX = 0x0D;

        public static IReadOnlyCollection<byte> ADDRESS_TAGS = new List<byte>()
        {
            ADDRESS_STREET_NUMBER,
            ADDRESS_ZIP_CODE,
            ADDRESS_MUNICIPALITY
        }.AsReadOnly();

        public static IReadOnlyCollection<byte> IDENTITY_TAGS = new List<byte>()
        {
            ID_NATIONAL_NUMBER,
            ID_LAST_NAME,
            ID_FIRST_NAME,
            ID_NATIONALITY,
            ID_BIRTH_LOCATION,
            ID_BIRTH_DATE,
            ID_SEX
        }.AsReadOnly();
    }
}
