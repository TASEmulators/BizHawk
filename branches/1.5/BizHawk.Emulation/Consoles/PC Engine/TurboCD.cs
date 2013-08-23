using System;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        public byte[] CdIoPorts = new byte[16];

        public bool IntADPCM // INTA
        {
            get { return (CdIoPorts[3] & 0x04) != 0; }
            set { CdIoPorts[3] = (byte)((CdIoPorts[3] & ~0x04) | (value ? 0x04 : 0x00)); }
        }
        public bool IntStop // INTSTOP
        {
            get { return (CdIoPorts[3] & 0x08) != 0; }
            set { CdIoPorts[3] = (byte)((CdIoPorts[3] & ~0x08) | (value ? 0x8 : 0x00)); }
        }
        public bool IntSubchannel // INTSUB
        {
            get { return (CdIoPorts[3] & 0x10) != 0; }
            set { CdIoPorts[3] = (byte)((CdIoPorts[3] & ~0x10) | (value ? 0x10 : 0x00)); }
        }
        public bool IntDataTransferComplete // INTM
        {
            get { return (CdIoPorts[3] & 0x20) != 0; }
            set { CdIoPorts[3] = (byte)((CdIoPorts[3] & ~0x20) | (value ? 0x20 : 0x00)); }
        }
        public bool IntDataTransferReady // INTD
        {
            get { return (CdIoPorts[3] & 0x40) != 0; }
            set { CdIoPorts[3] = (byte)((CdIoPorts[3] & ~0x40) | (value ? 0x40 : 0x00)); }
        }

        void SetCDAudioCallback()
        {
            CDAudio.CallbackAction = () =>
                {
                    IntDataTransferReady = false;
                    IntDataTransferComplete = true;
                    CDAudio.Stop();
                };
        }

        void WriteCD(int addr, byte value)
        {
            if (!TurboCD && !BramEnabled)
                return; // flee if no turboCD hooked up
            if (!TurboCD && addr != 0x1FF807)
                return; // only bram port available unless full TurobCD mode.

            switch (addr & 0x1FFF)
            {
                case 0x1800: // SCSI Drive Control Line
                    CdIoPorts[0] = value;
                    SCSI.SEL = true;
                    SCSI.Think();
                    SCSI.SEL = false;
                    SCSI.Think();
                    break;

                case 0x1801: // CDC Command
                    CdIoPorts[1] = value;
                    SCSI.DataBits = value;
                    SCSI.Think();
                    break;

                case 0x1802: // ACK and Interrupt Control
                    CdIoPorts[2] = value;
                    SCSI.ACK = ((value & 0x80) != 0);

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
                    // Latch CDDA data... no action needed for us (because we cheat)
                    break;

                case 0x1807: // BRAM Unlock
                    if (BramEnabled && (value & 0x80) != 0)
                        BramLocked = false;
                    break;

                case 0x1808: // ADPCM address LSB
                    ADPCM.IOAddress &= 0xFF00;
                    ADPCM.IOAddress |= value;
                    if ((CdIoPorts[0x0D] & 0x10) != 0)
                        Console.WriteLine("doing silly thing");
                    break;

                case 0x1809: // ADPCM address MSB
                    ADPCM.IOAddress &= 0x00FF;
                    ADPCM.IOAddress |= (ushort)(value << 8);
                    if ((CdIoPorts[0x0D] & 0x10) != 0)
                        Console.WriteLine("doing silly thing");
                    break;

                case 0x180A: // ADPCM Memory Read/Write Port
                    ADPCM.Port180A = value;
                    break;

                case 0x180B: // ADPCM DMA Control
                    ADPCM.Port180B = value;
                    break;

                case 0x180D: // ADPCM Address Control
                    ADPCM.AdpcmControlWrite(value);
                    break;

                case 0x180E: // ADPCM Playback Rate
                    ADPCM.Port180E = value;
                    break;

                case 0x180F: // Audio Fade Timer
                    CdIoPorts[0x0F] = value;
                    // TODO ADPCM fades/vol control also.
                    switch (value)
                    {
                        case 0:
                            CDAudio.LogicalVolume = 100;
                            break;

                        case 8:
                        case 9:
                            if (CDAudio.FadeOutFramesRemaining == 0)
                                CDAudio.FadeOut(360); // 6 seconds
                            break;

                        case 12:
                        case 13:
                            if (CDAudio.FadeOutFramesRemaining == 0)
                                CDAudio.FadeOut(120); // 2 seconds
                            break;
                    }
                    break;

                // Arcade Card ports
                case 0x1AE0:
                    ShiftRegister &= ~0xFF;
                    ShiftRegister |= value;
                    break;
                case 0x1AE1:
                    ShiftRegister &= ~0xFF00;
                    ShiftRegister |= value << 8;
                    break;
                case 0x1AE2:
                    ShiftRegister &= ~0xFF0000;
                    ShiftRegister |= value << 16;
                    break;
                case 0x1AE3:
                    ShiftRegister &= 0xFFFFFF;
                    ShiftRegister |= value << 24;
                    break;
                case 0x1AE4:
                    ShiftAmount = (byte) (value & 0x0F);
                    if (ShiftAmount != 0)
                    {
                        if ((ShiftAmount & 8) != 0)
                            ShiftRegister >>= 16 - ShiftAmount;
                        else
                            ShiftRegister <<= ShiftAmount;
                    }
                    break;
                case 0x1AE5:
                    RotateAmount = value;
                    // rotate not implemented, as no test case exists
                    break;

                default:
                    if (addr >= 0x1FFA00 && addr < 0x1FFA40)
                        WriteArcadeCard(addr & 0x1FFF, value);
                    else
                        Log.Error("CD", "unknown write to {0:X4}:{1:X2} pc={2:X4}", addr, value, Cpu.PC);
                    break;
            }
        }

        public byte ReadCD(int addr)
        {
            if (!TurboCD && !BramEnabled)
                return 0xFF; //bail if no TurboCD.
            if (!TurboCD && addr != 0x1FF803) // only allow access to $1803 unless full TurboCD mode.
                return 0xFF;

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
                    return returnValue;

                case 0x1801: // Read data bus
                    return SCSI.DataBits;

                case 0x1802: // ADPCM / CD Control
                    return CdIoPorts[2];

                case 0x1803: // BRAM Lock
                    if (BramEnabled)
                        BramLocked = true;

                    returnValue = CdIoPorts[3];
                    CdIoPorts[3] ^= 2;
                    return returnValue;

                case 0x1804: // CD Reset
                    return CdIoPorts[4];

                case 0x1805: // CD audio data Low
                    if ((CdIoPorts[3] & 0x2) == 0)
                        sample = CDAudio.VolumeLeft;
                    else
                        sample = CDAudio.VolumeRight;
                    return (byte) sample;

                case 0x1806: // CD audio data High
                    if ((CdIoPorts[3] & 0x2) == 0)
                        sample = CDAudio.VolumeLeft;
                    else
                        sample = CDAudio.VolumeRight;
                    return (byte) (sample >> 8);

                case 0x1808: // Auto Handshake Data Input
                    returnValue = SCSI.DataBits;
                    if (SCSI.REQ && SCSI.IO && !SCSI.CD)
                    {
                        SCSI.ACK = false;
                        SCSI.REQ = false;
                        SCSI.Think();
                    }
                    return returnValue;

                case 0x180A: // ADPCM Memory Read/Write Port
                    return ADPCM.Port180A;

                case 0x180B: // ADPCM Data Transfer Control
                    return ADPCM.Port180B;

                case 0x180C: // ADPCM Status
                    returnValue = 0;
                    if (ADPCM.EndReached)
                        returnValue |= 0x01;
                    if (ADPCM.AdpcmIsPlaying)
                        returnValue |= 0x08;
                    if (ADPCM.AdpcmBusyWriting)
                        returnValue |= 0x04;
                    if (ADPCM.AdpcmBusyReading)
                        returnValue |= 0x80;
                    //Log.Error("CD", "Read ADPCM Status {0:X2}", returnValue);

                    return returnValue;

                case 0x180D: // ADPCM Play Control
                    return CdIoPorts[0x0D];

                case 0x180F: // Audio Fade Timer
                    return CdIoPorts[0x0F];

                // These are some retarded version check
                case 0x18C1: return 0xAA;
                case 0x18C2: return 0x55;
                case 0x18C3: return 0x00;
                case 0x18C5: return 0xAA;
                case 0x18C6: return 0x55;
                case 0x18C7: return 0x03;

                // Arcade Card ports
                case 0x1AE0: return ArcadeCard ? (byte) (ShiftRegister >> 0)  : (byte) 0xFF;
                case 0x1AE1: return ArcadeCard ? (byte) (ShiftRegister >> 8)  : (byte) 0xFF;
                case 0x1AE2: return ArcadeCard ? (byte) (ShiftRegister >> 16) : (byte) 0xFF;
                case 0x1AE3: return ArcadeCard ? (byte) (ShiftRegister >> 24) : (byte) 0xFF;
                case 0x1AE4: return ArcadeCard ? ShiftAmount : (byte) 0xFF;
                case 0x1AE5: return ArcadeCard ? RotateAmount : (byte) 0xFF;

                case 0x1AFE: return ArcadeCard ? (byte) 0x10 : (byte) 0xFF;
                case 0x1AFF: return ArcadeCard ? (byte) 0x51 : (byte) 0xFF;

                default:
                    if (addr >= 0x1FFA00 && addr < 0x1FFA40)
                        return ReadArcadeCard(addr & 0x1FFF);
                    else
                        Log.Error("CD", "unknown read to {0:X4}", addr);
                    return 0xFF;
            }
        }

        public void RefreshIRQ2()
        {
            int mask = CdIoPorts[2] & CdIoPorts[3] & 0x7C;
            Cpu.IRQ2Assert = (mask != 0);
        }
    }
}