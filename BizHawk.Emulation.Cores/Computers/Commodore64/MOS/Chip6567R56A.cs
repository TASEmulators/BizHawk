using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// vic ntsc old
	// TODO is everything right? it's mostly a copy from the other NTSC chip with tweaks wherever it was neccessary to fix something
	public static class Chip6567R56A
	{
		static int cycles = 64;
		static int scanwidth = cycles * 8;
		static int lines = 262;
		static int vblankstart = 0x00D % lines;
		static int vblankend = 0x018 % lines;
		static int hblankoffset = 20;
		static int hblankstart = (0x18C + hblankoffset) % scanwidth;
		static int hblankend = (0x1F0 + hblankoffset) % scanwidth;

		static int[] timing = Vic.TimingBuilder_XRaster(0x19C, 0x200, scanwidth, -1, -1);
		static int[] fetch = Vic.TimingBuilder_Fetch(timing, 0x174);
		static int[] ba = Vic.TimingBuilder_BA(fetch);
		static int[] act = Vic.TimingBuilder_Act(timing, 0x004, 0x14C, hblankstart, hblankend);

		static int[][] pipeline = {
				timing,
				fetch,
				ba,
				act
			};

		public static Vic Create()
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
