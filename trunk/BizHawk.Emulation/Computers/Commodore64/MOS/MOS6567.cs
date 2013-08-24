namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// vic ntsc
	static public class MOS6567
	{
        static int[] timing = Vic.TimingBuilder_XRaster(0x19C, 0x200, 0x208, 0x18C, 8);
        static int[] fetch = Vic.TimingBuilder_Fetch(timing, 0x174);
        static int[] ba = Vic.TimingBuilder_BA(fetch);
        static int[] act = Vic.TimingBuilder_Act(timing, 0x004, 0x14C);

        static int[][] pipeline = new int[][]
			{
				timing,
				fetch,
				ba,
                act
			};

        static public Vic Create()
        {
            return new Vic(65, 263, pipeline, 14318181 / 14);
        }
	}
}
