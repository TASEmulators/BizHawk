using System;
using BizHawk.Emulation.Sound;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        // herein I begin to realize the downside of prodigous use of partial class.
        // too much stuff in one namespace.
        // partial class still rocks though. you rock, partial class.

        public ushort adpcm_io_address;
        public ushort adpcm_read_address;
        public ushort adpcm_write_address;
        public ushort adpcm_length;

        public int adpcm_read_timer,   adpcm_write_timer;
        public byte adpcm_read_buffer,  adpcm_write_buffer;
        public bool adpcm_read_pending, adpcm_write_pending;

        long LastThink;
        float adpcm_playback_timer;

        public byte[] ADPCM_RAM;

        public MetaspuSoundProvider ADPCM_Provider;

        static readonly int[] StepSize = 
        {
              16,  17,  19,  21,  23,  25,  28,  31,  34,  37,  41,  45,
              50,  55,  60,  66,  73,  80,  88,  97, 107, 118, 140, 143,
             157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449,
             494, 544, 598, 658, 724, 796, 876, 963,1060,1166,1282,1411,
             1552
        };

        static readonly int[] StepFactor = { -1, -1, -1, -1, 2, 4, 6, 8 };

        int AddClamped(int num1, int num2, int min, int max)
        {
            int result = num1 + num2;
            if (result < min) return min;
            if (result > max) return max;
            return result;
        }

        public void AdpcmControlWrite(byte value)
        {
            Log.Error("CD","ADPCM CONTROL WRITE {0:X2}",value);
            if ((CdIoPorts[0x0D] & 0x80) != 0 && (value & 0x80) == 0)
            {
                Log.Note("CD", "Reset ADPCM!");
                adpcm_read_address = 0;
                adpcm_write_address = 0;
                adpcm_io_address = 0;
                nibble = false;
                playingSample = 0;
                adpcm_playback_timer = 0;
                magnitude = 0;
                AdpcmIsPlaying = false;
            }

            if ((value & 8) != 0)
            {
                adpcm_read_address = adpcm_io_address;
                if ((value & 4) == 0)
                    adpcm_read_address--;
            }

            if ((CdIoPorts[0x0D] & 2) == 0 && (value & 2) != 0)
            {
                adpcm_write_address = adpcm_io_address;
                if ((value & 1) == 0)
                    adpcm_write_address--;
            }

            if ((value & 0x10) != 0)
            {
                adpcm_length = adpcm_io_address;
                Console.WriteLine("SET LENGTH={0:X4}", adpcm_length);
            }

            if (AdpcmIsPlaying && (value & 0x20) == 0)
                AdpcmIsPlaying = false; // only plays as long as this bit is set

            if (AdpcmIsPlaying == false && (value & 0x20) != 0)
            {
                Console.WriteLine("Start playing!");
                AdpcmIsPlaying = true;
                nibble = false;
                playingSample = 0;
                adpcm_playback_timer = 0;
                magnitude = 0;
            }
        }

        public void AdpcmDataWrite(byte value)
        {
            adpcm_write_buffer = value;
            adpcm_write_timer = 24;
            adpcm_write_pending = true;
        }

        public byte AdpcmDataRead()
        {
            adpcm_read_pending = true;
            adpcm_read_timer = 24;
            return adpcm_read_buffer;
        }


        public bool AdpcmIsPlaying   { get; private set; }
        public bool AdpcmBusyWriting { get { return AdpcmCdDmaRequested; } }
        public bool AdpcmBusyReading { get { return adpcm_read_pending; } }
        
        Random rnd = new Random();

        int playingSample;
        int nextSampleTimer = 0;
        bool nibble;

        int magnitude;

        void DecodeAdpcmSample()
        {
            // get sample. it's one nibble.
            byte sample;
            if (nibble == false)
            {
                sample = (byte) (ADPCM_RAM[adpcm_read_address] >> 4);
                nibble = true;
            } else {
                sample = (byte)(ADPCM_RAM[adpcm_read_address] & 0xF);
                nibble = false;
                adpcm_length--;
                adpcm_read_address++;
            }

            bool positive = (sample & 8) == 0;
            int mag = sample & 7;
            int m = StepFactor[mag];

            magnitude = AddClamped(magnitude, m, 0, 48);
            int adjustment = StepSize[magnitude];
            if (positive == false) adjustment *= -1;
            playingSample = AddClamped(playingSample, adjustment, 0, 4095);
            
            adpcm_length--;
        }

        void AdpcmEmitSample()
        {
            if (AdpcmIsPlaying == false)
                ADPCM_Provider.buffer.enqueue_sample(0, 0);
            else
            {
                int rate = 16 - (CdIoPorts[0x0E] & 0x0F);
                float khz = 32 / rate;

                if (nextSampleTimer == 0)
                {
                    DecodeAdpcmSample();
                    nextSampleTimer = 4;
                }
                nextSampleTimer--;

                if (adpcm_length == 0)
                {
                    AdpcmIsPlaying = false;
                }

                short adjustedSample = (short)((playingSample - 2048) << 3);
                ADPCM_Provider.buffer.enqueue_sample(adjustedSample, adjustedSample);
            }
        }

        public void AdpcmThink()
        {
            int cycles = (int) (Cpu.TotalExecutedCycles - LastThink);
            LastThink = Cpu.TotalExecutedCycles;

            adpcm_playback_timer -= cycles;
            if (adpcm_playback_timer < 0)
            {
                adpcm_playback_timer += 162.81f; // # of CPU cycles that translate to one 44100hz sample.
                AdpcmEmitSample();
            }

            if (adpcm_read_timer  > 0) adpcm_read_timer  -= cycles;
            if (adpcm_write_timer > 0) adpcm_write_timer -= cycles;

            if (adpcm_read_pending && adpcm_read_timer <= 0)
            {
                adpcm_read_buffer = ADPCM_RAM[adpcm_read_address++];
                adpcm_read_pending = false;
                if (adpcm_length > ushort.MinValue)
                    adpcm_length--;
            }

            if (adpcm_write_pending && adpcm_write_timer <= 0)
            {
                ADPCM_RAM[adpcm_write_address++] = adpcm_write_buffer;
                adpcm_write_pending = false;
                if (adpcm_length < ushort.MaxValue) 
                    adpcm_length++;
            }

            if (AdpcmCdDmaRequested)
            {
                if (SCSI.REQ && SCSI.IO && !SCSI.CD && !SCSI.ACK)
                {
                    byte dmaByte = SCSI.DataBits;
                    ADPCM_RAM[adpcm_write_address++] = dmaByte;

                    SCSI.ACK = false;
                    SCSI.REQ = false;
                    SCSI.Think();
                }

                if (SCSI.DataTransferInProgress == false)
                {
                    CdIoPorts[0x0B] = 0;
                    Console.WriteLine("          ADPCM DMA COMPLETED");
                }
            }

            CdIoPorts[0x03] &= 0xF3;
            if (AdpcmIsPlaying == false) CdIoPorts[0x03] |= 0x08;
            RefreshIRQ2();
        }

        bool AdpcmCdDmaRequested { get { return (CdIoPorts[0x0B] & 3) != 0; } }
    }
}
