using System;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        private byte[] CdIoPorts = new byte[16];

        private void WriteCD(int addr, byte value)
        {
            switch (addr & 0x1FFF)
            {
                case 0x1802: // ADPCM / CD Control
                    CdIoPorts[2] = value;
                    break;

                case 0x1804: // CD Reset Command
                    CdIoPorts[4] = value;
                    break;

                case 0x1807: // BRAM Unlock
                    if (BramEnabled && (value & 0x80) != 0)
                    {
                        Console.WriteLine("UNLOCK BRAM!");
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
                    // TODO: hook this up to audio system);
                    break;

                default:
                    Console.WriteLine("unknown write to {0:X4}:{1:X2}",addr, value);
                    break;
            }
        }

        public byte ReadCD(int addr)
        {
            switch (addr & 0x1FFF)
            {
                case 0x1802: // ADPCM / CD Control
                    return CdIoPorts[2];

                case 0x1803: // BRAM Lock
                    if (BramEnabled)
                    {
                        Console.WriteLine("LOCKED BRAM!");
                        BramLocked = true;
                    }
                    return CdIoPorts[3];

                case 0x1804: // CD Reset
                    return CdIoPorts[4];

                case 0x180F: // Audio Fade Timer
                    return CdIoPorts[0x0F];

                default:
                    Console.WriteLine("unknown read to {0:X4}", addr);
                    return 0xFF;
            }
        }
    }
}
