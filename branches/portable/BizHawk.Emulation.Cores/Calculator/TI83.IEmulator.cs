using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ISoundProvider SoundProvider
		{
			get { return NullSound.SilenceProvider; }
		}

		public ISyncSoundProvider SyncSoundProvider
		{
			get { return new FakeSyncSound(NullSound.SilenceProvider, 735); }
		}

		public bool StartAsyncSound()
		{
			return true;
		}

		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition
		{
			get { return TI83Controller; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound)
		{
			_lagged = true;

			//I eyeballed this speed
			for (int i = 0; i < 5; i++)
			{
				_onPressed = Controller.IsPressed("ON");

				//and this was derived from other emus
				Cpu.ExecuteCycles(10000);
				Cpu.Interrupt = true;
			}

			Frame++;

			if (_lagged)
			{
				_lagCount++;
			}

			_isLag = _lagged;
		}

		public int Frame
		{
			get { return _frame; }
			set { _frame = value; }
		}

		public string SystemId { get { return "TI83"; } }

		public bool DeterministicEmulation { get { return true; } }

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose() { }
	}
}
