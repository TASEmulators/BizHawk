using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari7800
{
	public partial class Atari7800 : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition { get; private set; }

		public void FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_frame++;

			if (controller.IsPressed("Power"))
			{
				// it seems that theMachine.Reset() doesn't clear ram, etc
				// this should leave hsram intact but clear most other things
				HardReset();
			}

			ControlAdapter.Convert(controller, _theMachine.InputState);
			_theMachine.ComputeNextFrame(_avProvider.Framebuffer);

			_islag = _theMachine.InputState.Lagged;

			if (_islag)
			{
				_lagcount++;
			}

			_avProvider.FillFrameBuffer();
		}

		public int Frame => _frame;

		public string SystemId => "A78"; // TODO 2600?

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
			if (_avProvider != null)
			{
				_avProvider.Dispose();
				_avProvider = null;
			}
		}
	}
}
