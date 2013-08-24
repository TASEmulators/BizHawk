namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// vic pal
	static public class MOS6569
	{
        static int[] timing = Vic.TimingBuilder_XRaster(0x194, 0x1F8, 0x1F8, -1, -1);
        static int[] fetch = Vic.TimingBuilder_Fetch(timing, 0x164);
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
            return new Vic(63, 312, pipeline, 17734472 / 18);
        }
	}
}
