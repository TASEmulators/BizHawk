using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;

			_islag = true;

			// Handle all the console controls here
			if (_controller.IsPressed("Power"))
			{
				HardReset();
			}

			if (_controller.IsPressed("Toggle Left Difficulty") && !_leftDifficultySwitchHeld)
			{
				_leftDifficultySwitchPressed ^= true;
				_leftDifficultySwitchHeld = true;
			}
			else if (!_controller.IsPressed("Toggle Left Difficulty"))
			{
				_leftDifficultySwitchHeld = false;
			}

			if (_controller.IsPressed("Toggle Right Difficulty") && !_rightDifficultySwitchHeld)
			{
				_rightDifficultySwitchPressed ^= true;
				_rightDifficultySwitchHeld = true;
			}
			else if (!_controller.IsPressed("Toggle Right Difficulty"))
			{
				_rightDifficultySwitchHeld = false;
			}

			unselect_reset = false;

			int count = 0;
			while (!_tia.New_Frame)
			{
				Cycle();
				count++;
				if (count > 1000000 && !SP_FRAME)
				{
					if (SP_RESET)
					{
						unselect_reset = true;
					}

					if (SP_SELECT)
					{
						unselect_select = true;
					}

					if (!SP_RESET && !SP_SELECT)
					{
						throw new Exception("ERROR: Unable to resolve Frame. Please Report.");
					}
				}
			}

			_tia.New_Frame = false;

			if (!renderSound)
			{
				_tia.AudioClocks = 0; // we need this here since the async sound provider won't check in this case
			}

			if (_islag)
			{
				_lagCount++;
			}

			_tia.LineCount = 0;

			_frame++;

			return true;
		}

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.A26;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_islag = false;
		}

		public void Dispose()
		{
		}
	}
}
