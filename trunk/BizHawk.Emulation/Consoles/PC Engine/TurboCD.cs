using System;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        private byte[] CdIoPorts = new byte[16];

        private void InitScsiBus()
        {
            SCSI = new ScsiCDBus(this, disc);
            // this is kind of stupid
            SCSI.DataTransferReady = yes =>
                 {
                     // set or clear Ready Bit
                     if (yes)
                         CdIoPorts[3] |= 0x40;
                     else
                         CdIoPorts[3] &= 0xBF;
                 };
            SCSI.DataTransferComplete = yes =>
                {
                    if (yes)
                        CdIoPorts[3] |= 0x20; // Set "Complete"
                    else
                    {
                        CdIoPorts[3] &= 0xBF; // Clear "ready" 
   
                    }
                    
                };
        }

        private void WriteCD(int addr, byte value)
        {
            //Log.Note("CD","Write: {0:X4} {1:X2} (PC={2:X4})", addr & 0x1FFF, value, Cpu.PC);
            switch (addr & 0x1FFF)
            {
                case 0x1800: // SCSI Drive Control Line
                    CdIoPorts[0] = value;
                //    Console.WriteLine("Write to CDC Status [0] {0:X2}", value);

                    SCSI.SEL = true;
                    SCSI.Think();
                    SCSI.SEL = false;
                    SCSI.Think();

                    // this probably does some things
                    // possibly clear irq line or trigger or who knows
                    break;

                case 0x1801: // CDC Command
                    CdIoPorts[1] = value;
                    SCSI.DataBits = value;
                    SCSI.Think();
//                    Console.WriteLine("Write to CDC Command [1] {0:X2}", value);
                    break;

                case 0x1802: // ACK and Interrupt Control
                    CdIoPorts[2] = value;
                    SCSI.ACK = ((value & 0x80) != 0);

                    if ((CdIoPorts[2] & 0x04) != 0) Log.Note("CD", "INTAEN enable");
                    if ((CdIoPorts[2] & 0x08) != 0) Log.Note("CD", "INTSTOPEN enable");
                    if ((CdIoPorts[2] & 0x10) != 0) Log.Note("CD", "INTSUBEN enable");
                    if ((CdIoPorts[2] & 0x20) != 0) Log.Note("CD", "INTMEN enable");
                    if ((CdIoPorts[2] & 0x40) != 0) Log.Note("CD", "INTDEN enable");
                    if ((Cpu.IRQControlByte & 0x01) != 0) Log.Note("CD", "BTW, IRQ2 is not masked");

                    SCSI.Think();
                    RefreshIRQ2();
                    break;

                case 0x1804: // CD Reset Command
                    CdIoPorts[4] = value;
                    SCSI.RST = ((value & 0x02) != 0);
                    SCSI.Think();
                    if (SCSI.RST)
                    {
                        CdIoPorts[3] &= 0x8F; // Clear interrupt control bits
                        RefreshIRQ2();
                    }
                    break;

                case 0x1805:
                case 0x1806:
                    // Latch CDDA data... no action needed for us
                    break;

                case 0x1807: // BRAM Unlock
                    if (BramEnabled && (value & 0x80) != 0)
                    {
                        //Console.WriteLine("UNLOCK BRAM!");
                        BramLocked = false;
                    }
                    break;

                case 0x180B: // ADPCM DMA Control
                    CdIoPorts[0x0B] = value;
                    Console.WriteLine("Write to ADPCM DMA Control [B]");
                    // TODO... there is DMA to be done 
                    break;

                case 0x180D: // ADPCM Address Control
                    CdIoPorts[0x0D] = value;
                    Console.WriteLine("Write to ADPCM Address Control [D]");
                    break;

                case 0x180E: // ADPCM Playback Rate
                    CdIoPorts[0x0E] = value;
                    Console.WriteLine("Write to ADPCM Address Control [E]");
                    break;

                case 0x180F: // Audio Fade Timer
                    CdIoPorts[0x0F] = value;
                    Console.WriteLine("Write to CD Audio fade timer [F]");
                    // TODO: hook this up to audio system. and to your mother
                    break;

                default:
                    Console.WriteLine("unknown write to {0:X4}:{1:X2}",addr, value);
                    break;
            }
        }

        public byte ReadCD(int addr)
        {
            byte returnValue = 0;
            short sample;

            switch (addr & 0x1FFF)
            {
                case 0x1800: //  SCSI Drive Control Line
                    if (SCSI.IO)  returnValue |= 0x08;
                    if (SCSI.CD)  returnValue |= 0x10;
                    if (SCSI.MSG) returnValue |= 0x20;
                    if (SCSI.REQ) returnValue |= 0x40;
                    if (SCSI.BSY) returnValue |= 0x80;
                    //Log.Note("CD", "Read: 1800 {0:X2} (PC={1:X4})", returnValue, Cpu.PC);
                    return returnValue;

                case 0x1801: // Read data bus
                    //Log.Note("CD", "Read: 1801 {0:X2} (PC={1:X4})", SCSI.DataBits, Cpu.PC);
                    return SCSI.DataBits;

                case 0x1802: // ADPCM / CD Control
                    //Log.Note("CD", "Read: 1802 {0:X2} (PC={1:X4})", CdIoPorts[2], Cpu.PC);
                    return CdIoPorts[2];

                case 0x1803: // BRAM Lock
                    if (BramEnabled)
                    {
                        Log.Note("CD", "Read: 1803 {0:X2} (PC={1:X4})", CdIoPorts[3], Cpu.PC);
                        BramLocked = true;
                    
                    }
                    returnValue = CdIoPorts[3];
                    CdIoPorts[3] ^= 2;
                    return returnValue;

                case 0x1804: // CD Reset
                    //Log.Note("CD", "Read: 1804 {0:X2} (PC={1:X4})", CdIoPorts[4], Cpu.PC);
                    return CdIoPorts[4];

                case 0x1805: // CD audio data Low
                    if ((CdIoPorts[0x3] & 0x2) == 0)
                        sample = CDAudio.VolumeLeft;
                    else
                        sample = CDAudio.VolumeRight;
                    return (byte) sample;

                case 0x1806: // CD audio data High
                    if ((CdIoPorts[0x3] & 0x2) == 0)
                        sample = CDAudio.VolumeLeft;
                    else
                        sample = CDAudio.VolumeRight;
                    return (byte) (sample >> 8);

                case 0x1808: // "auto handshake data input"
                    byte ret = SCSI.DataBits;
                    //Console.WriteLine("read 1808 {0:X2} remain: {1}", ret, SCSI.DataIn.Count);
                    if (SCSI.REQ && SCSI.IO && !SCSI.CD)
                    {
                        SCSI.ACK = false;
                        SCSI.REQ = false;
                        SCSI.Think();
                    }
                    return ret;

                case 0x180F: // Audio Fade Timer
                    Log.Note("CD", "Read: 180F {0:X2} (PC={1:X4})", CdIoPorts[0xF], Cpu.PC);
                    return CdIoPorts[0x0F];

                // These are some retarded version check
                case 0x18C1: return 0xAA;
                case 0x18C2: return 0x55;
                case 0x18C3: return 0x00;
                case 0x18C5: return 0xAA;
                case 0x18C6: return 0x55;
                case 0x18C7: return 0x03;

                default:
                    Log.Note("CD", "unknown read to {0:X4}", addr);
                    return 0xFF;
            }
        }

        private void RefreshIRQ2()
        {
            int mask = CdIoPorts[2] & CdIoPorts[3] & 0x7C;
            Cpu.IRQ2Assert = (mask != 0);
        }
    }
}