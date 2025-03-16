using BizHawk.Common.NumberExtensions;
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
					int speedIndex = Convert.ToInt32(controller.IsPressed($"P{i+1} Run")
						|| _syncSettings.AlwaysRun);

					int turnSpeed = 0;
					// lower speed for tapping turn buttons
					if (controller.IsPressed($"P{i + 1} Turn Right") || controller.IsPressed($"P{i + 1} Turn Left"))
					{
						_turnHeld[i]++;
						turnSpeed = _turnHeld[i] < 6 ? _turnSpeeds[2] : _turnSpeeds[speedIndex];
					}
					else
					{
						_turnHeld[i] = 0;
					}

					// initial axis read
					players[i]._RunSpeed = potReaders[i](controller, 0);
					players[i]._StrafingSpeed = potReaders[i](controller, 1);
					players[i]._TurningSpeed = potReaders[i](controller, 2);
					players[i]._WeaponSelect = potReaders[i](controller, 3);

					// override axis based on movement buttons (turning is reversed upstream)
					if (controller.IsPressed($"P{i+1} Forward")) players[i]._RunSpeed = _runSpeeds[speedIndex];
					if (controller.IsPressed($"P{i+1} Backward")) players[i]._RunSpeed = -_runSpeeds[speedIndex];
					if (controller.IsPressed($"P{i+1} Strafe Right")) players[i]._StrafingSpeed = _strafeSpeeds[speedIndex];
					if (controller.IsPressed($"P{i+1} Strafe Left")) players[i]._StrafingSpeed = -_strafeSpeeds[speedIndex];
					if (controller.IsPressed($"P{i + 1} Turn Right")) players[i]._TurningSpeed = -turnSpeed;
					if (controller.IsPressed($"P{i + 1} Turn Left")) players[i]._TurningSpeed = turnSpeed;

					// mouse-driven running
					// divider matches the core
					players[i]._RunSpeed -= (int)(potReaders[i](controller, 4) * _syncSettings.MouseRunSensitivity / 8.0);
					players[i]._RunSpeed = players[i]._RunSpeed.Clamp<int>(-_runSpeeds[1], _runSpeeds[1]);

					// mouse-driven turning
					// divider recalibrates minimal mouse movement to be 1 (requires global setting)
					players[i]._TurningSpeed -= (int)(potReaders[i](controller, 5) * _syncSettings.MouseTurnSensitivity / 272.0);
					if (_syncSettings.TurningResolution == TurningResolution.Shorttics)
					{
						// calc matches the core
						players[i]._TurningSpeed = ((players[i]._TurningSpeed << 8) + 128) >> 8;
					}

					// bool buttons
					var actionsBitfield = portReaders[i](controller);
					players[i]._Fire = actionsBitfield & 0b00001;
					players[i]._Action = (actionsBitfield & 0b00010) >> 1;
					players[i]._Automap = (actionsBitfield & 0b00100) >> 2;

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

			_core.dsda_frame_advance(
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
