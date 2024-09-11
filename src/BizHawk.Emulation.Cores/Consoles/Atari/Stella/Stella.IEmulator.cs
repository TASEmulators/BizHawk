using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

        private bool _leftDifficultyToggled = false;
		private bool _rightDifficultyToggled = false;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			
			int port1 = _controllerDeck.ReadPort1(controller);
			int port2 = _controllerDeck.ReadPort2(controller);
            
			// Handle all the console controls here
			bool powerPressed = false;
			bool resetPressed = false;
			if (controller.IsPressed("Power")) powerPressed = true;
			if (controller.IsPressed("Reset")) resetPressed = true;
			if (controller.IsPressed("Toggle Left Difficulty"))	_leftDifficultyToggled = !_leftDifficultyToggled;
			if (controller.IsPressed("Toggle Right Difficulty")) _rightDifficultyToggled = !_rightDifficultyToggled;

			IsLagFrame = true;

			Core.stella_frame_advance(port1, port2, resetPressed, powerPressed, _leftDifficultyToggled, _rightDifficultyToggled);

			if (IsLagFrame)
				LagCount++;

			if (render)
				UpdateVideo();

			if (renderSound)
				update_audio();

			_frame++;

			return true;
		}

		public int _frame;

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.A26;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
		}
	}
}
