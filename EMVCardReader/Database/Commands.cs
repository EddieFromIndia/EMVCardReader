namespace EMVCardReader.Database
{
    public static class Commands
    {
        //see belgian_electronic_identity_card_content_v2.8.a page 10 + 13
        //4.1 File structure
        internal static readonly byte[] IDENTITY_FILE_LOCATION = new byte[] {
                            0x3F,// MASTER FILE, Head directory MF "3f00"  
                            0x00,
                            0xDF,// Dedicated File, subdirectory identity DF(ID) "DF01"  
                            0x01,
                            0x40,// Elementary File, the identity file itself EF(ID#RN) "4031"  
                            0x31
        };

        //see belgian_electronic_identity_card_content_v2.8.a page 10 + 13
        //4.1 File structure
        internal static readonly byte[] ADDRESS_FILE_LOCATION = new byte[] {
                            0x3F,// MASTER FILE, Head directory MF "3f00"  
                            0x00,
                            0xDF,// Dedicated File, subdirectory identity DF(ID) "DF01"  
                            0x01,
                            0x40,// Elementary File, the address file EF(ID#Address) "4033"  
                            0x33
        };

        //see Reference Manual BelPic (V1.7) page 23
        //6.4 READ BINARY Command (ISO 7816-4)
        internal static readonly byte[] READ_FILE_COMMAND = new byte[] {
                            0x00, //CLA   
                            0xB0, //Read binary command  
                            0x00, //OFF_H higher byte of the offset (bit 8 = 0)  
                            0x00,
                            0 //le
        };
    }
}
