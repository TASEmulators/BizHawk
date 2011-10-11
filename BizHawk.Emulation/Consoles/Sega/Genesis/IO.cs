namespace BizHawk.Emulation.Consoles.Sega
{
    partial class Genesis
    {
        public bool SegaCD = false;

        public byte ReadIO(int offset)
        {
            offset &= 3;
            byte value;
            switch (offset)
            {
                case 0: // version
                    value = (byte) (SegaCD ? 0x00 : 0x20);
                    switch((char)RomData[0x01F0])
                    {
                        case 'J': value |= 0x00; break;
                        case 'U': value |= 0x80; break;
                        case 'E': value |= 0xC0; break;
                        case 'A': value |= 0xC0; break;
                        case '4': value |= 0x80; break;
                        default:  value |= 0x80; break;
                    }
                    //value |= 1; // US
                    return value;
            }
            return 0xFF;
        }

        public void WriteIO(int offset, int value)
        {
            
        }
    }
}