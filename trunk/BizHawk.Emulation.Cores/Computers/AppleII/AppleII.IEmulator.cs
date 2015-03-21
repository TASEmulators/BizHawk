using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		[FeatureNotImplemented]
		public ISoundProvider SoundProvider
		{
			get { return NullSound.SilenceProvider; }
		}

		[FeatureNotImplemented]
		public ISyncSoundProvider SyncSoundProvider
		{
			get { return new FakeSyncSound(NullSound.SilenceProvider, 735); }
		}

		[FeatureNotImplemented]
		public bool StartAsyncSound()
		{
			return true;
		}

		[FeatureNotImplemented]
		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition
		{
			get { return AppleIIController; }
		}

		public IController Controller { get; set; }


		public int Frame { get; set; }

		public string SystemId { get { return "AppleII"; } }

		public bool DeterministicEmulation { get { return true; } }

		public void FrameAdvance(bool render, bool rendersound)
		{
			FrameAdv(render, rendersound);
		}

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			_machine.Dispose();
		}
	}
}
