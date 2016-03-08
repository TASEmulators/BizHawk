namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// vic ntsc
	public static class Chip6567R8
	{
	    private static readonly int Cycles = 65;
	    private static readonly int ScanWidth = Cycles * 8;
	    private static readonly int Lines = 263;
	    private static readonly int VblankStart = 0x00D % Lines;
	    private static readonly int VblankEnd = 0x018 % Lines;
	    private static readonly int HblankOffset = 24;
	    private static readonly int HblankStart = (0x18C + HblankOffset) % ScanWidth;
	    private static readonly int HblankEnd = (0x1F0 + HblankOffset) % ScanWidth;

	    private static readonly int[] Timing = Vic.TimingBuilder_XRaster(0x19C, 0x200, ScanWidth, 0x184, 8);
	    private static readonly int[] Fetch = Vic.TimingBuilder_Fetch(Timing, 0x174);
	    private static readonly int[] Ba = Vic.TimingBuilder_BA(Fetch);
	    private static readonly int[] Act = Vic.TimingBuilder_Act(Timing, 0x004, 0x154, 0x16C);

	    private static readonly int[][] Pipeline = {
				Timing,
				Fetch,
				Ba,
                Act
			};

		public static Vic Create(C64.BorderType borderType)
		{
			return new Vic(
				Cycles, Lines,
				Pipeline,
				14318181 / 14,
				HblankStart, HblankEnd,
				VblankStart, VblankEnd,
				borderType,
                6136, 8182
                );
		}
	}
}
