using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision
	{
		private SN76489 PSG;
		private AY_3_8910 SGM_sound;
		private readonly FakeSyncSound _fakeSyncSound; 
	}
}
