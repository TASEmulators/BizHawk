using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : IEmulator
	{
		public string SystemId => "MAME";
		public bool DeterministicEmulation => true;
		public int Frame { get; private set; }
		public IEmulatorServiceProvider ServiceProvider { get; }
		public ControllerDefinition ControllerDefinition => MAMEController;

		private bool _memAccess = false;
		private bool _paused = true;
		private bool _exiting = false;
		private bool _frameDone = true;

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			if (_exiting)
			{
				return false;
			}

			_controller = controller;
			_paused = false;
			_frameDone = false;

			if (_memAccess)
			{
				_mamePeriodicComplete.WaitOne();
			}

			for (; _frameDone == false;)
			{
				_mameFrameComplete.WaitOne();
			}

			Frame++;

			if (IsLagFrame)
			{
				LagCount++;
			}

			return true;
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			_exiting = true;
			_mameThread.Join();
			_mameSaveBuffer = new byte[0];
			_hawkSaveBuffer = new byte[0];
		}
	}
}