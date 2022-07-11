using EMVCardReader.Database;
using EMVCardReader.Models;
using Great.EmvTags;
using Newtonsoft.Json;
using PCSC;
using PCSC.Iso7816;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;

namespace EMVCardReader
{
    public class Program
    {
        /// <summary>
        /// Default PDOL to be used when no PDOL is received from the card.
        /// </summary>
        private static readonly byte[] defaultPDOL = new byte[] { 0x83, 0x00 };

        /// <summary>
        /// The selected card reader name.
        /// </summary>
        public static string SelectedReader = string.Empty;

        /// <summary>
        /// The instance of JSON data object.
        /// </summary>
        public static JsonData jsonData = new JsonData();



        /// <summary>
        /// The main method.
        /// </summary>
        /// <returns>Exit status of the application</returns>
        private static int Main()
        {
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("                EMV Card Reader");
            try
            {
                using (ISCardContext context = ContextFactory.Instance.Establish(SCardScope.System))
                {
                    while (true)
                    {
                        string choice = DisplaySelectionMenu();

                        switch (choice)
                        {
                            // Select a card reader
                            case "0":
                                string[] readerNames = context.GetReaders();
                                if (readerNames == null || readerNames.Length < 1)
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("ERROR: No card readers found.");
                                    Console.WriteLine();
                                }
                                else
                                {
                                    SelectedReader = ChooseReader(readerNames);

                                    Console.WriteLine();
                                    Console.WriteLine("The card reader has been selected.");
                                    Console.WriteLine();
                                }
                                break;

                            // Check if card reader still connected
                            case "1":
                                // If a reader is not selected
                                if (string.IsNullOrEmpty(SelectedReader))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("ERROR: No card readers selected. Select a reader first.");
                                    Console.WriteLine();
                                }
                                else
                                {
                                    using (IsoReader isoReader = new IsoReader(context, SelectedReader, SCardShareMode.Shared, SCardProtocol.Any, false))
                                    {
                                        Console.WriteLine();
                                        Console.Write("Checking if the card reader is connected. Please wait...");

                                        // Trying to read the Cold ATR from the card
                                        if (GetColdAtr(context).Length > 0)
                                        {
                                            Console.WriteLine();
                                            Console.WriteLine("true");
                                            Console.WriteLine();
                                        }
                                        else
                                        {
                                            Console.WriteLine();
                                            Console.WriteLine("false");
                                            Console.WriteLine();
                                        }
                                    }
                                }
                                break;

                            // Check card type
                            case "2":
                                // If a reader is not selected
                                if (string.IsNullOrEmpty(SelectedReader))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("ERROR: No card readers selected. Select a reader first.");
                                    Console.WriteLine();
                                }
                                else
                                {
                                    Console.WriteLine();
                                    Console.Write("Determining card type. Please wait...");

                                    string cardType = GetCardType(context);
                                    if (cardType == null)
                                    {
                                        Console.WriteLine();
                                        Console.WriteLine("ERROR: Card type cannot be determined.");
                                        Console.WriteLine();
                                    }
                                    else
                                    {
                                        Console.WriteLine();
                                        Console.WriteLine(cardType);
                                        Console.WriteLine();
                                    }
                                }
                                break;

                            // Get card details and display
                            case "3":
                                // If a reader is not selected
                                if (string.IsNullOrEmpty(SelectedReader))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("ERROR: No card readers selected. Select a reader first.");
                                    Console.WriteLine();
                                }
                                else
                                {
                                    Console.WriteLine();
                                    Console.Write("Reading card data. Please wait...");

                                    GenerateCardDetails(context);

                                    string jsonData = SerializeDataToJson();

                                    DisplayData(jsonData);
                                }
                                break;

                            // Exit application
                            case "4":
                                return 0;

                            default:
                                Console.WriteLine("ERROR: An invalid number has been entered.");
                                Console.WriteLine();
                                break;
                        }
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
                return 1;
            }
        }




        /// <summary>
        /// Displays the main selection menu.
        /// </summary>
        /// <returns>The action chosen by the user</returns>
        private static string DisplaySelectionMenu()
        {
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("[0] Select a card reader from a list of readers.");
            Console.WriteLine("[1] Check if the card reader is still connected.");
            Console.WriteLine("[2] Check card type.");
            Console.WriteLine("[3] Get card details.");
            Console.WriteLine("[4] Exit application.");
            Console.WriteLine();
            Console.Write("Choose an action: ");
            return Console.ReadLine();
        }

        /// <summary>
        /// Displays the list of card readers available
        /// and returns the name of the reader selected.
        /// </summary>
        /// <param name="readerNames">List of readers available</param>
        /// <returns>Name of the card reader selected</returns>
        private static string ChooseReader(IList<string> readerNames)
        {
            // Show available readers.
            Console.WriteLine("Available card readers: ");
            for (int i = 0; i < readerNames.Count; i++)
            {
                Console.WriteLine("[" + i + "] " + readerNames[i]);
            }

            // Ask the user which one to choose.
            Console.WriteLine();
            Console.Write("Choose a card reader: ");
            string line = Console.ReadLine();

            if (int.TryParse(line, out int choice) && choice >= 0 && choice < readerNames.Count)
            {
                return readerNames[choice];
            }

            Console.WriteLine("An invalid number has been entered.");
            return null;
        }

        /// <summary>
        /// Gets the card type.
        /// </summary>
        /// <param name="context">The card context</param>
        /// <returns>Card type as string</returns>
        private static string GetCardType(ISCardContext context)
        {
            using (IsoReader isoReader = new IsoReader(context, SelectedReader, SCardShareMode.Shared, SCardProtocol.Any, false))
            {
                List<byte[]> aidList = GetAidsFromPSE(isoReader);

                if (aidList.Count == 0)
                {
                    // Generate AID list manually
                    aidList = GenerateCandidateList(isoReader);
                }

                WarmReset(isoReader);

                // Process Application for each AIDs
                foreach (byte[] AID in aidList)
                {
                    string label = GetApplicationLabel(isoReader, AID);

                    if (label != null)
                    {
                        return label;
                    }

                    WarmReset(isoReader);
                }

                isoReader.Disconnect(SCardReaderDisposition.Reset);
            }

            return null;
        }

        /// <summary>
        /// Generate AID list for PSE.
        /// </summary>
        /// <param name="isoReader"></param>
        /// <returns>List of AIDs or empty list</returns>
        private static List<byte[]> GetAidsFromPSE(IsoReader isoReader)
        {
            List<byte[]> AvailableAIDs = new List<byte[]>();

            // Select PSE
            Response response = SelectFileCommand(isoReader, DataProcessor.AsciiStringToByteArray("1PAY.SYS.DDF01"));

            // Might be a french card
            if (response.SW1 == 0x6E && response.SW2 == 0x00)
            {
                WarmReset(isoReader);
                response = SelectFileCommand(isoReader, DataProcessor.AsciiStringToByteArray("1PAY.SYS.DDF01"));
            }

            // Might be a contactless card
            if (response.SW1 == 0x6A && response.SW2 == 0x82)
            {
                return GetAidsFromPPSE(isoReader);
            }

            if (response.SW1 == 0x61)
            {
                response = GetResponseCommand(isoReader, response.SW2);
            }

            if (!(response.SW1 == 0x90 && response.SW2 == 0x00))
            {
                return new List<byte[]>();
            }

            byte[] FCIofDDF = response.GetData();

            // Extract SFI
            EmvTlvList FCIofDDFTags = EmvTlvList.Parse(FCIofDDF);
            if (FCIofDDFTags[0].Children[1].Children[0].Tag.Hex == "88")
            {
                int sfi = FCIofDDFTags[0].Children[1].Children[0].Value.Bytes[0];
                int record = 1;
                int triesLeft = 5;

                do
                {
                    response = ReadRecordCommand(isoReader, record++, sfi);

                    if (response.SW1 == 0x6C && response.SW2 != 0x00)
                    {
                        response = ReadRecordCommand(isoReader, record, sfi, response.SW2);
                    }

                    // Generate list of AIDs
                    if (response.SW1 == 0x90 && response.SW2 == 0x00)
                    {
                        byte[] FCI = response.GetData();
                        AvailableAIDs.Add(DataProcessor.GetDataObject(FCI, new byte[] { 0x4F }));
                        continue;
                    }

                    // Some cards have records starting from 4 or 5,
                    // so, don't quit until 5 records are checked.
                    if (record > 5)
                    {
                        triesLeft--;
                    }
                    record++;
                } while (triesLeft > 0);
            }

            return AvailableAIDs;
        }

        /// <summary>
        /// Generate AID list for PPSE.
        /// </summary>
        /// <param name="isoReader"></param>
        /// <returns>List of AIDs or empty list</returns>
        private static List<byte[]> GetAidsFromPPSE(IsoReader isoReader)
        {
            List<byte[]> AvailableAIDs = new List<byte[]>();
            // Select PPSE
            Response response = SelectFileCommand(isoReader, DataProcessor.AsciiStringToByteArray("2PAY.SYS.DDF01"));

            if (response.SW1 == 0x61)
            {
                response = GetResponseCommand(isoReader, response.SW2);
            }

            if (!(response.SW1 == 0x90 && response.SW2 == 0x00))
            {
                return new List<byte[]>();
            }

            byte[] FCIofDDF = response.GetData();

            // Extract SFI
            EmvTlvList FCIofDDFTags = EmvTlvList.Parse(FCIofDDF);
            if (FCIofDDFTags[0].Children[1].Children[0].Tag.Hex == "88")
            {
                int sfi = FCIofDDFTags[0].Children[1].Children[0].Value.Bytes[0];
                int record = 1;
                int triesLeft = 5;

                do
                {
                    response = ReadRecordCommand(isoReader, record++, sfi);

                    if (response.SW1 == 0x6C && response.SW2 != 0x00)
                    {
                        response = ReadRecordCommand(isoReader, record, sfi, response.SW2);
                    }

                    // Generate list of AIDs
                    if (response.SW1 == 0x90 && response.SW2 == 0x00)
                    {
                        byte[] FCI = response.GetData();
                        AvailableAIDs.Add(DataProcessor.GetDataObject(FCI, new byte[] { 0x4F }));
                        continue;
                    }

                    // Some cards have records starting from 4 or 5,
                    // so, don't quit until 5 records are checked.
                    if (record > 5)
                    {
                        triesLeft--;
                    }
                    record++;
                } while (triesLeft > 0);
            }

            return AvailableAIDs;
        }

        /// <summary>
        /// Processes an application and generates the application-related tlv data.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        /// <param name="AID">The Application ID to be processed</param>
        private static string GetApplicationLabel(IsoReader isoReader, byte[] AID)
        {
            // Select Application
            Response response = SelectFileCommand(isoReader, AID);

            if (response.SW1 == 0x61)
            {
                response = GetResponseCommand(isoReader, response.SW2);
            }

            if (!(response.SW1 == 0x90 && response.SW2 == 0x00))
            {
                return null;
            }

            byte[] ADF = response.GetData();

            EmvTlv tag = (EmvTlvList.Parse(ADF)).FindFirst("50");

            if (tag == null)
            {
                return null;
            }

            return tag.Value.Ascii;
        }

        /// <summary>
        /// Reads the card details from the card and saves them in CardData.
        /// </summary>
        /// <param name="context">The card context</param>
        private static void GenerateCardDetails(ISCardContext context)
        {
            using (IsoReader isoReader = new IsoReader(context, SelectedReader, SCardShareMode.Shared, SCardProtocol.Any, false))
            {
                CardData.ColdATR = GetColdAtr(context);

                // Generate AID list from PSE
                ProcessPSE(isoReader, "1PAY.SYS.DDF01");
                ProcessPSE(isoReader, "2PAY.SYS.DDF01");

                if (CardData.AvailableAIDs.Count == 0)
                {
                    // Generate AID list manually
                    CardData.AvailableAIDs = GenerateCandidateList(isoReader);
                    foreach (byte[] aid in CardData.AvailableAIDs)
                    {
                        CardData.AvailableADFs.Add(new ADFModel()
                        {
                            AID = aid
                        });
                    }
                }

                CardData.AvailableAIDs = CardData.AvailableAIDs.Distinct().ToList();

                WarmReset(isoReader);

                // Process Application for each AIDs
                foreach (byte[] AID in CardData.AvailableAIDs)
                {
                    ProcessApplication(isoReader, AID);

                    WarmReset(isoReader);
                }

                GetCPLCData(isoReader);

                isoReader.Disconnect(SCardReaderDisposition.Reset);
            }
        }

        /// <summary>
        /// Gets the cold ATR from the card.
        /// </summary>
        /// <param name="context">The card context</param>
        /// <returns>The cold ATR as a byte array</returns>
        private static byte[] GetColdAtr(ISCardContext context)
        {
            SCardReaderState readerState = context.GetReaderStatus(SelectedReader);
            return readerState.Atr;
        }

        /// <summary>
        /// Warm Resets the card.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader.</param>
        private static void WarmReset(IsoReader isoReader)
        {
            isoReader.Disconnect(SCardReaderDisposition.Reset);
            isoReader.Connect(SelectedReader, SCardShareMode.Shared, SCardProtocol.Any);
        }

        /// <summary>
        /// Processes the PSE and generates the supported application list.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader.</param>
        /// <returns>True if the PSE is processed successfully, else false</returns>
        private static void ProcessPSE(IsoReader isoReader, string dfName)
        {
            // Select PSE
            Response response = SelectFileCommand(isoReader, DataProcessor.AsciiStringToByteArray(dfName));

            // Might be a french card
            if (response.SW1 == 0x6E && response.SW2 == 0x00)
            {
                WarmReset(isoReader);
                response = SelectFileCommand(isoReader, DataProcessor.AsciiStringToByteArray(dfName));
            }

            if (response.SW1 == 0x61)
            {
                response = GetResponseCommand(isoReader, response.SW2);
            }

            if (!(response.SW1 == 0x90 && response.SW2 == 0x00))
            {
                return;
            }

            CardData.FCIofDDF = response.GetData();

            // Extract SFI
            EmvTlvList FCIofDDFTags = EmvTlvList.Parse(CardData.FCIofDDF);
            if (FCIofDDFTags[0].Children[1].Children[0].Tag.Hex == "88")
            {
                int sfi = FCIofDDFTags[0].Children[1].Children[0].Value.Bytes[0];
                int record = 1;
                int triesLeft = 5;

                do
                {
                    response = ReadRecordCommand(isoReader, record++, sfi);

                    if (response.SW1 == 0x6C && response.SW2 != 0x00)
                    {
                        response = ReadRecordCommand(isoReader, record, sfi, response.SW2);
                    }

                    // Generate list of AIDs
                    if (response.SW1 == 0x90 && response.SW2 == 0x00)
                    {
                        byte[] FCI = response.GetData();
                        CardData.AvailableAIDs.Add(DataProcessor.GetDataObject(FCI, new byte[] { 0x4F }));
                        CardData.AvailableADFs.Add(new ADFModel()
                        {
                            AID = CardData.AvailableAIDs[CardData.AvailableAIDs.Count - 1],
                        });
                        continue;
                    }

                    // Some cards have records starting from 4 or 5,
                    // so, don't quit until 5 records are checked.
                    if (record > 5)
                    {
                        triesLeft--;
                    }
                    record++;
                } while (triesLeft > 0);
            }
        }

        /// <summary>
        /// Processes an application and generates the application-related tlv data.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        /// <param name="AID">The Application ID to be processed</param>
        private static void ProcessApplication(IsoReader isoReader, byte[] AID)
        {
            // Select Application
            Response response = SelectFileCommand(isoReader, AID);

            if (response.SW1 == 0x61)
            {
                response = GetResponseCommand(isoReader, response.SW2);
            }

            if (!(response.SW1 == 0x90 && response.SW2 == 0x00))
            {
                return;
            }

            CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).ADF = response.GetData();

            byte[] PDOL = DataProcessor.GetDataObject(response.GetData(), new byte[] { 0x9F, 0x38 });
            CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).PDOL = PDOL;

            PDOL = BuildPdolDataBlock(PDOL);
            
            response = GetProcessingOptionsCommand(isoReader, PDOL);

            if (!(response.SW1 == 0x90 && response.SW2 == 00))
            {
                if (PDOL == defaultPDOL) // If PDOL is not present
                {
                    return;
                }

                response = GetProcessingOptionsCommand(isoReader, PDOL);

                if (!(response.SW1 == 0x90 && response.SW2 == 00))
                {
                    return;
                }
            }

            byte[] gpoData = response.GetData();

            CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).ProcessingOptions = gpoData;

            // Get ATC
            response = GetDataCommand(isoReader, new byte[] { 0x9F, 0x36 });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).ATC = response.GetData();
            }

            // Get LastOnlineATCRegister
            response = GetDataCommand(isoReader, new byte[] { 0x9F, 0x13 });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).LastOnlineATCRegister = response.GetData();
            }

            // Get PIN Try Counter
            response = GetDataCommand(isoReader, new byte[] { 0x9F, 0x17 });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).PinTryCounter = response.GetData();
            }

            // Get Log Entry
            response = GetDataCommand(isoReader, new byte[] { 0x9F, 0x4D });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).LogEntry = response.GetData();
            }

            // Get Log Format
            response = GetDataCommand(isoReader, new byte[] { 0x9F, 0x4F });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).LogFormat = response.GetData();
            }

            // Loop through AFL Records
            byte[] AFL;
            List<byte[]> AFLList = new List<byte[]>();

            if (gpoData[0] == 0x80)
            {
                AFL = gpoData.Skip(4).ToArray();
            }
            else
            {
                AFL = DataProcessor.GetDataObject(gpoData, new byte[] { 0x94 });
            }

            AFLList = DataProcessor.SplitArray(AFL, 4);
            AFLList.RemoveAt(AFLList.Count - 1);

            // Get Records from AFL
            foreach (byte[] afl in AFLList)
            {
                int SFI = DataProcessor.ExtractSFI(afl[0]);
                int RecordNumber = afl[1];
                
                do
                {
                    response = ReadRecordCommand(isoReader, RecordNumber, SFI);

                    if (response.SW1 == 0x6C)
                    {
                        response = ReadRecordCommand(isoReader, RecordNumber, SFI, response.SW2);
                    }

                    if (response.SW1 == 0x90 && response.SW2 == 0x00)
                    {
                        byte[] FCI = response.GetData();
                        CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).AEFs.Add(new RecordModel() { AFL = afl, FCI = FCI });
                    }
                    RecordNumber++;

                } while (RecordNumber <= afl[2]);
            }
        }

        /// <summary>
        /// Gets the CPLC data from the card.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        private static void GetCPLCData(IsoReader isoReader)
        {
            Response response = GetDataCommand(isoReader, new byte[] { 0x9F, 0x7F });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.CPLC = response.GetData();
            }
        }

        /// <summary>
        /// Generates the candidate list.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        /// <returns>The candidate list</returns>
        private static List<byte[]> GenerateCandidateList(IsoReader isoReader)
        {
            List<byte[]> candidateList = new List<byte[]>();

            foreach (string aid in AIDList.List)
            {
                Response response = SelectFileCommand(isoReader, DataProcessor.HexStringToByteArray(aid));

                if (response.SW1 == 0x61)
                {
                    response = GetResponseCommand(isoReader, response.SW2);
                }

                if (response.SW1 == 0x90 && response.SW2 == 0x00)
                {
                    candidateList.Add(DataProcessor.HexStringToByteArray(aid));
                }
            }

            return candidateList;
        }

        /// <summary>
        /// Serializes all the read data to JSON.
        /// </summary>
        /// <returns>The serialized data as JSON string</returns>
        private static string SerializeDataToJson()
        {
            if (CardData.ColdATR != null)
            {
                jsonData.ColdATR = DataProcessor.ByteArrayToHexString(CardData.ColdATR, true);
            }

            List<string> aids = new List<string>();
            foreach (byte[] aid in CardData.AvailableAIDs)
            {
                aids.Add(DataProcessor.ByteArrayToHexString(aid, true));
            }
            jsonData.AIDs = aids;

            if (CardData.FCIofDDF != null)
            {
                jsonData.TLV.Add(DecodeTLV(CardData.FCIofDDF));
            }

            if (CardData.FCIofDDFContactless != null)
            {
                jsonData.TLV.Add(DecodeTLV(CardData.FCIofDDFContactless));
            }

            foreach (ADFModel adf in CardData.AvailableADFs)
            {
                if (adf.ADF != null)
                {
                    jsonData.TLV.Add(DecodeTLV(adf.ADF));
                }

                foreach (RecordModel aef in adf.AEFs)
                {
                    if (aef.FCI != null)
                    {
                        jsonData.TLV.Add(DecodeTLV(aef.FCI));
                    }
                }

                if (adf.ATC != null)
                {
                    jsonData.TLV.Add(DecodeTLV(adf.ATC));
                }

                if (adf.LastOnlineATCRegister != null)
                {
                    jsonData.TLV.Add(DecodeTLV(adf.LastOnlineATCRegister));
                }

                if (adf.PinTryCounter != null)
                {
                    jsonData.TLV.Add(DecodeTLV(adf.PinTryCounter));
                }

                if (adf.LogEntry != null)
                {
                    jsonData.TLV.Add(DecodeTLV(adf.LogEntry));
                }

                if (adf.LogFormat != null)
                {
                    jsonData.TLV.Add(DecodeTLV(adf.LogFormat));
                }
            }

            jsonData.CPLC = DataProcessor.ByteArrayToHexString(CardData.CPLC, true);

            ClearCardData();

            return JsonConvert.SerializeObject(jsonData);
        }

        /// <summary>
        /// Decodes the TLV data.
        /// </summary>
        /// <param name="tlv">The TLV data to be decoded</param>
        /// <returns>The decoded data as object</returns>
        private static object DecodeTLV(byte[] tlv)
        {
            EmvTlvList tlvList = EmvTagParser.ParseTlvList(tlv);

            foreach (EmvTlv tlvItem in tlvList)
            {
                switch (tlvItem.Tag.Hex)
                {
                    case "6F":
                        FCI fci = new FCI();
                        foreach (EmvTlv tag in tlvItem.Children)
                        {
                            switch (tag.Tag.Hex)
                            {
                                case "84":
                                    fci.DF = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "A5":
                                    foreach (EmvTlv subtag in tag.Children)
                                    {
                                        switch (subtag.Tag.Hex)
                                        {
                                            case "5F55":
                                                fci.IssuerCountryCode2 = DataProcessor.ByteArrayToAsciiString(subtag.Value.Bytes);
                                                break;
                                            case "5F56":
                                                fci.IssuerCountryCode3 = DataProcessor.ByteArrayToAsciiString(subtag.Value.Bytes);
                                                break;
                                            case "87":
                                                fci.ApplicationPriorityIndicator = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes);
                                                break;
                                            case "42":
                                                fci.IssuerIdentificationNumber = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "50":
                                                fci.ApplicationLabel = DataProcessor.ByteArrayToAsciiString(subtag.Value.Bytes);
                                                break;
                                            case "88":
                                                fci.SFI = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes);
                                                break;
                                            case "5F2D":
                                                fci.LanguagePreference = DataProcessor.ByteArrayToAsciiString(subtag.Value.Bytes);
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                        return fci;
                    case "70":
                        ADF adf = new ADF();
                        foreach (EmvTlv tag in tlvItem.Children)
                        {
                            switch (tag.Tag.Hex)
                            {
                                case "57":
                                    Track2E track2E = new Track2E();
                                    int separatorIndex = tag.Value.Hex.IndexOf('D');

                                    track2E.PAN = tag.Value.Hex.Substring(0, separatorIndex);
                                    track2E.MajorIndustryIdentifier = track2E.PAN.Substring(0, 1);
                                    track2E.IssuerIdentifierNumber = track2E.PAN.Substring(0, 6);
                                    track2E.AccountNumber = track2E.PAN.Substring(6, track2E.PAN.Length - 7);
                                    track2E.CheckDigit = track2E.PAN.Substring(track2E.PAN.Length - 1);

                                    track2E.ExpirationData = tag.Value.Hex.Substring(separatorIndex + 1, 4);
                                    track2E.ServiceCode = tag.Value.Hex.Substring(separatorIndex + 5, 3);
                                    track2E.DiscretionaryData = tag.Value.Hex.Substring(separatorIndex + 8);

                                    adf.Track2E = track2E;
                                    break;
                                case "5F20":
                                    adf.CardholderName = DataProcessor.ByteArrayToAsciiString(tag.Value.Bytes);
                                    break;
                                case "9F1F":
                                    adf.Track1D = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "9F20":
                                    adf.Track2D = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "9F0D":
                                    adf.IssuerActionCodeDefault = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "9F0E":
                                    adf.IssuerActionCodeDenial = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "9F0F":
                                    adf.IssuerActionCodeOnline = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "5F24":
                                    adf.ApplicationExpirationDate = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "5F25":
                                    adf.ApplicationEffectiveDate = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "8C":
                                    CDOL cdol1 = new CDOL();
                                    EmvTlvList dol = EmvTagParser.ParseDol(tag.Value.Bytes);

                                    foreach (EmvTlv dolTag in dol)
                                    {
                                        switch (dolTag.Tag.Hex)
                                        {
                                            case "9A":
                                                cdol1.TransactionDate = dolTag.Length.ToString();
                                                break;
                                            case "9F37":
                                                cdol1.UnpredictableNumber = dolTag.Length.ToString();
                                                break;
                                            case "9F1A":
                                                cdol1.TerminalCountryCode = dolTag.Length.ToString();
                                                break;
                                            case "5F2A":
                                                cdol1.TransactionCountryCode = dolTag.Length.ToString();
                                                break;
                                            case "9C":
                                                cdol1.TransactionType = dolTag.Length.ToString();
                                                break;
                                            case "9F02":
                                                cdol1.AmountAuthorised = dolTag.Length.ToString();
                                                break;
                                            case "9F03":
                                                cdol1.AmountOther = dolTag.Length.ToString();
                                                break;
                                            case "95":
                                                cdol1.TerminalVerificationResults = dolTag.Length.ToString();
                                                break;
                                            case "8A":
                                                cdol1.AuthorizationResponseCode = dolTag.Length.ToString();
                                                break;
                                            case "9F35":
                                                cdol1.TerminalType = dolTag.Length.ToString();
                                                break;
                                            case "9F45":
                                                cdol1.DataAuthenticationCode = dolTag.Length.ToString();
                                                break;
                                            case "9F4C":
                                                cdol1.IccDynamicNumber = dolTag.Length.ToString();
                                                break;
                                            case "91":
                                                cdol1.IssuerAuthenticationData = dolTag.Length.ToString();
                                                break;
                                        }
                                    }

                                    adf.CDOL1 = cdol1;
                                    break;
                                case "8D":
                                    CDOL cdol2 = new CDOL();
                                    dol = EmvTagParser.ParseDol(tag.Value.Bytes);

                                    foreach (EmvTlv dolTag in dol)
                                    {
                                        switch (dolTag.Tag.Hex)
                                        {
                                            case "9A":
                                                cdol2.TransactionDate = dolTag.Length.ToString();
                                                break;
                                            case "9F37":
                                                cdol2.UnpredictableNumber = dolTag.Length.ToString();
                                                break;
                                            case "9F1A":
                                                cdol2.TerminalCountryCode = dolTag.Length.ToString();
                                                break;
                                            case "5F2A":
                                                cdol2.TransactionCountryCode = dolTag.Length.ToString();
                                                break;
                                            case "9C":
                                                cdol2.TransactionType = dolTag.Length.ToString();
                                                break;
                                            case "9F02":
                                                cdol2.AmountAuthorised = dolTag.Length.ToString();
                                                break;
                                            case "9F03":
                                                cdol2.AmountOther = dolTag.Length.ToString();
                                                break;
                                            case "95":
                                                cdol2.TerminalVerificationResults = dolTag.Length.ToString();
                                                break;
                                            case "8A":
                                                cdol2.AuthorizationResponseCode = dolTag.Length.ToString();
                                                break;
                                            case "9F35":
                                                cdol2.TerminalType = dolTag.Length.ToString();
                                                break;
                                            case "9F45":
                                                cdol2.DataAuthenticationCode = dolTag.Length.ToString();
                                                break;
                                            case "9F4C":
                                                cdol2.IccDynamicNumber = dolTag.Length.ToString();
                                                break;
                                            case "91":
                                                cdol2.IssuerAuthenticationData = dolTag.Length.ToString();
                                                break;
                                        }
                                    }

                                    adf.CDOL2 = cdol2;
                                    break;
                                case "5A":
                                    adf.ApplicationPAN = tag.Value.Hex;
                                    break;
                                case "5F34":
                                    adf.ApplicationPANSN = tag.Value.Hex;
                                    break;
                                case "9F08":
                                case "9F09":
                                    adf.ApplicationVersionNumber = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "5F28":
                                    adf.IssuerCountryCode = DataProcessor.ByteArrayToHexString(tag.Value.Bytes);
                                    break;
                                case "8E":
                                    adf.CVMList = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                                case "9F07":
                                    adf.ApplicationUsageControl = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
                                    break;
                            }
                        }
                        return adf;
                    case "9F36":
                        return new AdditionalData
                        {
                            ApplicationTransactionCounter = DataProcessor.ByteArrayToIntString(tlvItem.Value.Bytes)
                        };
                    case "9F13":
                        return new AdditionalData
                        {
                            LastOnlineATCRegister = DataProcessor.ByteArrayToHexString(tlvItem.Value.Bytes, true)
                        };
                    case "9F17":
                        return new AdditionalData
                        {
                            PinTryCounter = DataProcessor.ByteArrayToHexString(tlvItem.Value.Bytes, true)
                        };
                    case "9F4D":
                        return new AdditionalData
                        {
                            LogEntry = DataProcessor.ByteArrayToHexString(tlvItem.Value.Bytes, true)
                        };
                    case "9F4F":
                        return new AdditionalData
                        {
                            LogFormat = DataProcessor.ByteArrayToHexString(tlvItem.Value.Bytes, true)
                        };
                }
            }
            return new object();
        }

        /// <summary>
        /// Displays the data processed.
        /// </summary>
        /// <param name="jsonData">The JSON data to be displayed</param>
        private static void DisplayData(string jsonData)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("SCANNED EMV DATA AS JSON:");
            Console.WriteLine();
            Console.WriteLine(jsonData);
        }

        /// <summary>
        /// Clears all the fields in CardData class.
        /// </summary>
        private static void ClearCardData()
        {
            CardData.ColdATR = null;
            CardData.AvailableAIDs = new List<byte[]>();
            CardData.AvailableADFs = new List<ADFModel>();
            CardData.FCIofDDF = null;
            CardData.FCIofDDFContactless = null;
            CardData.CPLC = null;
        }




        /// <summary>
        /// SELECT file APDU command.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        /// <param name="data">Data to be sent as a part of the command</param>
        /// <returns>APDU response of the command</returns>
        private static Response SelectFileCommand(IsoReader isoReader, byte[] data)
        {
            CommandApdu command = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol)
            {
                CLA = 0x00,
                Instruction = InstructionCode.SelectFile,
                P1 = 0x04,
                P2 = 0x00,
                Data = data
            };

            return isoReader.Transmit(command);
        }

        /// <summary>
        /// GET RESPONSE APDU command.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        /// <param name="le">The expected length (Le) field of the APDU command</param>
        /// <returns>APDU response of the command</returns>
        private static Response GetResponseCommand(IsoReader isoReader, byte le)
        {
            CommandApdu command = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol)
            {
                CLA = 0x00,
                Instruction = InstructionCode.GetResponse,
                P1 = 0x00,
                P2 = 0x00,
                Le = le
            };

            return isoReader.Transmit(command);
        }

        /// <summary>
        /// READ RECORD APDU command.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        /// <param name="record">The record number to be read</param>
        /// <param name="sfi">The SFI of the file to be read</param>
        /// <param name="le">The expected length (Le) field of the APDU command. Default = 0</param>
        /// <returns>APDU response of the command</returns>
        private static Response ReadRecordCommand(IsoReader isoReader, int record, int sfi, byte le = 0x00)
        {
            CommandApdu command = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol)
            {
                CLA = 0x00,
                Instruction = InstructionCode.ReadRecord,
                P1 = (byte)record,
                P2 = DataProcessor.SFItoP2(sfi),
                Le = le
            };

            return isoReader.Transmit(command);
        }

        /// <summary>
        /// Processes the PDOL and builds the PDOL data block to be sent with the GPO command.
        /// </summary>
        /// <param name="pdol">PDOL data to be processed</param>
        /// <returns>The processed PDOL data block as a byte array</returns>
        private static byte[] BuildPdolDataBlock(byte[] pdol)
        {
            if (pdol == null)
            {
                pdol = defaultPDOL;
                return pdol;
            }
            else
            {
                byte[] data = new byte[] { 0x83, 0x00 };
                EmvTlvList tlvs = EmvTagParser.ParseDol(pdol);

                foreach (EmvTlv tlv in tlvs)
                {
                    if (tlv.Tag.Hex == "9F66" && tlv.Length == 4)
                    {
                        // Terminal Transaction Qualifiers (VISA)
                        data = DataProcessor.AddBytesToArrayEnd(data, new byte[] { 0x30, 0x00, 0x00, 0x00 });
                    }
                    else if (tlv.Tag.Hex == "9F1A" && tlv.Length == 2)
                    {
                        // Terminal country code
                        data = DataProcessor.AddBytesToArrayEnd(data, new byte[] { 0x02, 0x50 });
                    }
                    else if (tlv.Tag.Hex == "5F2A" && tlv.Length == 2)
                    {
                        // Transaction currency code
                        data = DataProcessor.AddBytesToArrayEnd(data, new byte[] { 0x09, 0x78 });
                    }
                    else if (tlv.Tag.Hex == "9A" && tlv.Length == 3)
                    {
                        // Transaction date
                        data = DataProcessor.AddBytesToArrayEnd(data, DataProcessor.HexStringToByteArray(DataProcessor.AsciiStringToHexString(DateTime.Now.ToString("yyMMdd"), false)));
                    }
                    else if (tlv.Tag.Hex == "9F35" && tlv.Length == 1)
                    {
                        // Terminal type
                        data = DataProcessor.AddBytesToArrayEnd(data, new byte[] { 0xEA });
                    }
                    else if (tlv.Tag.Hex == "9F37" && tlv.Length == 4)
                    {
                        // Transaction currency code
                        data = DataProcessor.AddBytesToArrayEnd(data, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
                    }
                    else
                    {
                        for (int i = 0; i < tlv.Length; i++)
                        {
                            data = DataProcessor.AddBytesToArrayEnd(data, new byte[] { 0x00 });
                        }
                    }
                }

                data[1] = (byte)(data.Length - 2);
                return data;
            }
        }

        /// <summary>
        /// GET PROCESSING OPTIONS APDU command.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        /// <param name="PDOL">The processed PDOL data block as a byte array</param>
        /// <returns>APDU response of the command</returns>
        private static Response GetProcessingOptionsCommand(IsoReader isoReader, byte[] PDOL)
        {
            CommandApdu command = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol)
            {
                CLA = 0x80,
                INS = 0xA8,
                P1 = 0x00,
                P2 = 0x00,
                Data = PDOL
            };

            return isoReader.Transmit(command);
        }

        /// <summary>
        /// GET DATA APDU command
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        /// <param name="tag">The tag whose data is to be fetched from the card</param>
        /// <returns>APDU response of the command</returns>
        private static Response GetDataCommand(IsoReader isoReader, byte[] tag)
        {
            CommandApdu command = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol)
            {
                CLA = 0x80,
                Instruction = InstructionCode.GetData,
                P1 = tag[0],
                P2 = tag[1]
            };

            return isoReader.Transmit(command);
        }
    }
}