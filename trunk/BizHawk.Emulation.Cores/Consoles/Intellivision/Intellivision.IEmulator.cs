using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed partial class Intellivision : IEmulator
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

		public bool StartAsyncSound()
		{
			return true;
		}

		public void EndAsyncSound()
		{

		}

		public ControllerDefinition ControllerDefinition
		{
			get { return IntellivisionController; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound)
		{
			Frame++;
			_cpu.AddPendingCycles(14934);
			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				_stic.Execute(cycles);
				Connect();
				_cpu.LogData();
			}
		}

		public int Frame { get; private set; }

		public string SystemId
		{
			get { return "INTV"; }
		}

		public bool DeterministicEmulation { get { return true; } }

		[FeatureNotImplemented]
		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{

		}
	}
}
