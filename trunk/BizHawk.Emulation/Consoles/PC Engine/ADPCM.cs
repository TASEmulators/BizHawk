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

        public byte[] ADPCM_RAM; 
        public byte adpcm_data_read_buffer;

        public void AdpcmControlWrite(byte value)
        {
            Log.Error("CD","ADPCM CONTROL WRITE {0:X2}",value);
            if ((CdIoPorts[0x0D] & 0x80) != 0 && (value & 0x80) == 0)
            {
                Log.Note("CD", "Reset ADPCM!");
                adpcm_read_address = 0;
                adpcm_write_address = 0;
                adpcm_io_address = 0; // ???? does this happen?
            }

            else if ((value & 8) != 0)
            {
                adpcm_read_address = adpcm_io_address;
                if ((value & 4) != 0)
                    throw new Exception("nibble offset set. BLUH");
            }

            else if ((CdIoPorts[0x0D] & 2) != 0 && (value & 2) != 0)
            {
                adpcm_write_address = adpcm_io_address;
                if ((value & 1) != 0)
                    throw new Exception("nibble offset set. we should probably do something about that.");

                Log.Error("CD", "Set ADPCM WRITE ADDRESS = {0:X4}", adpcm_write_address);
            }
        }

        public void AdpcmDataWrite(byte value)
        {
            // TODO this should probably be buffered, but for now we do it instantly
            //Console.WriteLine("ADPCM[{0:X4}] = {1:X2}", adpcm_write_address, value);
            ADPCM_RAM[adpcm_write_address++] = value;
        }

        public byte AdpcmDataRead()
        {
            byte returnValue = adpcm_data_read_buffer;
            adpcm_data_read_buffer = ADPCM_RAM[adpcm_read_address++];
            return returnValue;
        }

        public bool AdpcmIsPlaying { get { return false; } }
        public bool AdpcmBusyWriting { get { return AdpcmCdDmaRequested; } }
        public bool AdpcmBusyReading { get { return false; } }

        public void AdpcmThink()
        {
            if (AdpcmCdDmaRequested)
            {
                //Console.WriteLine("CD->ADPCM dma...");
                if (SCSI.REQ && SCSI.IO && !SCSI.CD && !SCSI.ACK)
                {
                    byte dmaByte = SCSI.DataBits;
                    if (adpcm_write_address == 0xFFF9)
                        adpcm_write_address = 0xFFF9;
                    //Console.WriteLine("ADPCM[{0:X4}] = {1:X2}", adpcm_write_address, dmaByte);
                    ADPCM_RAM[adpcm_write_address++] = dmaByte;

                    SCSI.ACK = false;
                    SCSI.REQ = false;
                    SCSI.Think();
                }

                if (SCSI.DataTransferInProgress == false)
                {
                    CdIoPorts[0x0B] = 0;
                }
            }
        
            // Do audio rendering and shit.
        }

        private bool AdpcmCdDmaRequested { get { return (CdIoPorts[0x0B] & 3) != 0; } }
    }
}
