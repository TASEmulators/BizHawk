using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }
		public ControllerDefinition ControllerDefinition { get; private set; }
		public int Frame { get; private set; }
		public string SystemId => VSystemID.Raw.Doom;
		public bool DeterministicEmulation => true;
		private delegate int ReadPort(IController c);

		public bool FrameAdvance(IController controller, bool renderVideo, bool renderAudio)
		{
			// Declaring inputs
			LibDSDA.PackedPlayerInput camera = new();
			LibDSDA.PackedPlayerInput[] players = [
				new LibDSDA.PackedPlayerInput(),
				new LibDSDA.PackedPlayerInput(),
				new LibDSDA.PackedPlayerInput(),
				new LibDSDA.PackedPlayerInput()
			];

			int automapButtons = 0;

			// this is the only change that we're announcing on the front end.
			// not announcing it at all feels weird given how vanilla and ports do it.
			// but announcing it via internal messages can show wrong values,
			// because gamma is detached from savestates, yet the internal on-screen message isn't.
			// so you can be at gamma 3, switch to 4, get the message, then load a state that
			// was saved when the gamma 2 message was visible, and it'd now be saying 2,
			// even tho it'd actually stay at 4 after state load.
			// announcing live settings changes only makes sense if they happen on hotkey
			// and not from the dialog where you're deliberately changing them.
			// so if keys for other settings are added, they can be announced too.
			if (controller.IsPressed("Change Gamma") && !_lastGammaInput)
			{
				// cycle through [0 - 4]
				_settings.Gamma++;
				_settings.Gamma %= 5;
				Comm.Notify("Gamma correction " +
					(_settings.Gamma == 0 ? "OFF" : "level " + _settings.Gamma),
					4); // internal messages last 4 seconds
			}

			if (controller.IsPressed("Automap Toggle"))      automapButtons |= (1 << 0);
			if (controller.IsPressed("Automap +"))           automapButtons |= (1 << 1);
			if (controller.IsPressed("Automap -"))           automapButtons |= (1 << 2);
			if (controller.IsPressed("Automap Full/Zoom"))   automapButtons |= (1 << 3);
			if (controller.IsPressed("Automap Follow"))      automapButtons |= (1 << 4);
			if (controller.IsPressed("Automap Up"))          automapButtons |= (1 << 5);
			if (controller.IsPressed("Automap Down"))        automapButtons |= (1 << 6);
			if (controller.IsPressed("Automap Right"))       automapButtons |= (1 << 7);
			if (controller.IsPressed("Automap Left"))        automapButtons |= (1 << 8);
			if (controller.IsPressed("Automap Grid"))        automapButtons |= (1 << 9);
			if (controller.IsPressed("Automap Mark"))        automapButtons |= (1 << 10);
			if (controller.IsPressed("Automap Clear Marks")) automapButtons |= (1 << 11);


			camera.WeaponSelect  = controller.AxisValue($"Camera Mode");
			camera.RunSpeed      = controller.AxisValue($"Camera Run Speed");
			camera.StrafingSpeed = controller.AxisValue($"Camera Strafing Speed");
			camera.TurningSpeed  = controller.AxisValue($"Camera Turning Speed") << 8;
			camera.FlyLook       = controller.AxisValue($"Camera Fly");

			if (controller.IsPressed($"Camera Reset"))
				camera.Buttons |= LibDSDA.Buttons.Fire;

			for (int i = 0; i < 4; i++)
			{
				var port = i + 1;
				if (PlayerPresent(_syncSettings, port))
				{
					players[i].Buttons = LibDSDA.Buttons.None;

					bool strafe = controller.IsPressed($"P{port} Strafe");
					int speedIndex = Convert.ToInt32(controller.IsPressed($"P{port} Run")
						|| _syncSettings.AlwaysRun);

					int turnSpeed = 0;
					// lower speed for tapping turn buttons
					if (controller.IsPressed($"P{port} Turn Right")
						|| controller.IsPressed($"P{port} Turn Left"))
					{
						_turnHeld[i]++;
						turnSpeed = _turnHeld[i] < 6
							? _turnSpeeds[2]
							: _turnSpeeds[speedIndex];
					}
					else
					{
						_turnHeld[i] = 0;
					}

					// initial axis read
					players[i].RunSpeed      = controller.AxisValue($"P{port} Run Speed");
					players[i].StrafingSpeed = controller.AxisValue($"P{port} Strafing Speed");
					players[i].WeaponSelect  = controller.AxisValue($"P{port} Weapon Select");
					if (players[i].WeaponSelect > 0)
					{
						players[i].Buttons = LibDSDA.Buttons.ChangeWeapon;
					}
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
						// if several weapon buttons are pressed, higher overrides lower
						if (controller.IsPressed($"P{port} Weapon Select {unit}"))
						{
							players[i].WeaponSelect = unit;
							players[i].Buttons |= LibDSDA.Buttons.ChangeWeapon;
						}
					}

					// override movement axis based on buttons
					// turning is reversed upstream
					if (controller.IsPressed($"P{port} Forward"))
						players[i].RunSpeed = _runSpeeds[speedIndex];

					if (controller.IsPressed($"P{port} Backward"))
						players[i].RunSpeed = -_runSpeeds[speedIndex];

					// turning with strafe button held will later be ADDED to these values
					// which is what makes strafe50 possible
					if (controller.IsPressed($"P{port} Strafe Right"))
						players[i].StrafingSpeed = _strafeSpeeds[speedIndex];

					if (controller.IsPressed($"P{port} Strafe Left"))
						players[i].StrafingSpeed = -_strafeSpeeds[speedIndex];

					if (strafe)
					{
						if (controller.IsPressed($"P{port} Turn Right"))
							players[i].StrafingSpeed += _strafeSpeeds[speedIndex];

						if (controller.IsPressed($"P{port} Turn Left"))
							players[i].StrafingSpeed -= _strafeSpeeds[speedIndex];
					}
					else
					{
						if (controller.IsPressed($"P{port} Turn Right"))
							players[i].TurningSpeed -= turnSpeed;

						if (controller.IsPressed($"P{port} Turn Left"))
							players[i].TurningSpeed += turnSpeed;
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
					players[i].StrafingSpeed = players[i].StrafingSpeed
						.Clamp<int>(-_runSpeeds[1], _runSpeeds[1]);

					// for shorttics we expose to player and parse from movies only 1 byte
					// but the core internally works with 2 bytes
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
					if (controller.IsPressed($"P{port} Fire"))
						players[i].Buttons |= LibDSDA.Buttons.Fire;

					if (controller.IsPressed($"P{port} Use"))
						players[i].Buttons |= LibDSDA.Buttons.Use;

					// Raven Games
					if (_syncSettings.InputFormat is not ControllerType.Doom)
					{
						players[i].FlyLook     = controller.AxisValue($"P{port} Fly / Look");
						players[i].ArtifactUse = controller.AxisValue($"P{port} Use Artifact");

						// these "buttons" are not part of ticcmd_t::buttons
						// we just use their free bits
						if (controller.IsPressed($"P{port} Inventory Left"))
							players[i].Buttons |= LibDSDA.Buttons.InventoryLeft;

						if (controller.IsPressed($"P{port} Inventory Right"))
							players[i].Buttons |= LibDSDA.Buttons.InventoryRight;

						if (controller.IsPressed($"P{port} Use Artifact"))
							players[i].Buttons |= LibDSDA.Buttons.ArtifactUse;

						if (controller.IsPressed($"P{port} Look Up"))
							players[i].Buttons |= LibDSDA.Buttons.LookUp;

						if (controller.IsPressed($"P{port} Look Down"))
							players[i].Buttons |= LibDSDA.Buttons.LookDown;

						if (controller.IsPressed($"P{port} Look Center"))
							players[i].Buttons |= LibDSDA.Buttons.LookCenter;

						if (controller.IsPressed($"P{port} Fly Up"))
							players[i].Buttons |= LibDSDA.Buttons.FlyUp;

						if (controller.IsPressed($"P{port} Fly Down"))
							players[i].Buttons |= LibDSDA.Buttons.FlyDown;

						if (controller.IsPressed($"P{port} Fly Center"))
							players[i].Buttons |= LibDSDA.Buttons.FlyCenter;

						if (_syncSettings.InputFormat is ControllerType.Hexen)
						{
							if (controller.IsPressed($"P{port} Jump"))
								players[i].ArtifactUse |= (int)LibDSDA.Buttons.Jump;

							if (controller.IsPressed($"P{port} End Player"))
								players[i].ArtifactUse |= (int)LibDSDA.Buttons.EndPlayer;
						}
					}
				}
			}

			var renderInfo = new LibDSDA.PackedRenderInfo()
			{
				SfxVolume          = _settings.SfxVolume,
				MusicVolume        = _settings.MusicVolume,
				Gamma              = _settings.Gamma,
				HeadsUpMode        = (int)_settings.HeadsUpMode,
				MapDetails         = (int)_settings.MapDetails,
				MapOverlay         = (int)_settings.MapOverlay,
				RenderVideo        = Convert.ToInt32(renderVideo),
				RenderAudio        = Convert.ToInt32(renderAudio),
				ShowMessages       = Convert.ToInt32(_settings.ShowMessages),
				ReportSecrets      = Convert.ToInt32(_settings.ReportSecrets),
				DsdaExHud          = Convert.ToInt32(_settings.DsdaExHud),
				DisplayCoordinates = Convert.ToInt32(_settings.DisplayCoordinates),
				DisplayCommands    = Convert.ToInt32(_settings.DisplayCommands),
				MapTotals          = Convert.ToInt32(_settings.MapTotals),
				MapTime            = Convert.ToInt32(_settings.MapTime),
				MapCoordinates     = Convert.ToInt32(_settings.MapCoordinates),
				PlayerPointOfView  = _settings.DisplayPlayer - 1,
			};

			IsLagFrame = _core.dsda_frame_advance(
				automapButtons,
				players,
				ref camera,
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
