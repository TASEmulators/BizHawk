using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }
		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;
		public int Frame { get; private set; }
		public string SystemId => VSystemID.Raw.Doom;
		public bool DeterministicEmulation => true;
		private delegate int ReadPort(IController c);

		public bool FrameAdvance(IController controller, bool renderVideo, bool renderAudio)
		{
			// Declaring inputs
			LibDSDA.PackedPlayerInput[] players = [
				new LibDSDA.PackedPlayerInput(),
				new LibDSDA.PackedPlayerInput(),
				new LibDSDA.PackedPlayerInput(),
				new LibDSDA.PackedPlayerInput()
			];

			ReadPort[] buttonsReaders =
			[
				_controllerDeck.ReadButtons1,
				_controllerDeck.ReadButtons2,
				_controllerDeck.ReadButtons3,
				_controllerDeck.ReadButtons4,
			];

			int commonButtons = 0;

			int playersPresent = Convert.ToInt32(_syncSettings.Player1Present)
				| Convert.ToInt32(_syncSettings.Player2Present) << 1
				| Convert.ToInt32(_syncSettings.Player3Present) << 2
				| Convert.ToInt32(_syncSettings.Player4Present) << 3;

			if (controller.IsPressed("Change Gamma") && !_lastGammaInput)
			{
				// cycle through [0 - 4]
				_settings.Gamma++;
				_settings.Gamma %= 5;
			}

			if (controller.IsPressed("Automap Toggle"))      commonButtons |= (1 << 0);
			if (controller.IsPressed("Automap +"))           commonButtons |= (1 << 1);
			if (controller.IsPressed("Automap -"))           commonButtons |= (1 << 2);
			if (controller.IsPressed("Automap Full/Zoom"))   commonButtons |= (1 << 3);
			if (controller.IsPressed("Automap Follow"))      commonButtons |= (1 << 4);
			if (controller.IsPressed("Automap Up"))          commonButtons |= (1 << 5);
			if (controller.IsPressed("Automap Down"))        commonButtons |= (1 << 6);
			if (controller.IsPressed("Automap Right"))       commonButtons |= (1 << 7);
			if (controller.IsPressed("Automap Left"))        commonButtons |= (1 << 8);
			if (controller.IsPressed("Automap Grid"))        commonButtons |= (1 << 9);
			if (controller.IsPressed("Automap Mark"))        commonButtons |= (1 << 10);
			if (controller.IsPressed("Automap Clear Marks")) commonButtons |= (1 << 11);

			for (int i = 0; i < 4; i++)
			{
				if ((playersPresent & (1 << i)) is not 0)
				{
					int port = i + 1;
					bool strafe = controller.IsPressed($"P{port} Strafe");
					int speedIndex = Convert.ToInt32(controller.IsPressed($"P{port} Run")
						|| _syncSettings.AlwaysRun);

					int turnSpeed = 0;
					// lower speed for tapping turn buttons
					if (controller.IsPressed($"P{port} Turn Right") || controller.IsPressed($"P{port} Turn Left"))
					{
						_turnHeld[i]++;
						turnSpeed = _turnHeld[i] < 6 ? _turnSpeeds[2] : _turnSpeeds[speedIndex];
					}
					else
					{
						_turnHeld[i] = 0;
					}

					// initial axis read
					players[i].RunSpeed      = controller.AxisValue($"P{port} Run Speed");
					players[i].StrafingSpeed = controller.AxisValue($"P{port} Strafing Speed");
					players[i].WeaponSelect  = controller.AxisValue($"P{port} Weapon Select");
					// core counts angle counterclockwise
					players[i].TurningSpeed  = controller.AxisValue($"P{port} Turning Speed") << 8;

					if (_syncSettings.TurningResolution == TurningResolution.Longtics)
					{
						players[i].TurningSpeed += controller.AxisValue($"P{port} Turning Speed Frac.");
					}
					
					// override weapon axis based on buttons
					var weaponRange = controller.Definition.Axes[$"P{port} Weapon Select"].Range;
					for (var unit = weaponRange.Start; unit <= weaponRange.EndInclusive; unit++)
					{
						// if several weapon buttons are pressed, highest one overrides lower
						if (controller.IsPressed($"P{port} Weapon Select {unit}")) players[i].WeaponSelect = unit;
					}

					// override movement axis based on buttons (turning is reversed upstream)
					if (controller.IsPressed($"P{port} Forward"))      players[i].RunSpeed      =  _runSpeeds   [speedIndex];
					if (controller.IsPressed($"P{port} Backward"))     players[i].RunSpeed      = -_runSpeeds   [speedIndex];
					// turning with strafe button held will later be ADDED to these values (which is what makes strafe50 possible)
					if (controller.IsPressed($"P{port} Strafe Right")) players[i].StrafingSpeed =  _strafeSpeeds[speedIndex];
					if (controller.IsPressed($"P{port} Strafe Left"))  players[i].StrafingSpeed = -_strafeSpeeds[speedIndex];
					if (strafe)
					{
						if (controller.IsPressed($"P{port} Turn Right")) players[i].StrafingSpeed += _strafeSpeeds[speedIndex];
						if (controller.IsPressed($"P{port} Turn Left"))  players[i].StrafingSpeed -= _strafeSpeeds[speedIndex];
					}
					else
					{
						if (controller.IsPressed($"P{port} Turn Right")) players[i].TurningSpeed -= turnSpeed;
						if (controller.IsPressed($"P{port} Turn Left"))  players[i].TurningSpeed += turnSpeed;
					}

					// mouse-driven running
					// divider matches the core
					players[i].RunSpeed -= (int)(controller.AxisValue($"P{port} Mouse Running") * _syncSettings.MouseRunSensitivity / 8.0);
					players[i].RunSpeed = players[i].RunSpeed.Clamp<int>(-_runSpeeds[1], _runSpeeds[1]);

					// mouse-driven turning
					var mouseTurning = controller.AxisValue($"P{port} Mouse Turning") * _syncSettings.MouseTurnSensitivity;
					if (strafe)
					{
						players[i].StrafingSpeed += mouseTurning / 5;
					}
					else
					{
						players[i].TurningSpeed -= mouseTurning;
					}
					// ultimately strafe speed is limited to max run speed, NOT max strafe speed
					players[i].StrafingSpeed = players[i].StrafingSpeed.Clamp<int>(-_runSpeeds[1], _runSpeeds[1]);

					// for shorttics we expose to player and parse from movies only 1 byte, but the core internally works with 2 bytes
					if (_syncSettings.TurningResolution == TurningResolution.Shorttics)
					{
						int desiredAngleturn = players[i].TurningSpeed + _turnCarry;
						players[i].TurningSpeed = (desiredAngleturn + 128) & 0xff00;
						_turnCarry = desiredAngleturn - players[i].TurningSpeed;
						players[i].TurningSpeed = ((players[i].TurningSpeed + 128) >> 8) << 8;
					}

					if (_syncSettings.Strafe50Turns == Strafe50Turning.Ignore
						&& Math.Abs(players[i].StrafingSpeed) > _strafeSpeeds[1])
					{
						players[i].TurningSpeed = 0;
					}

					// bool buttons
					var actionsBitfield = buttonsReaders[i](controller);
					players[i].Buttons = actionsBitfield;

					// Raven Games
					if (_syncSettings.InputFormat is DoomControllerTypes.Heretic or DoomControllerTypes.Hexen)
					{
						players[i].FlyLook     = controller.AxisValue($"P{port} Fly / Look");
						players[i].ArtifactUse = controller.AxisValue($"P{port} Use Artifact");

						if (_syncSettings.InputFormat is DoomControllerTypes.Hexen)
						{
							players[i].Jump      = (actionsBitfield & 0b01000) >> 3;
							players[i].EndPlayer = (actionsBitfield & 0b10000) >> 4;
						}
					}
				}
			}

			LibDSDA.PackedRenderInfo renderInfo = new LibDSDA.PackedRenderInfo()
			{
				SfxVolume          = _settings.SfxVolume,
				MusicVolume        = _settings.MusicVolume,
				Gamma              = _settings.Gamma,
				HeadsUpMode        = (int)_settings.HeadsUpMode,
				MapDetails         = (int)_settings.MapDetails,
				MapOverlay         = (int)_settings.MapOverlay,
				RenderVideo        = renderVideo                  ? 1 : 0,
				RenderAudio        = renderAudio                  ? 1 : 0,
				ShowMessages       = _settings.ShowMessages       ? 1 : 0,
				ReportSecrets      = _settings.ReportSecrets      ? 1 : 0,
				DsdaExHud          = _settings.DsdaExHud          ? 1 : 0,
				DisplayCoordinates = _settings.DisplayCoordinates ? 1 : 0,
				DisplayCommands    = _settings.DisplayCommands    ? 1 : 0,
				MapTotals          = _settings.MapTotals          ? 1 : 0,
				MapTime            = _settings.MapTime            ? 1 : 0,
				MapCoordinates     = _settings.MapCoordinates     ? 1 : 0,
				PlayerPointOfView  = _settings.DisplayPlayer - 1,
			};

			IsLagFrame = _core.dsda_frame_advance(
				commonButtons,
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

			_lastGammaInput = controller.IsPressed("Change Gamma");

			return true;
		}

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
