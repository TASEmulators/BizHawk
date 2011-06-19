using System;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        private void WriteCD(int addr, byte value)
        {
            switch (addr & 0x1FFF)
            {
                case 0x1807:

                    if (BramEnabled && (value & 0x80) != 0)
                    {
                        Console.WriteLine("UNLOCK BRAM!");
                        BramLocked = false;
                    }
                    break;
            }
        }

        public byte ReadCD(int addr)
        {
            switch (addr & 0x1FFF)
            {
                case 0x1803:
                    if (BramEnabled)
                    {
                        Console.WriteLine("LOCKED BRAM!");
                        BramLocked = true;
                    }
                    break;
            }
            return 0xFF;
        }
    }
}
