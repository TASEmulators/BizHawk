// Contains rendering functions for legacy TMS9918 modes.

namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class VDP
    {
        private int[] PaletteTMS9918 = new int[] 
        {
			unchecked((int)0x00000000),
			unchecked((int)0xFF000000),
			unchecked((int)0xFF47B73B),
			unchecked((int)0xFF7CCF6F),
			unchecked((int)0xFF5D4EFF),
			unchecked((int)0xFF8072FF),
			unchecked((int)0xFFB66247),
			unchecked((int)0xFF5DC8ED),
			unchecked((int)0xFFD76B48),
			unchecked((int)0xFFFB8F6C),
			unchecked((int)0xFFC3CD41),
			unchecked((int)0xFFD3DA76),
			unchecked((int)0xFF3E9F2F),
			unchecked((int)0xFFB664C7),
			unchecked((int)0xFFCCCCCC),
			unchecked((int)0xFFFFFFFF)
		};

        private void RenderBackgroundM0()
        {
            int yc = ScanLine/8;
            int yofs = ScanLine%8;
            int FrameBufferOffset = ScanLine*256;
            int PatternNameOffset = mystery_pn + (yc*32);

            for (int xc=0; xc<32; xc++)
            {
                int pn = VRAM[PatternNameOffset++];
                int pv = VRAM[PatternGeneratorBase + (pn*8) + yofs];
                int colorEntry = VRAM[ColorTableBase + (pn/8)];
                int fgColor = PaletteTMS9918[(colorEntry >> 4 & 0x0F)];
                int bgColor = PaletteTMS9918[(colorEntry & 0x0F)];
                
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x80) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x40) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x20) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x10) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x08) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x04) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x02) > 0) ? fgColor : bgColor;
                FrameBuffer[FrameBufferOffset++] = ((pv & 0x01) > 0) ? fgColor : bgColor;
            }
        }
    }
}