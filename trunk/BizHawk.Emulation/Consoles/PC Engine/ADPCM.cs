using System;

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

        public long adpcm_read_timer,   adpcm_write_timer;
        public byte adpcm_read_buffer,  adpcm_write_buffer;
        public bool adpcm_read_pending, adpcm_write_pending;

        public byte[] ADPCM_RAM; 

        public void AdpcmControlWrite(byte value)
        {
            Log.Error("CD","ADPCM CONTROL WRITE {0:X2}",value);
            if ((CdIoPorts[0x0D] & 0x80) != 0 && (value & 0x80) == 0)
            {
                Log.Note("CD", "Reset ADPCM!");
                adpcm_read_address = 0;
                adpcm_write_address = 0;
                adpcm_io_address = 0;
            }

            else if ((value & 8) != 0)
            {
                adpcm_read_address = adpcm_io_address;
                if ((value & 4) == 0)
                    adpcm_read_address--;
            }

            else if ((CdIoPorts[0x0D] & 2) == 0 && (value & 2) != 0)
            {
                adpcm_write_address = adpcm_io_address;
                if ((value & 1) == 0)
                    adpcm_write_address--;
            }
        }

        public void AdpcmDataWrite(byte value)
        {
            adpcm_write_buffer = value;
            adpcm_write_timer = Cpu.TotalExecutedCycles + 24;
            adpcm_write_pending = true;
        }

        public byte AdpcmDataRead()
        {
            adpcm_read_pending = true;
            adpcm_read_timer = Cpu.TotalExecutedCycles + 24;
            return adpcm_read_buffer;
        }

        public bool AdpcmIsPlaying   { get { return false; } }
        public bool AdpcmBusyWriting { get { return AdpcmCdDmaRequested; } }
        public bool AdpcmBusyReading { get { return adpcm_read_pending; } }

        public void AdpcmThink()
        {
            if (adpcm_read_pending && Cpu.TotalExecutedCycles >= adpcm_read_timer)
            {
                adpcm_read_buffer = ADPCM_RAM[adpcm_read_address++];
                adpcm_read_pending = false;
            }

            if (adpcm_write_pending && Cpu.TotalExecutedCycles >= adpcm_write_timer)
            {
                ADPCM_RAM[adpcm_write_address++] = adpcm_write_buffer;
                adpcm_write_pending = false;
            }

            if (AdpcmCdDmaRequested)
            {
                //Console.WriteLine("CD->ADPCM dma...");
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
        
            // Do audio rendering and shit.
        }

        private bool AdpcmCdDmaRequested { get { return (CdIoPorts[0x0B] & 3) != 0; } }
    }
}
