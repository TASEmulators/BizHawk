namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class Genesis
    {
        // todo ???????
        public bool SegaCD = false;

        public int ReadIO(int offset)
        {
            int value;
            switch (offset)
            {
                case 0: // version
                    value = SegaCD ? 0x00 : 0x20;
                    switch((char)RomData[0x01F0])
                    {
                        case 'J': value |= 0x00; break;
                        case 'U': value |= 0x80; break;
                        case 'E': value |= 0xC0; break;
                        case 'A': value |= 0xC0; break;
                        case '4': value |= 0x80; break;
                        default:  value |= 0x80; break;
                    }
                    value |= 1; // US
                    return value;
            }
            return 0xFF;
        }

        public void WriteIO(int offset, int value)
        {
            
        }
    }
}