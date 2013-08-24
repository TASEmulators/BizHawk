namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// vic ntsc
	static public class MOS6567
	{
        static int[][] pipeline = new int[5][];
        static public Vic Create()
        {
            return new Vic(65, 263, pipeline, 14318181 / 14);
        }
	}
}
