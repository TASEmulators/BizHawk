using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Computers.Doom.CInterface;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		private delegate int ReadPot(IController c, int pot);
		private delegate byte ReadPort(IController c);

		public bool FrameAdvance(IController controller, bool renderVideo, bool renderAudio)
		{
			// Declaring inputs
			PackedPlayerInput[] players = [
				new PackedPlayerInput(),
				new PackedPlayerInput(),
				new PackedPlayerInput(),
				new PackedPlayerInput()
			];

			ReadPot[] potReaders =
			[
				_controllerDeck.ReadPot1,
				_controllerDeck.ReadPot2,
				_controllerDeck.ReadPot3,
				_controllerDeck.ReadPot4,
			];

			ReadPort[] portReaders =
			[
				_controllerDeck.ReadPort1,
				_controllerDeck.ReadPort2,
				_controllerDeck.ReadPort3,
				_controllerDeck.ReadPort4,
			];

			int playersPresent = Convert.ToInt32(_syncSettings.Player1Present)
				| Convert.ToInt32(_syncSettings.Player2Present) << 1
				| Convert.ToInt32(_syncSettings.Player3Present) << 2
				| Convert.ToInt32(_syncSettings.Player4Present) << 3;

			for (int i = 0; i < 4; i++)
			{
				if ((playersPresent & (1 << i)) is not 0)
				{
					players[i]._RunSpeed = potReaders[i](controller, 0);
					players[i]._StrafingSpeed = potReaders[i](controller, 1);
					players[i]._TurningSpeed = potReaders[i](controller, 2);
					players[i]._WeaponSelect = potReaders[i](controller, 3);

					var actionsBitfield = portReaders[i](controller);
					players[i]._Fire = actionsBitfield & 0b00001;
					players[i]._Action = (actionsBitfield & 0b00010) >> 1;
					players[i]._AltWeapon = (actionsBitfield & 0b00100) >> 2;

					// Handling mouse-driven running
					players[i]._RunSpeed -= (int)((float)potReaders[i](controller, 4) * (float)_syncSettings.MouseRunSensitivity / 6.0);
					if (players[i]._RunSpeed > 50) players[i]._RunSpeed = 50;
					if (players[i]._RunSpeed < -50) players[i]._RunSpeed = -50;

					// Handling mouse-driven turning
					players[i]._TurningSpeed -= (int)((float)potReaders[i](controller, 5) * (float)_syncSettings.MouseTurnSensitivity / 300.0);
					if (_syncSettings.TurningResolution == TurningResolution.Shorttics)
					{
						players[i]._TurningSpeed >>= 8;
					}

					// Raven Games
					if (_syncSettings.InputFormat is DoomControllerTypes.Heretic or DoomControllerTypes.Hexen)
					{
						players[i]._FlyLook = potReaders[i](controller, 6);
						players[i]._ArtifactUse = potReaders[i](controller, 7);
						if (_syncSettings.InputFormat is DoomControllerTypes.Hexen)
						{
							players[i]._Jump = (actionsBitfield & 0b01000) >> 3;
							players[i]._EndPlayer = (actionsBitfield & 0b10000) >> 4;
						}
					}
				}
			}

			PackedRenderInfo renderInfo = new PackedRenderInfo();
			renderInfo._RenderVideo = renderVideo ? 1 : 0;
			renderInfo._RenderAudio = renderAudio ? 1 : 0;
			renderInfo._PlayerPointOfView = _settings.DisplayPlayer - 1;

			Core.dsda_frame_advance(
				ref players[0],
				ref players[1],
				ref players[2],
				ref players[3],
				ref renderInfo);

			if (renderVideo)
				UpdateVideo();

			if (renderAudio)
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
		}
	}
}
