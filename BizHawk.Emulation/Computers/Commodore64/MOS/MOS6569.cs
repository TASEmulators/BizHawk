namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// vic pal
	public class MOS6569
	{
        static int[] timing = Vic.TimingBuilder_XRaster(0x194, 0x1F8, 0x1F8, -1, -1);
        static int[] fetch = Vic.TimingBuilder_Fetch(timing, 0x164);
        static int[] ba = Vic.TimingBuilder_BA(fetch);

        static int[][] pipeline = new int[][]
			{
				timing,
				fetch,
				ba,
				new int[] // actions
				{
					0, 0,
					0, 0,
					0, 0,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0,
					0, Vic.pipelineUpdateVc,
					0, Vic.pipelineChkSprChunch,

					0, Vic.pipelineUpdateMcBase,
					0, Vic.pipelineChkBrdL1,
					0, Vic.pipelineChkBrdL0,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0,
					0, 0,
					Vic.pipelineChkSprDma, 0,

					Vic.pipelineChkSprDma, Vic.pipelineChkBrdR0 | Vic.pipelineChkSprExp,
					0, Vic.pipelineChkBrdR1,
					Vic.pipelineChkSprDisp, Vic.pipelineUpdateRc,
					0, 0,
					0, 0,

					0, 0,
					0, 0,
					0, 0
				}
			};

        static public Vic Create()
        {
            return new Vic(63, 312, pipeline, 17734472 / 18);
        }
	}
}
