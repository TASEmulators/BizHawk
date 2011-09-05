using System;
using BizHawk.Emulation.Sound;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public sealed class ADPCM
    {
        public ushort IOAddress;
        public ushort ReadAddress;
        public ushort WriteAddress;
        public ushort AdpcmLength;

        public int  ReadTimer,   WriteTimer;
        public byte ReadBuffer,  WriteBuffer;
        public bool ReadPending, WritePending;

        public byte[] RAM = new byte[0x10000];
        public MetaspuSoundProvider SoundProvider = new MetaspuSoundProvider(ESynchMethod.ESynchMethod_V);

        float Playback44khzTimer;
        
        ScsiCDBus SCSI;
        PCEngine pce;

        public ADPCM(PCEngine pcEngine, ScsiCDBus scsi)
        {
            pce = pcEngine;
            SCSI = scsi;
        }

        public void AdpcmControlWrite(byte value)
        {
            //Log.Error("CD","ADPCM CONTROL WRITE {0:X2}",value);
            if ((Port180D & 0x80) != 0 && (value & 0x80) == 0)
            {
                Log.Note("CD", "Reset ADPCM!");
                ReadAddress = 0;
                WriteAddress = 0;
                IOAddress = 0;
                nibble = false;
                playingSample = 0;
                Playback44khzTimer = 0;
                magnitude = 0;
                AdpcmIsPlaying = false;
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
                //Console.WriteLine("SET LENGTH={0:X4}", adpcm_length);
            }

            if (AdpcmIsPlaying && (value & 0x20) == 0)
                AdpcmIsPlaying = false; // only plays as long as this bit is set

            if (AdpcmIsPlaying == false && (value & 0x20) != 0)
            {
                if ((value & 0x40) == 0)
                    Console.WriteLine("a thing thats normally set is not set");

                Console.WriteLine("Start playing! READ {0:X4} LENGTH {1:X4}", ReadAddress, AdpcmLength);
                AdpcmIsPlaying = true;
        //        nibble = true;
                playingSample = 2048;
                magnitude = 0;
                Playback44khzTimer = 0;
            }

            Port180D = value;
        }

        public bool AdpcmIsPlaying   { get; private set; }
        public bool AdpcmBusyWriting { get { return AdpcmCdDmaRequested; } }
        public bool AdpcmBusyReading { get { return ReadPending; } }

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
            }

            if (WritePending && WriteTimer <= 0)
            {
                RAM[WriteAddress++] = WriteBuffer;
                WritePending = false;
                if (AdpcmLength < ushort.MaxValue) 
                    AdpcmLength++;
            }

            if (AdpcmCdDmaRequested)
            {
                if (SCSI.REQ && SCSI.IO && !SCSI.CD && !SCSI.ACK)
                {
                    byte dmaByte = SCSI.DataBits;
                    RAM[WriteAddress++] = dmaByte;

                    SCSI.ACK = false;
                    SCSI.REQ = false;
                    SCSI.Think();
                }

                if (SCSI.DataTransferInProgress == false)
                {
                    Port180B = 0;
                    Console.WriteLine("          ADPCM DMA COMPLETED");
                }
            }

            pce.IRQ2Monitor &= 0xF3;
            if (AdpcmIsPlaying == false) pce.IRQ2Monitor |= 0x08;
            pce.RefreshIRQ2();
        }

        public bool AdpcmCdDmaRequested { get { return (Port180B & 3) != 0; } }

        public byte Port180A
        {
            set
            {
                WriteBuffer = value;
                WriteTimer = 24;
                WritePending = true;
            }

            get 
            {
                ReadPending = true;
                ReadTimer = 24;
                return ReadBuffer;
            }
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
        //                              Playback Functions
        // ***************************************************************************

        static readonly int[] StepSize = 
        {
              16,  17,  19,  21,  23,  25,  28,  31,  34,  37,  41,  45,
              50,  55,  60,  66,  73,  80,  88,  97, 107, 118, 140, 143,
             157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449,
             494, 544, 598, 658, 724, 796, 876, 963,1060,1166,1282,1411,
             1552
        };

        static readonly int[] StepFactor = { -1, -1, -1, -1, 2, 4, 6, 8 };

        int playingSample;
        float nextSampleTimer = 0;
        float destSamplesPerSourceSample;
        bool nibble;
        int magnitude;

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
            // get sample. it's one nibble.
            byte sample = ReadNibble();

            bool positive = (sample & 8) == 0;
            int mag = sample & 7;
            int m = StepFactor[mag];

            magnitude = AddClamped(magnitude, m, 0, 48);
            int adjustment = StepSize[magnitude];
            if (positive == false) adjustment *= -1;
            playingSample = AddClamped(playingSample, adjustment, 0, 4095);

            //Console.WriteLine("decode: {0:X}  sample: {1}   ad_ref_index: {2}", sample,playingSample, magnitude);
        }

        void AdpcmEmitSample()
        {
            if (AdpcmIsPlaying == false)
                SoundProvider.buffer.enqueue_sample(0, 0);
            else
            {
                int rate = 16 - (Port180E & 0x0F);
                float khz = 32 / rate;

                if (nextSampleTimer <= 0)
                {
                    DecodeAdpcmSample();
                    nextSampleTimer += destSamplesPerSourceSample;
                }
                nextSampleTimer--;

                if (AdpcmLength == 0)
                {
                    AdpcmIsPlaying = false;
                }

                short adjustedSample = (short)((playingSample - 2048) << 3);
                SoundProvider.buffer.enqueue_sample(adjustedSample, adjustedSample);
            }
        }
    }
}
