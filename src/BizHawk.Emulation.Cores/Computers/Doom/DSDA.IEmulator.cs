using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		private bool _leftDifficultyToggled;
		private bool _rightDifficultyToggled;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			int port1 = _controllerDeck.ReadPort1(controller);
			//int port2 = _controllerDeck.ReadPort2(controller);
            
			// Handle all the console controls here
			bool powerPressed = false;
			bool resetPressed = false;
			if (controller.IsPressed("Power")) powerPressed = true;
			if (controller.IsPressed("Reset")) resetPressed = true;
			if (controller.IsPressed("Toggle Left Difficulty"))	_leftDifficultyToggled = !_leftDifficultyToggled;
			if (controller.IsPressed("Toggle Right Difficulty")) _rightDifficultyToggled = !_rightDifficultyToggled;

			Core.dsda_frame_advance();

			if (render)
				UpdateVideo();

			if (renderSound)
				UpdateAudio();

			Frame++;

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.Doom;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			Frame = 0;
		}

		public void Dispose()
		{
			_elf.Dispose();
			DisposeSound();
		}
	}
}
