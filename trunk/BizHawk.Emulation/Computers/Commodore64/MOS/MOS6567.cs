using System.Drawing;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// vic ntsc
	static public class MOS6567
	{
        static int cycles = 65;
        static int lines = 263;
        static int vblankstart = 0x00D;
        static int vblankend = 0x018;
        static int hblankstart = 0x18C;
        static int hblankend = 0x1F0;
        static int scanwidth = cycles * 8;

        static int[] timing = Vic.TimingBuilder_XRaster(0x19C, 0x200, scanwidth, 0x18C, 8);
        static int[] fetch = Vic.TimingBuilder_Fetch(timing, 0x174);
        static int[] ba = Vic.TimingBuilder_BA(fetch);
        static int[] act = Vic.TimingBuilder_Act(timing, 0x004, 0x14C, hblankstart, hblankend);

        static int[][] pipeline = new int[][]
			{
				timing,
				fetch,
				ba,
                act
			};

        static public Vic Create()
        {
            return new Vic(
                cycles, lines, 
                pipeline, 
                14318181 / 14,
                hblankstart, hblankend,
                vblankstart, vblankend
                );
        }
	}
}
