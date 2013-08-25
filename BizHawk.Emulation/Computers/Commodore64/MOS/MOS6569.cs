using System.Drawing;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// vic pal
	static public class MOS6569
	{
        static int cycles = 63;
        static int scanwidth = cycles * 8;
        static int lines = 312;
        static int vblankstart = 0x12C % lines;
        static int vblankend = 0x00F % lines;
        static int hblankoffset = 20;
        static int hblankstart = (0x17C + hblankoffset) % scanwidth;
        static int hblankend = (0x1E0 + hblankoffset) % scanwidth;

        static int[] timing = Vic.TimingBuilder_XRaster(0x194, 0x1F8, scanwidth, -1, -1);
        static int[] fetch = Vic.TimingBuilder_Fetch(timing, 0x164);
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
                17734472 / 18,
                hblankstart, hblankend,
                vblankstart, vblankend
                );
        }
	}
}
