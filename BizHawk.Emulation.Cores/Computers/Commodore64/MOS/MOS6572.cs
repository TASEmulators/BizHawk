using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// pal n / drean - TODO correct?
	class MOS6572
	{
		static int cycles = 65;
		static int scanwidth = cycles * 8;
		static int lines = 312;
		static int vblankstart = 0x12C % lines;
		static int vblankend = 0x00F % lines;
		static int hblankoffset = 20;
		static int hblankstart = (0x18C + hblankoffset) % scanwidth - 8; // -8 because the VIC repeats internal pixel cycles around 0x18C
		static int hblankend = (0x1F0 + hblankoffset) % scanwidth - 8;

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
				14328225 / 14,
				hblankstart, hblankend,
				vblankstart, vblankend
				);
		}
	}
}
