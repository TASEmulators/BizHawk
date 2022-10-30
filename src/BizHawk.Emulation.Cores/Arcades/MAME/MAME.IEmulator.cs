using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : IEmulator
	{
		public string SystemId => VSystemID.Raw.MAME;
		public bool DeterministicEmulation => true;
		public int Frame { get; private set; }
		public IEmulatorServiceProvider ServiceProvider { get; }
		public ControllerDefinition ControllerDefinition => MAMEController;

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			if (IsCrashed)
			{
				return false;
			}

			_controller = controller;

			// signal to mame we want to frame advance
			_mameCmd = MAME_CMD.STEP;
			SafeWaitEvent(_mameCommandComplete);

			// tell mame the next periodic callback will update video
			_mameCmd = MAME_CMD.VIDEO;
			_mameCommandWaitDone.Set();

			// wait until the mame thread is done updating video
			SafeWaitEvent(_mameCommandComplete);
			_mameCommandWaitDone.Set();

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
			_mameCmd = MAME_CMD.EXIT;
			_mameCommandWaitDone.Set();
			_mameThread.Join();
			_mameSaveBuffer = new byte[0];
			_hawkSaveBuffer = new byte[0];
		}
	}
}