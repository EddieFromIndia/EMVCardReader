using EMVCardReader.Models;
using Great.EmvTags;
using Newtonsoft.Json;
using PCSC;
using PCSC.Iso7816;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EMVCardReader
{
    public class Program
    {
        /// <summary>
        /// Default PDOL to be used when no PDOL is received from the card.
        /// </summary>
        private static readonly byte[] defaultPDOL = new byte[] { 0x83, 0x0B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

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
                            case "0":
                                // Select a card reader
                                string[] readerNames = context.GetReaders();
                                if (readerNames == null || readerNames.Length < 1)
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("No card readers found.");
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
                            case "1":
                                // Check if card reader still connected
                                if (string.IsNullOrEmpty(SelectedReader))
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("No card readers selected. Select a reader first.");
                                    Console.WriteLine();
                                }
                                else
                                {
                                    SCardReaderState readerState = context.GetReaderStatus(SelectedReader);
                                    Console.WriteLine();
                                    switch (readerState.CurrentState)
                                    {
                                        case SCRState.Unaware:
                                        case SCRState.Changed:
                                        case SCRState.Unknown:
                                        case SCRState.Ignore:
                                        case SCRState.Unavailable:
                                        case SCRState.Empty:
                                        case SCRState.Exclusive:
                                        case SCRState.Mute:
                                        case SCRState.Unpowered:
                                            Console.WriteLine("false");
                                            break;
                                        case SCRState.Present:
                                        case SCRState.AtrMatch:
                                        case SCRState.InUse:
                                            Console.WriteLine("true");
                                            break;
                                    }

                                    Console.WriteLine();
                                }
                                break;
                            case "2":
                                // Check card type
                                break;
                            case "3":
                                // Get card details and display
                                GenerateCardDetails(context);

                                string jsonData = SerializeDataToJson();

                                DisplayData(jsonData);
                                break;
                            case "4":
                                // Exit application
                                return 0;
                            default:
                                Console.WriteLine("An invalid number has been entered.");
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
        /// Reads the card details from the card and saves them in CardData.
        /// </summary>
        /// <param name="context">The card context</param>
        private static void GenerateCardDetails(ISCardContext context)
        {
            Console.Write("Reading card data. Please wait...");

            using (IsoReader isoReader = new IsoReader(context, SelectedReader, SCardShareMode.Shared, SCardProtocol.Any, false))
            {
                CardData.ColdATR = GetColdAtr(context);

                bool aidListGenerated = ProcessPSE(isoReader);
                if (!aidListGenerated)
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

                // Pending: WARM RESET

                // Process Application for each AIDs
                foreach (byte[] AID in CardData.AvailableAIDs)
                {
                    ProcessApplication(isoReader, AID);

                    // Pending: WARM RESET
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
        /// Processes the PSE and generates the supported application list.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader.</param>
        /// <returns>True if the PSE is processed successfully, else false</returns>
        private static bool ProcessPSE(IsoReader isoReader)
        {
            // Select PSE
            Response response = SelectFileCommand(isoReader, DataProcessor.AsciiStringToByteArray("1PAY.SYS.DDF01"));

            // Might be a french card
            if (response.SW1 == 0x6E && response.SW2 == 0x00)
            {
                // Pending: WARM RESET
                response = SelectFileCommand(isoReader, DataProcessor.AsciiStringToByteArray("1PAY.SYS.DDF01"));
            }

            // Might be a contactless card
            if (response.SW1 == 0x6A && response.SW2 == 0x82)
            {
                return ProcessPPSE(isoReader);
            }

            if (response.SW1 == 0x61)
            {
                response = GetResponseCommand(isoReader, response.SW2);
            }

            if (!(response.SW1 == 0x90 && response.SW2 == 0x00))
            {
                return false;
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
                        CardData.AvailableAIDs.Add(DataProcessor.GetDataObject(FCI, new byte[] { 0x4f }));
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

            return true;
        }

        /// <summary>
        /// Processes the PPSE and generates the supported application list.
        /// </summary>
        /// <param name="isoReader">The instance of the currently used ISO/IEC 7816 compliant reader</param>
        /// <returns>True if the PPSE is processed successfully, else false</returns>
        private static bool ProcessPPSE(IsoReader isoReader)
        {
            // Select PPSE
            Response response = SelectFileCommand(isoReader, DataProcessor.AsciiStringToByteArray("2PAY.SYS.DDF01"));

            if (response.SW1 == 0x61)
            {
                response = GetResponseCommand(isoReader, response.SW2);
            }

            if (!(response.SW1 == 0x90 && response.SW2 == 0x00))
            {
                return false;
            }

            CardData.FCIofDDFContactless = response.GetData();

            // Extract SFI
            EmvTlvList FCIofDDFTags = EmvTlvList.Parse(CardData.FCIofDDFContactless);
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
                        CardData.AvailableAIDs.Add(DataProcessor.GetDataObject(FCI, new byte[] { 0x4f }));
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

            return true;
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

            byte[] PDOL = DataProcessor.GetDataObject(response.GetData(), new byte[] { 0x9f, 0x38 });
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
            response = GetDataCommand(isoReader, new byte[] { 0x9f, 0x36 });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).ATC = response.GetData();
            }

            // Get LastOnlineATCRegister
            response = GetDataCommand(isoReader, new byte[] { 0x9f, 0x13 });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).LastOnlineATCRegister = response.GetData();
            }

            // Get PIN Try Counter
            response = GetDataCommand(isoReader, new byte[] { 0x9f, 0x17 });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).PinTryCounter = response.GetData();
            }

            // Get Log Entry
            response = GetDataCommand(isoReader, new byte[] { 0x9f, 0x4D });
            if (response.SW1 == 0x90 && response.SW2 == 00)
            {
                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).LogEntry = response.GetData();
            }

            // Get Log Format
            response = GetDataCommand(isoReader, new byte[] { 0x9f, 0x4F });
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

            // Get Records from AFL
            foreach (byte[] afl in AFLList)
            {
                int RecordNumber = afl[1];

                // Pending: Check record number
                do
                {
                    response = ReadRecordCommand(isoReader, RecordNumber, RecordNumber);

                    if (response.SW1 == 0x6C)
                    {
                        response = ReadRecordCommand(isoReader, RecordNumber, RecordNumber, response.SW2);
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
            Response response = GetDataCommand(isoReader, new byte[] { 0x9f, 0x7F });
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

                if (response.SW1 == 0x61 || (response.SW1 == 0x90 && response.SW2 == 0x00))
                {
                    response = GetResponseCommand(isoReader, response.SW2);

                    if (response.SW1 == 0x90 || response.SW2 == 0x00)
                    {
                        candidateList.Add(DataProcessor.HexStringToByteArray(aid));
                    }
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
            jsonData.ColdATR = DataProcessor.ByteArrayToHexString(CardData.ColdATR, true);

            List<string> aids = new List<string>();
            foreach (byte[] aid in CardData.AvailableAIDs)
            {
                aids.Add(DataProcessor.ByteArrayToHexString(aid, true));
            }
            jsonData.AIDs = aids;

            jsonData.TLV.Add(DecodeTLV(CardData.FCIofDDF));
            jsonData.TLV.Add(DecodeTLV(CardData.FCIofDDFContactless));

            foreach (ADFModel adf in CardData.AvailableADFs)
            {
                jsonData.TLV.Add(DecodeTLV(adf.ADF));

                foreach (RecordModel aef in adf.AEFs)
                {
                    jsonData.TLV.Add(DecodeTLV(aef.FCI));
                }

                jsonData.TLV.Add(DecodeTLV(adf.ATC));
                jsonData.TLV.Add(DecodeTLV(adf.LastOnlineATCRegister));
                jsonData.TLV.Add(DecodeTLV(adf.PinTryCounter));
                jsonData.TLV.Add(DecodeTLV(adf.LogEntry));
                jsonData.TLV.Add(DecodeTLV(adf.LogFormat));
            }

            jsonData.CPLC = DataProcessor.ByteArrayToHexString(CardData.CPLC, true);

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
                                                fci.ApplicationPriorityIndicator = DataProcessor.ByteArrayToAsciiString(subtag.Value.Bytes);
                                                break;
                                            case "42":
                                                fci.IssuerIdentificationNumber = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "50":
                                                fci.ApplicationLabel = DataProcessor.ByteArrayToAsciiString(subtag.Value.Bytes);
                                                break;
                                            case "88":
                                                fci.SFI = DataProcessor.ByteArrayToAsciiString(subtag.Value.Bytes);
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
                                    adf.Track2E = DataProcessor.ByteArrayToHexString(tag.Value.Bytes, true);
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
                                case "8C":
                                    CDOL cdol1 = new CDOL();
                                    foreach (EmvTlv subtag in tag.Children)
                                    {
                                        switch (subtag.Tag.Hex)
                                        {
                                            case "9A":
                                                cdol1.TransactionDate = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9F37":
                                                cdol1.UnpredictableNumber = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9F1A":
                                                cdol1.TerminalCountryCode = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "5F2A":
                                                cdol1.TransactionCountryCode = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9C":
                                                cdol1.TransactionType = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9F02":
                                                cdol1.AmountAuthorised = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9F03":
                                                cdol1.AmountOther = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "95":
                                                cdol1.TerminalVerificationResults = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "8A":
                                                cdol1.AuthorizationResponseCode = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                        }
                                    }
                                    adf.CDOL1 = cdol1;
                                    break;
                                case "8D":
                                    CDOL cdol2 = new CDOL();
                                    foreach (EmvTlv subtag in tag.Children)
                                    {
                                        switch (subtag.Tag.Hex)
                                        {
                                            case "9A":
                                                cdol2.TransactionDate = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9F37":
                                                cdol2.UnpredictableNumber = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9F1A":
                                                cdol2.TerminalCountryCode = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "5F2A":
                                                cdol2.TransactionCountryCode = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9C":
                                                cdol2.TransactionType = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9F02":
                                                cdol2.AmountAuthorised = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "9F03":
                                                cdol2.AmountOther = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "95":
                                                cdol2.TerminalVerificationResults = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                            case "8A":
                                                cdol2.AuthorizationResponseCode = DataProcessor.ByteArrayToHexString(subtag.Value.Bytes, true);
                                                break;
                                        }
                                    }
                                    adf.CDOL2 = cdol2;
                                    break;
                                case "5A":
                                    adf.ApplicationPAN = DataProcessor.ByteArrayToHexString(tag.Value.Bytes);
                                    break;
                                case "5F34":
                                    adf.ApplicationPANSN = DataProcessor.ByteArrayToHexString(tag.Value.Bytes);
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
                            ApplicationTransactionCounter = DataProcessor.ByteArrayToHexString(tlvItem.Value.Bytes)
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
                        data = DataProcessor.AddBytesToArray(data, new byte[] { 0x30, 0x00, 0x00, 0x00 });
                    }
                    else if (tlv.Tag.Hex == "9F1A" && tlv.Length == 2)
                    {
                        // Terminal country code
                        data = DataProcessor.AddBytesToArray(data, new byte[] { 0x02, 0x50 });
                    }
                    else if (tlv.Tag.Hex == "5F2A" && tlv.Length == 2)
                    {
                        // Transaction currency code
                        data = DataProcessor.AddBytesToArray(data, new byte[] { 0x09, 0x78 });
                    }
                    else if (tlv.Tag.Hex == "9A" && tlv.Length == 3)
                    {
                        // Transaction date
                        data = DataProcessor.AddBytesToArray(data, DataProcessor.HexStringToByteArray(DataProcessor.AsciiStringToHexString(DateTime.Now.ToString("yyMMdd"), false)));
                    }
                    else if (tlv.Tag.Hex == "9F37" && tlv.Length == 4)
                    {
                        // Transaction currency code
                        data = DataProcessor.AddBytesToArray(data, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
                    }
                    else
                    {
                        for (int i = 0; i < tlv.Length; i++)
                        {
                            data = DataProcessor.AddBytesToArray(data, new byte[] { 0x00 });
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
        /// v
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
            CommandApdu command = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol)
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