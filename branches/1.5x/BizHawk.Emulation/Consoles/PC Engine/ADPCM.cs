using System;
using BizHawk.Emulation.Sound;
using System.IO;
using System.Globalization;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public sealed class ADPCM : ISoundProvider
    {
        ScsiCDBus SCSI;
        PCEngine pce;
        MetaspuSoundProvider SoundProvider = new MetaspuSoundProvider(ESynchMethod.ESynchMethod_V);

        // ***************************************************************************

        public ushort IOAddress;
        public ushort ReadAddress;
        public ushort WriteAddress;
        public ushort AdpcmLength;

        public int  ReadTimer,   WriteTimer;
        public byte ReadBuffer,  WriteBuffer;
        public bool ReadPending, WritePending;

        public byte[] RAM = new byte[0x10000];
  
        // ***************************************************************************
        
        public bool AdpcmIsPlaying      { get; private set; }
        public bool HalfReached         { get; private set; }
        public bool EndReached          { get; private set; }
        public bool AdpcmBusyWriting    { get { return AdpcmCdDmaRequested; } }
        public bool AdpcmBusyReading    { get { return ReadPending; } }
        public bool AdpcmCdDmaRequested { get { return (Port180B & 3) != 0; } }

        // ***************************************************************************

        public byte Port180A
        {
            set { WritePending = true; WriteTimer = 24; WriteBuffer = value; }
            get { ReadPending  = true; ReadTimer  = 24; return ReadBuffer; }
        }

        public byte Port180B;
        public byte Port180D;

        byte port180E;
        public byte Port180E
        {
            get { return port180E; }
            set
            {
                port180E = value;
                float khz = 32 / (16 - (Port180E & 0x0F));
                destSamplesPerSourceSample = 44.1f / khz;
            }
        }

        // ***************************************************************************

        public ADPCM(PCEngine pcEngine, ScsiCDBus scsi)
        {
            pce = pcEngine;
            SCSI = scsi;
            MaxVolume = 24576;
        }

        public void AdpcmControlWrite(byte value)
        {
            //Log.Error("CD","ADPCM CONTROL WRITE {0:X2}",value);
            if ((Port180D & 0x80) != 0 && (value & 0x80) == 0)
            {
                ReadAddress = 0;
                WriteAddress = 0;
                IOAddress = 0;
                nibble = false;
                AdpcmIsPlaying = false;
                HalfReached = false;
                EndReached = false;
                playingSample = 0;
                Playback44khzTimer = 0;
                magnitude = 0;
            }

            if ((value & 8) != 0)
            {
                ReadAddress = IOAddress;
                if ((value & 4) == 0)
                    ReadAddress--;
            }

            if ((Port180D & 2) == 0 && (value & 2) != 0)
            {
                WriteAddress = IOAddress;
                if ((value & 1) == 0)
                    WriteAddress--;
            }

            if ((value & 0x10) != 0)
            {
                AdpcmLength = IOAddress;
                EndReached = false;
            }

            if (AdpcmIsPlaying && (value & 0x20) == 0)
                AdpcmIsPlaying = false; // clearing this bit stops playback

            if (AdpcmIsPlaying == false && (value & 0x20) != 0)
            {
                if ((value & 0x40) == 0)
                    Console.WriteLine("a thing thats normally set is not set");

                AdpcmIsPlaying = true;
                playingSample = 2048;
                magnitude = 0;
                Playback44khzTimer = 0;
            }

            Port180D = value;
        }

        public void Think(int cycles)
        {
            Playback44khzTimer -= cycles;
            if (Playback44khzTimer < 0)
            {
                Playback44khzTimer += 162.81f; // # of CPU cycles that translate to one 44100hz sample.
                AdpcmEmitSample();
            }

            if (ReadTimer  > 0) ReadTimer  -= cycles;
            if (WriteTimer > 0) WriteTimer -= cycles;

            if (ReadPending && ReadTimer <= 0)
            {
                ReadBuffer = RAM[ReadAddress++];
                ReadPending = false;
                if (AdpcmLength > ushort.MinValue)
                    AdpcmLength--;
                else
                {
                    EndReached = true;
                    HalfReached = false;
                    //Port180D &= 0x9F;
                }
            }

            if (WritePending && WriteTimer <= 0)
            {
                RAM[WriteAddress++] = WriteBuffer;
                WritePending = false;
                if (AdpcmLength < ushort.MaxValue) 
                    AdpcmLength++;
                HalfReached = AdpcmLength < 0x8000;
            }

            if (AdpcmCdDmaRequested)
            {
                if (SCSI.REQ && SCSI.IO && !SCSI.CD && !SCSI.ACK)
                {
                    byte dmaByte = SCSI.DataBits;
                    RAM[WriteAddress++] = dmaByte;
                    AdpcmLength++;

                    SCSI.ACK = false;
                    SCSI.REQ = false;
                    SCSI.Think();
                }

                if (SCSI.DataTransferInProgress == false)
                    Port180B = 0;
            }

            pce.IntADPCM = HalfReached;
            pce.IntStop = EndReached;
            pce.RefreshIRQ2();
        }
        
        // ***************************************************************************
        //                              Playback Functions
        // ***************************************************************************

        float Playback44khzTimer;
        int playingSample;
        float nextSampleTimer;
        float destSamplesPerSourceSample;
        bool nibble;
        int magnitude;

        static readonly int[] StepSize = 
        {
            0x0002, 0x0006, 0x000A, 0x000E, 0x0012, 0x0016, 0x001A, 0x001E,
            0x0002, 0x0006, 0x000A, 0x000E, 0x0013, 0x0017, 0x001B, 0x001F,
            0x0002, 0x0006, 0x000B, 0x000F, 0x0015, 0x0019, 0x001E, 0x0022,
            0x0002, 0x0007, 0x000C, 0x0011, 0x0017, 0x001C, 0x0021, 0x0026,
            0x0002, 0x0007, 0x000D, 0x0012, 0x0019, 0x001E, 0x0024, 0x0029,
            0x0003, 0x0009, 0x000F, 0x0015, 0x001C, 0x0022, 0x0028, 0x002E,
            0x0003, 0x000A, 0x0011, 0x0018, 0x001F, 0x0026, 0x002D, 0x0034,
            0x0003, 0x000A, 0x0012, 0x0019, 0x0022, 0x0029, 0x0031, 0x0038,
            0x0004, 0x000C, 0x0015, 0x001D, 0x0026, 0x002E, 0x0037, 0x003F,
            0x0004, 0x000D, 0x0016, 0x001F, 0x0029, 0x0032, 0x003B, 0x0044,
            0x0005, 0x000F, 0x0019, 0x0023, 0x002E, 0x0038, 0x0042, 0x004C,
            0x0005, 0x0010, 0x001B, 0x0026, 0x0032, 0x003D, 0x0048, 0x0053,
            0x0006, 0x0012, 0x001F, 0x002B, 0x0038, 0x0044, 0x0051, 0x005D,
            0x0006, 0x0013, 0x0021, 0x002E, 0x003D, 0x004A, 0x0058, 0x0065,
            0x0007, 0x0016, 0x0025, 0x0034, 0x0043, 0x0052, 0x0061, 0x0070,
            0x0008, 0x0018, 0x0029, 0x0039, 0x004A, 0x005A, 0x006B, 0x007B,
            0x0009, 0x001B, 0x002D, 0x003F, 0x0052, 0x0064, 0x0076, 0x0088,
            0x000A, 0x001E, 0x0032, 0x0046, 0x005A, 0x006E, 0x0082, 0x0096,
            0x000B, 0x0021, 0x0037, 0x004D, 0x0063, 0x0079, 0x008F, 0x00A5,
            0x000C, 0x0024, 0x003C, 0x0054, 0x006D, 0x0085, 0x009D, 0x00B5,
            0x000D, 0x0027, 0x0042, 0x005C, 0x0078, 0x0092, 0x00AD, 0x00C7,
            0x000E, 0x002B, 0x0049, 0x0066, 0x0084, 0x00A1, 0x00BF, 0x00DC,
            0x0010, 0x0030, 0x0051, 0x0071, 0x0092, 0x00B2, 0x00D3, 0x00F3,
            0x0011, 0x0034, 0x0058, 0x007B, 0x00A0, 0x00C3, 0x00E7, 0x010A,
            0x0013, 0x003A, 0x0061, 0x0088, 0x00B0, 0x00D7, 0x00FE, 0x0125,
            0x0015, 0x0040, 0x006B, 0x0096, 0x00C2, 0x00ED, 0x0118, 0x0143,
            0x0017, 0x0046, 0x0076, 0x00A5, 0x00D5, 0x0104, 0x0134, 0x0163,
            0x001A, 0x004E, 0x0082, 0x00B6, 0x00EB, 0x011F, 0x0153, 0x0187,
            0x001C, 0x0055, 0x008F, 0x00C8, 0x0102, 0x013B, 0x0175, 0x01AE,
            0x001F, 0x005E, 0x009D, 0x00DC, 0x011C, 0x015B, 0x019A, 0x01D9,
            0x0022, 0x0067, 0x00AD, 0x00F2, 0x0139, 0x017E, 0x01C4, 0x0209,
            0x0026, 0x0072, 0x00BF, 0x010B, 0x0159, 0x01A5, 0x01F2, 0x023E,
            0x002A, 0x007E, 0x00D2, 0x0126, 0x017B, 0x01CF, 0x0223, 0x0277,
            0x002E, 0x008A, 0x00E7, 0x0143, 0x01A1, 0x01FD, 0x025A, 0x02B6,
            0x0033, 0x0099, 0x00FF, 0x0165, 0x01CB, 0x0231, 0x0297, 0x02FD,
            0x0038, 0x00A8, 0x0118, 0x0188, 0x01F9, 0x0269, 0x02D9, 0x0349,
            0x003D, 0x00B8, 0x0134, 0x01AF, 0x022B, 0x02A6, 0x0322, 0x039D,
            0x0044, 0x00CC, 0x0154, 0x01DC, 0x0264, 0x02EC, 0x0374, 0x03FC,
            0x004A, 0x00DF, 0x0175, 0x020A, 0x02A0, 0x0335, 0x03CB, 0x0460,
            0x0052, 0x00F6, 0x019B, 0x023F, 0x02E4, 0x0388, 0x042D, 0x04D1,
            0x005A, 0x010F, 0x01C4, 0x0279, 0x032E, 0x03E3, 0x0498, 0x054D,
            0x0063, 0x012A, 0x01F1, 0x02B8, 0x037F, 0x0446, 0x050D, 0x05D4,
            0x006D, 0x0148, 0x0223, 0x02FE, 0x03D9, 0x04B4, 0x058F, 0x066A,
            0x0078, 0x0168, 0x0259, 0x0349, 0x043B, 0x052B, 0x061C, 0x070C,
            0x0084, 0x018D, 0x0296, 0x039F, 0x04A8, 0x05B1, 0x06BA, 0x07C3,
            0x0091, 0x01B4, 0x02D8, 0x03FB, 0x051F, 0x0642, 0x0766, 0x0889,
            0x00A0, 0x01E0, 0x0321, 0x0461, 0x05A2, 0x06E2, 0x0823, 0x0963,
            0x00B0, 0x0210, 0x0371, 0x04D1, 0x0633, 0x0793, 0x08F4, 0x0A54,
            0x00C2, 0x0246, 0x03CA, 0x054E, 0x06D2, 0x0856, 0x09DA, 0x0B5E
        };

        static readonly int[] StepFactor = { -1, -1, -1, -1, 2, 4, 6, 8 };

        int AddClamped(int num1, int num2, int min, int max)
        {
            int result = num1 + num2;
            if (result < min) return min;
            if (result > max) return max;
            return result;
        }

        byte ReadNibble()
        {
            byte value;
            if (nibble == false)
                value = (byte)(RAM[ReadAddress] >> 4);
            else
            {
                value = (byte)(RAM[ReadAddress] & 0xF);
                AdpcmLength--;
                ReadAddress++;
            }

            nibble ^= true;
            return value;
        }

        void DecodeAdpcmSample()
        {
            byte sample = ReadNibble();
            bool positive = (sample & 8) == 0;
            int mag = sample & 7;
            int m = StepFactor[mag];
            int adjustment = StepSize[(magnitude * 8) + mag];
            magnitude = AddClamped(magnitude, m, 0, 48);
            if (positive == false) adjustment *= -1;
            playingSample = AddClamped(playingSample, adjustment, 0, 4095);
        }

        void AdpcmEmitSample()
        {
            if (AdpcmIsPlaying == false)
                SoundProvider.buffer.enqueue_sample(0, 0);
            else
            {
                if (nextSampleTimer <= 0)
                {
                    DecodeAdpcmSample();
                    nextSampleTimer += destSamplesPerSourceSample;
                }
                nextSampleTimer--;

                HalfReached = AdpcmLength < 0x8000;

                if (AdpcmLength == 0)
                {
                    AdpcmIsPlaying = false;
                    EndReached = true;
                    HalfReached = false;
                }

                short adjustedSample = (short)((playingSample - 2048) * MaxVolume / 2048);
                SoundProvider.buffer.enqueue_sample(adjustedSample, adjustedSample);
            }
        }

        public void GetSamples(short[] samples)
        {
            SoundProvider.GetSamples(samples);
        }

        public void DiscardSamples()
        {
            SoundProvider.DiscardSamples();
        }

        public int MaxVolume { get; set; }

        // ***************************************************************************

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(RAM);
            writer.Write(IOAddress);
            writer.Write(AdpcmLength);
            writer.Write(ReadAddress);
            writer.Write((byte)ReadTimer);
            writer.Write(ReadBuffer);
            writer.Write(ReadPending);
            writer.Write(WriteAddress);
            writer.Write((byte)WriteTimer);
            writer.Write(WriteBuffer);
            writer.Write(WritePending);
            
            writer.Write(Port180B);
            writer.Write(Port180D);
            writer.Write(Port180E);
            
            writer.Write(AdpcmIsPlaying);
            writer.Write(HalfReached);
            writer.Write(EndReached);

            writer.Write(Playback44khzTimer);
            writer.Write((ushort)playingSample);
            writer.Write(nextSampleTimer);
            writer.Write(nibble);
            writer.Write((byte)magnitude);
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            RAM          = reader.ReadBytes(0x10000);
            IOAddress    = reader.ReadUInt16();
            AdpcmLength  = reader.ReadUInt16();
            ReadAddress  = reader.ReadUInt16();
            ReadTimer    = reader.ReadByte();
            ReadBuffer   = reader.ReadByte();
            ReadPending  = reader.ReadBoolean();
            WriteAddress = reader.ReadUInt16();
            WriteTimer   = reader.ReadByte();
            WriteBuffer  = reader.ReadByte();
            WritePending = reader.ReadBoolean();

            Port180B     = reader.ReadByte();
            Port180D     = reader.ReadByte();
            Port180E     = reader.ReadByte();
            
            AdpcmIsPlaying = reader.ReadBoolean();
            HalfReached    = reader.ReadBoolean();
            EndReached     = reader.ReadBoolean();

            Playback44khzTimer = reader.ReadSingle();
            playingSample = reader.ReadUInt16();
            nextSampleTimer = reader.ReadSingle();
            nibble = reader.ReadBoolean();
            magnitude = reader.ReadByte();

            pce.IntADPCM = HalfReached;
            pce.IntStop = EndReached;
            pce.RefreshIRQ2();
        }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[ADPCM]");
            writer.Write("RAM ");
            RAM.SaveAsHex(writer);
            writer.WriteLine("IOAddress {0:X4}", IOAddress);
            writer.WriteLine("AdpcmLength {0:X4}", AdpcmLength);
            writer.WriteLine("ReadAddress {0:X4}", ReadAddress);
            writer.WriteLine("ReadTimer {0}", ReadTimer);
            writer.WriteLine("ReadBuffer {0:X2}", ReadBuffer);
            writer.WriteLine("ReadPending {0}", ReadPending);
            writer.WriteLine("WriteAddress {0:X4}", WriteAddress);
            writer.WriteLine("WriteTimer {0}", WriteTimer);
            writer.WriteLine("WriteBuffer {0:X2}", WriteBuffer);
            writer.WriteLine("WritePending {0}", WritePending);

            writer.WriteLine("Port180B {0:X2}", Port180B);
            writer.WriteLine("Port180D {0:X2}", Port180D);
            writer.WriteLine("Port180E {0:X2}", Port180E);

            writer.WriteLine("AdpcmIsPlaying {0}", AdpcmIsPlaying);
            writer.WriteLine("HalfReached {0}", HalfReached);
            writer.WriteLine("EndReached {0}", EndReached);

            writer.WriteLine("Playback44khzTimer {0}", Playback44khzTimer);
            writer.WriteLine("PlayingSample {0:X4}", playingSample);
            writer.WriteLine("NextSampleTimer {0}", nextSampleTimer);
            writer.WriteLine("FirstNibble {0}", nibble);
            writer.WriteLine("Magnitude {0}", magnitude);
            writer.WriteLine("[/ADPCM]\n");
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/ADPCM]") break;
                if (args[0] == "RAM")
                    RAM.ReadFromHex(args[1]);
                else if (args[0] == "IOAddress")
                    IOAddress = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "AdpcmLength")
                    AdpcmLength = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ReadAddress")
                    ReadAddress = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ReadTimer")
                    ReadTimer = int.Parse(args[1]);
                else if (args[0] == "ReadBuffer")
                    ReadBuffer = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ReadPending")
                    ReadPending = bool.Parse(args[1]);
                else if (args[0] == "WriteAddress")
                    WriteAddress = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "WriteTimer")
                    WriteTimer = int.Parse(args[1]);
                else if (args[0] == "WriteBuffer")
                    WriteBuffer = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "WritePending")
                    WritePending = bool.Parse(args[1]);

                else if (args[0] == "Port180B")
                    Port180B = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Port180D")
                    Port180D = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Port180E")
                    Port180E = byte.Parse(args[1], NumberStyles.HexNumber);

                else if (args[0] == "AdpcmIsPlaying")
                    AdpcmIsPlaying = bool.Parse(args[1]);
                else if (args[0] == "HalfReached")
                    HalfReached = bool.Parse(args[1]);
                else if (args[0] == "EndReached")
                    EndReached = bool.Parse(args[1]);

                else if (args[0] == "Playback44khzTimer")
                    Playback44khzTimer = float.Parse(args[1]);
                else if (args[0] == "PlayingSample")
                    playingSample = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "NextSampleTimer")
                    nextSampleTimer = float.Parse(args[1]);
                else if (args[0] == "FirstNibble")
                    nibble = bool.Parse(args[1]);
                else if (args[0] == "Magnitude")
                    magnitude = int.Parse(args[1]);

                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }

            pce.IntADPCM = HalfReached;
            pce.IntStop = EndReached;
            pce.RefreshIRQ2();
        }
    }
}
