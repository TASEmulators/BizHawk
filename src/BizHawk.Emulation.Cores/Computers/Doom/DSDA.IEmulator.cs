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
					players[i].RunSpeed = potReaders[i](controller, 0);
					players[i].StrafingSpeed = potReaders[i](controller, 1);
					players[i].TurningSpeed = potReaders[i](controller, 2);
					players[i].WeaponSelect = potReaders[i](controller, 3);

					// override axis based on movement buttons (turning is reversed upstream)
					if (controller.IsPressed($"P{i + 1} Forward")) players[i].RunSpeed = _runSpeeds[speedIndex];
					if (controller.IsPressed($"P{i + 1} Backward")) players[i].RunSpeed = -_runSpeeds[speedIndex];
					if (controller.IsPressed($"P{i + 1} Strafe Right")) players[i].StrafingSpeed = _strafeSpeeds[speedIndex];
					if (controller.IsPressed($"P{i + 1} Strafe Left")) players[i].StrafingSpeed = -_strafeSpeeds[speedIndex];
					if (controller.IsPressed($"P{i + 1} Turn Right")) players[i].TurningSpeed = -turnSpeed;
					if (controller.IsPressed($"P{i + 1} Turn Left")) players[i].TurningSpeed = turnSpeed;

					// mouse-driven running
					// divider matches the core
					players[i].RunSpeed -= (int)(potReaders[i](controller, 4) * _syncSettings.MouseRunSensitivity / 8.0);
					players[i].RunSpeed = players[i].RunSpeed.Clamp<int>(-_runSpeeds[1], _runSpeeds[1]);

					// mouse-driven turning
					// divider recalibrates minimal mouse movement to be 1 (requires global setting)
					players[i].TurningSpeed -= (int)(potReaders[i](controller, 5) * _syncSettings.MouseTurnSensitivity / 272.0);
					if (_syncSettings.TurningResolution == TurningResolution.Shorttics)
					{
						// calc matches the core
						players[i].TurningSpeed = ((players[i].TurningSpeed << 8) + 128) >> 8;
					}

					// bool buttons
					var actionsBitfield = portReaders[i](controller);
					players[i].Fire = actionsBitfield & 0b00001;
					players[i].Action = (actionsBitfield & 0b00010) >> 1;
					players[i].Automap = (actionsBitfield & 0b00100) >> 2;

					// Raven Games
					if (_syncSettings.InputFormat is DoomControllerTypes.Heretic or DoomControllerTypes.Hexen)
					{
						players[i].FlyLook = potReaders[i](controller, 6);
						players[i].ArtifactUse = potReaders[i](controller, 7);
						if (_syncSettings.InputFormat is DoomControllerTypes.Hexen)
						{
							players[i].Jump = (actionsBitfield & 0b01000) >> 3;
							players[i].EndPlayer = (actionsBitfield & 0b10000) >> 4;
						}
					}
				}
			}

			PackedRenderInfo renderInfo = new PackedRenderInfo();
			renderInfo.RenderVideo = renderVideo ? 1 : 0;
			renderInfo.RenderAudio = renderAudio ? 1 : 0;
			renderInfo.PlayerPointOfView = _settings.DisplayPlayer - 1;

			IsLagFrame = _core.dsda_frame_advance(
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

			if (IsLagFrame)
			{
				LagCount++;
			}

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
