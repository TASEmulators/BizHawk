namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// vic ntsc
	public class MOS6567 : Vic
	{
        static protected int[][] pipeline = new int[5][];

		public MOS6567()
			: base(65, 263, pipeline, 14318181 / 14)
		{
		}
	}
}
