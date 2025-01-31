using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Computers.Doom.CInterface;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		private bool _leftDifficultyToggled;
		private bool _rightDifficultyToggled;

		public bool FrameAdvance(IController controller, bool renderVideo, bool renderSound)
		{
			// Declaring inputs
			PackedPlayerInput player1Inputs = new PackedPlayerInput();
			PackedPlayerInput player2Inputs = new PackedPlayerInput();
			PackedPlayerInput player3Inputs = new PackedPlayerInput();
			PackedPlayerInput player4Inputs = new PackedPlayerInput();

			if (_syncSettings.Player1Present)
			{
				player1Inputs._RunSpeed      = _controllerDeck.ReadPot1(controller, 0);
				player1Inputs._StrafingSpeed = _controllerDeck.ReadPot1(controller, 1);
				player1Inputs._TurningSpeed  = _controllerDeck.ReadPot1(controller, 2);
				player1Inputs._WeaponSelect  = _controllerDeck.ReadPot1(controller, 3);
				player1Inputs._Fire          = (_controllerDeck.ReadPort1(controller) & 0b001) > 0 ? 1 : 0;
				player1Inputs._Action        = (_controllerDeck.ReadPort1(controller) & 0b010) > 0 ? 1 : 0;
				player1Inputs._AltWeapon     = (_controllerDeck.ReadPort1(controller) & 0b100) > 0 ? 1 : 0;
			}

			Core.dsda_frame_advance(player1Inputs, player2Inputs, player3Inputs, player4Inputs, renderVideo ? 1 : 0, renderSound ? 1 : 0);

			if (renderVideo)
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
