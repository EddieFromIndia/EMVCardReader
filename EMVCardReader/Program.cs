﻿using Great.EmvTags;
using PCSC;
using PCSC.Iso7816;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMVCardReader
{
    public class Program
    {
        private static readonly byte[] defaultPDOL = new byte[] { 0x83, 0x0B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        private static void Main()
        {
            try
            {
                using (ISCardContext context = ContextFactory.Instance.Establish(SCardScope.System))
                {
                    string[] readerNames = context.GetReaders();
                    if (IsEmpty(readerNames))
                    {
                        Console.Error.WriteLine("You need at least one reader to run this application.");
                        Console.ReadKey();
                        return;
                    }

                    string name = ChooseReader(readerNames);
                    if (name is null)
                    {
                        return;
                    }

                    Console.WriteLine();
                    Console.WriteLine("Reading card data. Please wait...");
                    Console.WriteLine();

                    using (IsoReader isoReader = new IsoReader(context, name, SCardShareMode.Shared, SCardProtocol.Any, false))
                    {
                        bool isCandidateGenerated = false;
                        SCardReaderState readerState = context.GetReaderStatus(name);
                        CardData.ColdATR = readerState.Atr;

                        // For Contact Applications
                        Response response = SelectFileCommand(isoReader, Encoding.ASCII.GetBytes("1PAY.SYS.DDF1"));

                        if (response.SW1 != 0x61 && !(response.SW1 == 0x90 && response.SW2 == 0x00))
                        {
                            CardData.AvailableAIDs = GenerateCandidateList(isoReader);
                            CardData.AvailableADFs.Add(new ADFModel()
                            {
                                AID = CardData.AvailableAIDs[CardData.AvailableAIDs.Count - 1]
                            });

                            isCandidateGenerated = true;
                        }

                        if (!isCandidateGenerated)
                        {
                            response = GetResponseCommand(isoReader, response.SW2);

                            if (response.SW1 != 0x90 || response.SW2 != 0x00 || response.GetData()[0] != 0x6f)
                            {
                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                throw new Exception($"FCI of DDF not found!\nResponse [SW1 SW2]: {response.SW1} {response.SW2}\nData: {response.GetData().ToArray()}");
                            }

                            CardData.FCIofDDF = response.GetData();

                            EmvTlvList FCIofDDFTags = EmvTlvList.Parse(CardData.FCIofDDF);
                            if (FCIofDDFTags[0].Children[1].Children[0].Tag.Hex != "88")
                            {
                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                throw new Exception("No Application found in card!");
                            }

                            int sfi = FCIofDDFTags[0].Children[1].Children[0].Value.Bytes[0];

                            do
                            {
                                response = ReadRecordCommand(isoReader, (byte)sfi, Helpers.SFItoP2(sfi));
                                sfi++;

                                if (response.SW1 == 0x6A && response.SW2 == 0x83)
                                {
                                    break;
                                }

                                if (response.SW1 != 0x6C)
                                {
                                    isoReader.Disconnect(SCardReaderDisposition.Reset);
                                    throw new Exception($"Error fetching ADF.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                }

                                response = ReadRecordCommand(isoReader, (byte)sfi, Helpers.SFItoP2(sfi), response.SW2);

                                if (response.SW1 == 0x90 && response.SW2 == 0x00)
                                {
                                    byte[] ADF = response.GetData();
                                    CardData.AvailableAIDs.Add(Helpers.GetDataObject(ADF, new byte[] { 0x4f }));
                                    CardData.AvailableADFs.Add(new ADFModel()
                                    {
                                        AID = CardData.AvailableAIDs[CardData.AvailableAIDs.Count - 1],
                                        ADF = ADF,
                                        SFI = sfi
                                    });
                                }

                            } while (true);
                        }

                        foreach (byte[] AID in CardData.AvailableAIDs)
                        {
                            response = SelectFileCommand(isoReader, AID);

                            if (response.SW1 == 0x6A && response.SW2 == 0x81)
                            {
                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                throw new Exception($"The ADF with AID \"{AID}\" cannot be selected.");
                            }

                            if (response.SW1 == 0x6A && response.SW2 == 0x82)
                            {
                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                throw new Exception($"ADF with AID \"{AID}\" not found.");
                            }

                            if (response.SW1 == 0x62 && response.SW2 == 0x83)
                            {
                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                throw new Exception($"ADF with AID \"{AID}\" has been invalidated!");
                            }

                            if (response.SW1 == 0x61)
                            {
                                response = GetResponseCommand(isoReader, response.SW2);

                                if (response.SW1 != 0x90 || response.SW2 != 0x00 || response.GetData()[0] != 0x6f)
                                {
                                    isoReader.Disconnect(SCardReaderDisposition.Reset);
                                    throw new Exception($"FCI of ADF with AID \"{AID}\" not found!\nResponse [SW1 SW2]: {response.SW1} {response.SW2}\nData: {response.GetData().ToArray()}");
                                }

                                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).FCI = response.GetData();
                            }
                            else if (response.SW1 == 0x90 && response.SW2 == 0x00)
                            {
                                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).FCI = response.GetData();
                            }
                            else
                            {
                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                throw new Exception($"Error fetching ADF with AID \"{AID}\".\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                            }

                            byte[] PDOL = Helpers.GetDataObject(response.GetData(), new byte[] { 0x9f, 0x38 });
                            CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).PDOL = PDOL;

                            PDOL = BuildPdolDataBlock(PDOL);

                            response = GetProcessingOptionsCommand(isoReader, PDOL);

                            if (response.SW1 == 0x90 && response.SW2 == 00)
                            {
                                byte[] data = response.GetData();

                                CardData.AvailableADFs.FirstOrDefault(adf => adf.AID == AID).ProcessingOptions = data;

                                // Loop through AFL Records
                                byte[] AFL;
                                List<byte[]> AFLList = new List<byte[]>();

                                if (data[0] == 0x80)
                                {
                                    AFL = data.Skip(4).ToArray();
                                }
                                else
                                {
                                    AFL = Helpers.GetDataObject(data, new byte[] { 0x94 });
                                }

                                AFLList = Helpers.SplitArray(AFL, 4);

                                // Get Records from AFL
                                foreach (byte[] afl in AFLList)
                                {
                                    int RecordNumber = afl[1];

                                    do
                                    {
                                        response = ReadRecordCommand(isoReader, (byte)RecordNumber, Helpers.SFItoP2(RecordNumber));

                                        if (response.SW1 == 0x6A && response.SW2 == 0x82)
                                        {
                                            isoReader.Disconnect(SCardReaderDisposition.Reset);
                                            throw new Exception($"File not found for SFI {Helpers.ExtractSFI(afl[0])} and Record {RecordNumber}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                        }

                                        if (response.SW1 == 0x6A && response.SW2 == 0x83)
                                        {
                                            isoReader.Disconnect(SCardReaderDisposition.Reset);
                                            throw new Exception($"Record {RecordNumber} not found for SFI {Helpers.ExtractSFI(afl[0])}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                        }

                                        if (response.SW1 != 0x6C)
                                        {
                                            isoReader.Disconnect(SCardReaderDisposition.Reset);
                                            throw new Exception($"Error fetching AEF with AID: {AID} and AFL: {afl}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                        }

                                        response = ReadRecordCommand(isoReader, (byte)RecordNumber, Helpers.SFItoP2(RecordNumber), response.SW2);

                                        if (response.SW1 == 0x6A && response.SW2 == 0x82)
                                        {
                                            isoReader.Disconnect(SCardReaderDisposition.Reset);
                                            throw new Exception($"File not found for SFI {Helpers.ExtractSFI(afl[0])} and Record {RecordNumber}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                        }

                                        if (response.SW1 == 0x6A && response.SW2 == 0x83)
                                        {
                                            isoReader.Disconnect(SCardReaderDisposition.Reset);
                                            throw new Exception($"Record {RecordNumber} not found for SFI {Helpers.ExtractSFI(afl[0])}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
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
                        }

                        // For Contactless Applications
                        response = SelectFileCommand(isoReader, Encoding.ASCII.GetBytes("2PAY.SYS.DDF1"));

                        if (response.SW1 != 0x61 && !(response.SW1 == 0x90 && response.SW2 == 0x00))
                        {
                            CardData.AvailableAIDs = GenerateCandidateList(isoReader);
                            CardData.AvailableADFsContactless.Add(new ADFModel()
                            {
                                AID = CardData.AvailableAIDs[CardData.AvailableAIDs.Count - 1]
                            });

                            isCandidateGenerated = true;
                        }

                        if (!isCandidateGenerated)
                        {
                            response = GetResponseCommand(isoReader, response.SW2);

                            if (response.SW1 != 0x90 || response.SW2 != 0x00 || response.GetData()[0] != 0x6f)
                            {
                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                throw new Exception($"FCI of DDF not found!\nResponse [SW1 SW2]: {response.SW1} {response.SW2}\nData: {response.GetData().ToArray()}");
                            }

                            CardData.FCIofDDFContactless = response.GetData();

                            EmvTlvList FCIofDDFTags = EmvTlvList.Parse(CardData.FCIofDDFContactless);
                            if (FCIofDDFTags[0].Children[1].Children[0].Tag.Hex != "88")
                            {
                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                throw new Exception("No Application found in card!");
                            }

                            int sfi = FCIofDDFTags[0].Children[1].Children[0].Value.Bytes[0];

                            do
                            {
                                response = ReadRecordCommand(isoReader, (byte)sfi, Helpers.SFItoP2(sfi));
                                sfi++;

                                if (response.SW1 == 0x6A && response.SW2 == 0x83)
                                {
                                    break;
                                }

                                if (response.SW1 != 0x6C)
                                {
                                    isoReader.Disconnect(SCardReaderDisposition.Reset);
                                    throw new Exception($"Error fetching ADF.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                }

                                response = ReadRecordCommand(isoReader, (byte)sfi, Helpers.SFItoP2(sfi), response.SW2);

                                if (response.SW1 == 0x90 && response.SW2 == 0x00)
                                {
                                    byte[] ADF = response.GetData();
                                    CardData.AvailableAIDs.Add(Helpers.GetDataObject(ADF, new byte[] { 0x4f }));
                                    CardData.AvailableADFsContactless.Add(new ADFModel()
                                    {
                                        AID = CardData.AvailableAIDs[CardData.AvailableAIDs.Count - 1],
                                        ADF = ADF,
                                        SFI = sfi
                                    });
                                }

                            } while (true);

                            foreach (byte[] AID in CardData.AvailableAIDs)
                            {
                                response = SelectFileCommand(isoReader, AID);

                                if (response.SW1 == 0x6A && response.SW2 == 0x81)
                                {
                                    isoReader.Disconnect(SCardReaderDisposition.Reset);
                                    throw new Exception($"The ADF with AID \"{AID}\" cannot be selected.");
                                }

                                if (response.SW1 == 0x6A && response.SW2 == 0x82)
                                {
                                    isoReader.Disconnect(SCardReaderDisposition.Reset);
                                    throw new Exception($"ADF with AID \"{AID}\" not found.");
                                }

                                if (response.SW1 == 0x62 && response.SW2 == 0x83)
                                {
                                    isoReader.Disconnect(SCardReaderDisposition.Reset);
                                    throw new Exception($"ADF with AID \"{AID}\" has been invalidated!");
                                }

                                if (response.SW1 == 0x61)
                                {
                                    response = GetResponseCommand(isoReader, response.SW2);

                                    if (response.SW1 != 0x90 || response.SW2 != 0x00 || response.GetData()[0] != 0x6f)
                                    {
                                        isoReader.Disconnect(SCardReaderDisposition.Reset);
                                        throw new Exception($"FCI of ADF with AID \"{AID}\" not found!\nResponse [SW1 SW2]: {response.SW1} {response.SW2}\nData: {response.GetData().ToArray()}");
                                    }

                                    CardData.AvailableADFsContactless.FirstOrDefault(adf => adf.AID == AID).FCI = response.GetData();
                                }
                                else if (response.SW1 == 0x90 && response.SW2 == 0x00)
                                {
                                    CardData.AvailableADFsContactless.FirstOrDefault(adf => adf.AID == AID).FCI = response.GetData();
                                }
                                else
                                {
                                    isoReader.Disconnect(SCardReaderDisposition.Reset);
                                    throw new Exception($"Error fetching ADF with AID \"{AID}\".\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                }

                                byte[] PDOL = Helpers.GetDataObject(response.GetData(), new byte[] { 0x9f, 0x38 });
                                CardData.AvailableADFsContactless.FirstOrDefault(adf => adf.AID == AID).PDOL = PDOL;

                                PDOL = BuildPdolDataBlock(PDOL);

                                response = GetProcessingOptionsCommand(isoReader, PDOL);

                                if (response.SW1 == 0x90 && response.SW2 == 00)
                                {
                                    byte[] data = response.GetData();

                                    CardData.AvailableADFsContactless.FirstOrDefault(adf => adf.AID == AID).ProcessingOptions = data;

                                    // Loop through AFL Records
                                    byte[] AFL;
                                    List<byte[]> AFLList = new List<byte[]>();

                                    if (data[0] == 0x80)
                                    {
                                        AFL = data.Skip(4).ToArray();
                                    }
                                    else
                                    {
                                        AFL = Helpers.GetDataObject(data, new byte[] { 0x94 });
                                    }

                                    AFLList = Helpers.SplitArray(AFL, 4);

                                    // Get Records from AFL
                                    foreach (byte[] afl in AFLList)
                                    {
                                        int RecordNumber = afl[1];

                                        do
                                        {
                                            response = ReadRecordCommand(isoReader, (byte)RecordNumber, Helpers.SFItoP2(RecordNumber));

                                            if (response.SW1 == 0x6A && response.SW2 == 0x82)
                                            {
                                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                                throw new Exception($"File not found for SFI {Helpers.ExtractSFI(afl[0])} and Record {RecordNumber}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                            }

                                            if (response.SW1 == 0x6A && response.SW2 == 0x83)
                                            {
                                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                                throw new Exception($"Record {RecordNumber} not found for SFI {Helpers.ExtractSFI(afl[0])}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                            }

                                            if (response.SW1 != 0x6C)
                                            {
                                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                                throw new Exception($"Error fetching AEF with AID: {AID} and AFL: {afl}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                            }

                                            response = ReadRecordCommand(isoReader, (byte)RecordNumber, Helpers.SFItoP2(RecordNumber), response.SW2);

                                            if (response.SW1 == 0x6A && response.SW2 == 0x82)
                                            {
                                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                                throw new Exception($"File not found for SFI {Helpers.ExtractSFI(afl[0])} and Record {RecordNumber}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                            }

                                            if (response.SW1 == 0x6A && response.SW2 == 0x83)
                                            {
                                                isoReader.Disconnect(SCardReaderDisposition.Reset);
                                                throw new Exception($"Record {RecordNumber} not found for SFI {Helpers.ExtractSFI(afl[0])}.\nResponse [SW1 SW2]: {response.SW1} {response.SW2}");
                                            }

                                            if (response.SW1 == 0x90 && response.SW2 == 0x00)
                                            {
                                                byte[] FCI = response.GetData();
                                                CardData.AvailableADFsContactless.FirstOrDefault(adf => adf.AID == AID).AEFs.Add(new RecordModel() { AFL = afl, FCI = FCI });
                                            }
                                            RecordNumber++;
                                        } while (RecordNumber <= afl[2]);
                                    }
                                }
                            }
                        }

                        //response = GetDataCommand(isoReader);
                    }
                }

                DisplayData();

                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
            }
        }

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

            if (int.TryParse(line, out int choice) &&
                choice >= 0 &&
                choice <= readerNames.Count)
            {
                return readerNames[choice];
            }

            Console.WriteLine("An invalid number has been entered.");
            Console.ReadKey();
            return null;
        }

        private static bool IsEmpty(ICollection<string> readerNames) => readerNames == null || readerNames.Count < 1;

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

        private static Response ReadRecordCommand(IsoReader isoReader, byte p1, byte p2, byte le = 0x00)
        {
            CommandApdu command = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol)
            {
                CLA = 0x00,
                Instruction = InstructionCode.ReadRecord,
                P1 = p1,
                P2 = p2,
                Le = le
            };

            return isoReader.Transmit(command);
        }

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
                        data = Helpers.AddBytesToArray(data, new byte[] { 0x30, 0x00, 0x00, 0x00 });
                    }
                    else if (tlv.Tag.Hex == "9F1A" && tlv.Length == 2)
                    {
                        // Terminal country code
                        data = Helpers.AddBytesToArray(data, new byte[] { 0x02, 0x50 });
                    }
                    else if (tlv.Tag.Hex == "5F2A" && tlv.Length == 2)
                    {
                        // Transaction currency code
                        data = Helpers.AddBytesToArray(data, new byte[] { 0x09, 0x78 });
                    }
                    else if (tlv.Tag.Hex == "9A" && tlv.Length == 3)
                    {
                        // Transaction date
                        data = Helpers.AddBytesToArray(data, Helpers.StringToByteArray(Helpers.StringToHex(DateTime.Now.ToString("yyMMdd"), false)));
                    }
                    else if (tlv.Tag.Hex == "9F37" && tlv.Length == 4)
                    {
                        // Transaction currency code
                        data = Helpers.AddBytesToArray(data, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
                    }
                    else
                    {
                        for (int i = 0; i < tlv.Length; i++)
                        {
                            data = Helpers.AddBytesToArray(data, new byte[] { 0x00 });
                        }
                    }
                }

                data[1] = (byte)(data.Length - 2);
                return data;
            }
        }

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

        private static Response GetDataCommand(IsoReader isoReader)
        {
            CommandApdu command = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol)
            {
                CLA = 0x80,
                Instruction = InstructionCode.GetData,
                P1 = 0x00,
                P2 = 0x00,
                Data = new byte[] { 0x9F, 0x36, 0x9F, 0x13, 0x9F, 0x17, 0x9F, 0x4D, 0x9F, 0x4F }
            };

            return isoReader.Transmit(command);
        }

        private static List<byte[]> GenerateCandidateList(IsoReader isoReader)
        {
            List<byte[]> candidateList = new List<byte[]>();

            foreach (string aid in AIDList.List)
            {
                Response response = SelectFileCommand(isoReader, Helpers.StringToByteArray(aid));

                if (response.SW1 == 0x61 || (response.SW1 == 0x90 && response.SW2 == 0x00))
                {
                    response = GetResponseCommand(isoReader, response.SW2);

                    if (response.SW1 == 0x90 || response.SW2 == 0x00)
                    {
                        candidateList.Add(Helpers.StringToByteArray(aid));
                    }
                }
            }

            return candidateList;
        }

        private static void DisplayData()
        {
            Console.WriteLine();
            Console.WriteLine("________________________________________________________________");
            Console.WriteLine("SCANNED EMV DATA:");
            Console.WriteLine($"Cold ATR:   {Helpers.ByteArrayToHexString(CardData.ColdATR)}");
            Console.WriteLine();

            if (CardData.FCIofDDF != null)
            {
                EmvTlvList FCIofDDFTags = EmvTlvList.Parse(CardData.FCIofDDF);
                Console.WriteLine($"APPLICATION #{Helpers.ByteArrayToHexString(CardData.FCIofDDF)}");
                PrintFCI(FCIofDDFTags, 1);

                foreach (ADFModel adf in CardData.AvailableADFs)
                {
                    EmvTlvList ADFTags = EmvTlvList.Parse(adf.ADF);
                    Console.WriteLine("    File 1");
                    Console.WriteLine($"        Record {adf.SFI}");
                    PrintFCI(ADFTags, 3);
                }
                Console.WriteLine();
            }

            if (CardData.FCIofDDFContactless != null)
            {
                EmvTlvList FCIofDDFTags = EmvTlvList.Parse(CardData.FCIofDDFContactless);
                Console.WriteLine($"APPLICATION #{Helpers.ByteArrayToHexString(CardData.FCIofDDFContactless)}");
                PrintFCI(FCIofDDFTags, 1);

                foreach (ADFModel adf in CardData.AvailableADFs)
                {
                    EmvTlvList ADFTags = EmvTlvList.Parse(adf.ADF);
                    Console.WriteLine("    File 1");
                    Console.WriteLine($"        Record {adf.SFI}");
                    PrintFCI(ADFTags, 3);
                }
                Console.WriteLine();
            }

            foreach (ADFModel adf in CardData.AvailableADFs)
            {
                if (adf.FCI == null || adf.FCI.Length == 0)
                {
                    continue;
                }

                EmvTlvList FCITags = EmvTlvList.Parse(adf.FCI);
                Console.WriteLine($"APPLICATION #{Helpers.ByteArrayToHexString(adf.AID)}");
                Console.WriteLine($"    Answer to SELECT:    {Helpers.ByteArrayToHexString(adf.FCI)}");
                PrintFCI(FCITags, 2);

                if (adf.ProcessingOptions == null)
                {
                    continue;
                }

                EmvTlvList ProcessingOptionTags = EmvTlvList.Parse(adf.ProcessingOptions);
                Console.WriteLine($"    Processing Options:    {Helpers.ByteArrayToHexString(adf.ProcessingOptions)}");
                PrintProcessingOptions(ProcessingOptionTags, 2);
            }

            foreach (ADFModel adf in CardData.AvailableADFsContactless)
            {
                if (adf.FCI == null || adf.FCI.Length == 0)
                {
                    continue;
                }

                EmvTlvList FCITags = EmvTlvList.Parse(adf.FCI);
                Console.WriteLine($"APPLICATION #{Helpers.ByteArrayToHexString(adf.AID)}");
                Console.WriteLine($"    Answer to SELECT:    {Helpers.ByteArrayToHexString(adf.FCI)}");
                PrintFCI(FCITags, 2);

                if (adf.ProcessingOptions == null)
                {
                    continue;
                }

                EmvTlvList ProcessingOptionTags = EmvTlvList.Parse(adf.ProcessingOptions);
                Console.WriteLine($"    Processing Options:    {Helpers.ByteArrayToHexString(adf.ProcessingOptions)}");
                PrintProcessingOptions(ProcessingOptionTags, 2);
            }

            foreach (ADFModel adf in CardData.AvailableADFs)
            {
                foreach (RecordModel aef in adf.AEFs)
                {
                    if (aef.FCI == null || aef.FCI.Length == 0)
                    {
                        continue;
                    }

                    EmvTlvList AEFTags = EmvTlvList.Parse(aef.FCI);
                    Console.WriteLine($"    File {adf.SFI}");
                    Console.WriteLine($"        Record {adf.AEFs.IndexOf(aef)}");
                    PrintFCI(AEFTags, 3);
                }
            }

            foreach (ADFModel adf in CardData.AvailableADFsContactless)
            {
                foreach (RecordModel aef in adf.AEFs)
                {
                    if (aef.FCI == null || aef.FCI.Length == 0)
                    {
                        continue;
                    }

                    EmvTlvList AEFTags = EmvTlvList.Parse(aef.FCI);
                    Console.WriteLine($"    File {adf.SFI}");
                    Console.WriteLine($"        Record {adf.AEFs.IndexOf(aef)}");
                    PrintFCI(AEFTags, 3);
                }
            }
        }

        private static void PrintFCI(EmvTlvList tags, int indentationLevel = 0)
        {
            foreach (EmvTlv tag in tags)
            {
                for (int i = 0; i < indentationLevel; i++)
                {
                    Console.Write("    ");
                }

                string value = TagDatabase.Tags[tag.Tag.Hex][2] == "String" ? tag.Value.Ascii + "(S)" : tag.Value.Hex + "(H)";
                Console.WriteLine($"({tag.Tag.Hex}) {TagDatabase.Tags[tag.Tag.Hex][0]} ({tag.Length} " + (tag.Length > 1 ? "Bytes" : "Byte") + $"):   {value}");

                if (tag.Children.Count > 0)
                {
                    PrintFCI(tag.Children, ++indentationLevel);
                }
            }
        }

        private static void PrintProcessingOptions(EmvTlvList tags, int indentationLevel = 0)
        {
            string value;

            switch (tags[0].Tag.Hex)
            {
                case "80":
                    Console.WriteLine($"        ({tags[0].Tag.Hex}) Response Message Template Format 1 ({tags[0].Tag.Length} Bytes):   {tags[0].Value.Hex}(H)");
                    Console.WriteLine($"            (82) Application Interchange Profile (AIP) (2 Bytes):   {tags[0].Value.Hex.Substring(0, 4)}(H)");

                    value = Helpers.ByteToBinaryString(tags[0].Value.Bytes[0]);

                    if (value[1] == '1')
                    {
                        Console.WriteLine($"                Application Function 1.7:   SDA supported");
                    }

                    if (value[2] == '1')
                    {
                        Console.WriteLine($"                Application Function 1.6:   DDA supported");
                    }

                    if (value[3] == '1')
                    {
                        Console.WriteLine($"                Application Function 1.5:   Cardholder verification is supported");
                    }

                    if (value[4] == '1')
                    {
                        Console.WriteLine($"                Application Function 1.4:   Terminal Risk Management is to be performed");
                    }

                    if (value[5] == '1')
                    {
                        Console.WriteLine($"                Application Function 1.3:   Issuer Authentication is supported");
                    }

                    if (value[6] == '1')
                    {
                        Console.WriteLine($"                Application Function 1.2:   On device cardholder verification is supported");
                    }

                    if (value[7] == '1')
                    {
                        Console.WriteLine($"                Application Function 1.1:   CDA supported");
                    }

                    Console.WriteLine($"            (94) Application File Locator (AFL) ({tags[0].Length - 2} Bytes):   {tags[0].Value.Hex.Substring(2)}(H)");

                    List<byte[]> AFLItems = new List<byte[]>();
                    for (int i = 0; i < (tags[0].Value.Bytes.Length - 2); i += 4)
                    {
                        AFLItems.Add(tags[0].Value.Bytes.Skip(2 + i).Take(4).ToArray());
                    }

                    for (int i = 0; i < AFLItems.Count; i++)
                    {
                        Console.WriteLine($"                Item {i + 1} (4 Byte/s):   {Helpers.ByteArrayToHexString(AFLItems[i])}(H)");
                        Console.WriteLine($"                    Short File Identifier (SFI) (1 Byte/s):   {Helpers.ByteToSFI(AFLItems[i][0])}(H)");
                        Console.WriteLine($"                    First Record (1 Byte/s):   {Helpers.ByteToSFI(AFLItems[i][1])}(H)");
                        Console.WriteLine($"                    Last Record (1 Byte/s):   {Helpers.ByteToSFI(AFLItems[i][2])}(H)");
                        Console.WriteLine($"                    Number of records involved in offline data authentication (1 Byte/s):   {Helpers.ByteToSFI(AFLItems[i][3])}(H)");
                    }

                    break;
                case "77":
                    //Do something
                    break;
            }
        }
    }
}