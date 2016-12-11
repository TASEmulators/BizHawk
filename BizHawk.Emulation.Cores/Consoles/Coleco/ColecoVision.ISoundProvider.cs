using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	// Sound refactor TODO: Implement ISoundProvider here and sort this mess out
	public partial class ColecoVision
	{
		public SN76489 PSG;

		public IAsyncSoundProvider SoundProvider { get { return PSG; } }
		//public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(SoundProvider, 735); } }
	}
}
