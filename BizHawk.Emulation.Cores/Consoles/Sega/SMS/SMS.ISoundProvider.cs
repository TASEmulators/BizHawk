using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	// Sound refactor TODO: Implement ISoundProvider without the need for FakeSyncSound
	public sealed partial class SMS
	{
		private FakeSyncSound _fakeSyncSound;
		private IAsyncSoundProvider ActiveSoundProvider;
		private SoundMixer SoundMixer;
	}
}
