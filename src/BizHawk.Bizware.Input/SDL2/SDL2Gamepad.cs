using System.Collections.Generic;

using static SDL2.SDL;

namespace BizHawk.Bizware.Input
{
	/// <summary>
	/// SDL2 Gamepad Handler
	/// </summary>
	internal class SDL2Gamepad : IDisposable
	{
		// indexed by instance id
		private static readonly Dictionary<int, SDL2Gamepad> Gamepads = new();

		private readonly IntPtr Opaque;

		/// <summary>Is an SDL_GameController rather than an SDL_Joystick</summary>
		public readonly bool IsGameController;

		/// <summary>Has rumble</summary>
		public readonly bool HasRumble;

		/// <summary>Contains name and delegate function for all buttons, hats and axis</summary>
		public readonly IReadOnlyCollection<(string ButtonName, Func<bool> GetIsPressed)> ButtonGetters;
		
		/// <summary>For use in keybind boxes</summary>
		public string InputNamePrefix { get; private set; }

		/// <summary>Device index in SDL</summary>
		public int DeviceIndex { get; private set; }

		/// <summary>Instance ID in SDL</summary>
		public int InstanceID { get; }

		/// <summary>Device name in SDL</summary>
		public string DeviceName { get; }

		public static void Deinitialize()
		{
			foreach (var gamepad in Gamepads.Values)
			{
				gamepad.Dispose();
			}

			Gamepads.Clear();
		}

		public void Dispose()
		{
			Console.WriteLine($"Disconnecting SDL gamepad, device index {DeviceIndex}, instance ID {InstanceID}, name {DeviceName}");

			if (IsGameController)
			{
				SDL_GameControllerClose(Opaque);
			}
			else
			{
				SDL_JoystickClose(Opaque);
			}
		}

		private static void RefreshIndexes()
		{
			var njoysticks = SDL_NumJoysticks();
			for (var i = 0; i < njoysticks; i++)
			{
				var joystickId = SDL_JoystickGetDeviceInstanceID(i);
				if (Gamepads.TryGetValue(joystickId, out var gamepad))
				{
					gamepad.UpdateIndex(i);
				}
			}
		}

		public static void AddDevice(int deviceIndex)
		{
			var instanceId = SDL_JoystickGetDeviceInstanceID(deviceIndex);
			if (!Gamepads.ContainsKey(instanceId))
			{
				var gamepad = new SDL2Gamepad(deviceIndex);
				Gamepads.Add(gamepad.InstanceID, gamepad);
			}
			else
			{
				Console.WriteLine($"Gamepads contained a joystick with instance ID {instanceId}, ignoring add device event");
			}

			RefreshIndexes();
		}

		public static void RemoveDevice(int deviceInstanceId)
		{
			if (Gamepads.TryGetValue(deviceInstanceId, out var gamepad))
			{
				gamepad.Dispose();
				Gamepads.Remove(deviceInstanceId);
			}
			else
			{
				Console.WriteLine($"Gamepads did not contain a joystick with instance ID {deviceInstanceId}, ignoring remove device event");
			}

			RefreshIndexes();
		}

		public static IEnumerable<SDL2Gamepad> EnumerateDevices()
			=> Gamepads.Values;

		private List<(string ButtonName, Func<bool> GetIsPressed)> CreateGameControllerButtonGetters()
		{
			List<(string ButtonName, Func<bool> GetIsPressed)> buttonGetters = [ ];

			const int dzp = (int)(32768 / 2.5);
			const int dzn = (int)(-32768 / 2.5);
			const int dzt = (int)(32768 / 6.5);

			// buttons
			buttonGetters.Add(("A", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A) == 1));
			buttonGetters.Add(("B", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B) == 1));
			buttonGetters.Add(("X", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X) == 1));
			buttonGetters.Add(("Y", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y) == 1));
			buttonGetters.Add(("Back", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK) == 1));
			buttonGetters.Add(("Guide", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE) == 1));
			buttonGetters.Add(("Start", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START) == 1));
			buttonGetters.Add(("LeftThumb", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK) == 1));
			buttonGetters.Add(("RightThumb", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK) == 1));
			buttonGetters.Add(("LeftShoulder", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER) == 1));
			buttonGetters.Add(("RightShoulder", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER) == 1));
			buttonGetters.Add(("DpadUp", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP) == 1));
			buttonGetters.Add(("DpadDown", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN) == 1));
			buttonGetters.Add(("DpadLeft", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT) == 1));
			buttonGetters.Add(("DpadRight", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT) == 1));
			buttonGetters.Add(("Misc", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_MISC1) == 1));
			buttonGetters.Add(("Paddle1", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE1) == 1));
			buttonGetters.Add(("Paddle2", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE2) == 1));
			buttonGetters.Add(("Paddle3", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE3) == 1));
			buttonGetters.Add(("Paddle4", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE4) == 1));
			buttonGetters.Add(("Touchpad", () => SDL_GameControllerGetButton(Opaque, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_TOUCHPAD) == 1));

			// note: SDL has flipped meaning for the Y axis compared to DirectInput/XInput (-/+ for u/d instead of +/- for u/d)

			// sticks
			buttonGetters.Add(("LStickUp", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) <= dzn));
			buttonGetters.Add(("LStickDown", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) >= dzp));
			buttonGetters.Add(("LStickLeft", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) <= dzn));
			buttonGetters.Add(("LStickRight", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) >= dzp));
			buttonGetters.Add(("RStickUp", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) <= dzn));
			buttonGetters.Add(("RStickDown", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) >= dzp));
			buttonGetters.Add(("RStickLeft", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) <= dzn));
			buttonGetters.Add(("RStickRight", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) >= dzp));

			// triggers
			buttonGetters.Add(("LeftTrigger", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT) > dzt));
			buttonGetters.Add(("RightTrigger", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT) > dzt));

			return buttonGetters;
		}

		private List<(string ButtonName, Func<bool> GetIsPressed)> CreateJoystickButtonGetters()
		{
			List<(string ButtonName, Func<bool> GetIsPressed)> buttonGetters = new();

			const float dzp = (int)(32768 / 2.5);
			const float dzn = (int)(-32768 / 2.5);

			// axes
			buttonGetters.Add(("X+", () => SDL_JoystickGetAxis(Opaque, 0) >= dzp));
			buttonGetters.Add(("X-", () => SDL_JoystickGetAxis(Opaque, 0) <= dzn));
			buttonGetters.Add(("Y+", () => SDL_JoystickGetAxis(Opaque, 1) >= dzp));
			buttonGetters.Add(("Y-", () => SDL_JoystickGetAxis(Opaque, 1) <= dzn));
			buttonGetters.Add(("Z+", () => SDL_JoystickGetAxis(Opaque, 2) >= dzp));
			buttonGetters.Add(("Z-", () => SDL_JoystickGetAxis(Opaque, 2) <= dzn));
			buttonGetters.Add(("W+", () => SDL_JoystickGetAxis(Opaque, 3) >= dzp));
			buttonGetters.Add(("W-", () => SDL_JoystickGetAxis(Opaque, 3) <= dzn));
			buttonGetters.Add(("V+", () => SDL_JoystickGetAxis(Opaque, 4) >= dzp));
			buttonGetters.Add(("V-", () => SDL_JoystickGetAxis(Opaque, 4) <= dzn));
			buttonGetters.Add(("S+", () => SDL_JoystickGetAxis(Opaque, 5) >= dzp));
			buttonGetters.Add(("S-", () => SDL_JoystickGetAxis(Opaque, 5) <= dzn));
			buttonGetters.Add(("Q+", () => SDL_JoystickGetAxis(Opaque, 6) >= dzp));
			buttonGetters.Add(("Q-", () => SDL_JoystickGetAxis(Opaque, 6) <= dzn));
			buttonGetters.Add(("P+", () => SDL_JoystickGetAxis(Opaque, 7) >= dzp));
			buttonGetters.Add(("P-", () => SDL_JoystickGetAxis(Opaque, 7) <= dzn));
			buttonGetters.Add(("N+", () => SDL_JoystickGetAxis(Opaque, 8) >= dzp));
			buttonGetters.Add(("N-", () => SDL_JoystickGetAxis(Opaque, 8) <= dzn));
			var naxes = SDL_JoystickNumAxes(Opaque);
			for (var i = 9; i < naxes; i++)
			{
				var j = i;
				buttonGetters.Add(($"Axis{j}+", () => SDL_JoystickGetAxis(Opaque, j) >= dzp));
				buttonGetters.Add(($"Axis{j}-", () => SDL_JoystickGetAxis(Opaque, j) <= dzn));
			}

			// buttons
			var nbuttons = SDL_JoystickNumButtons(Opaque);
			for (var i = 0; i < nbuttons; i++)
			{
				var j = i;
				buttonGetters.Add(($"B{i + 1}", () => SDL_JoystickGetButton(Opaque, j) == 1));
			}

			// hats
			var nhats = SDL_JoystickNumHats(Opaque);
			for (var i = 0; i < nhats; i++)
			{
				var j = i;
				buttonGetters.Add(($"POV{j}U", () => (SDL_JoystickGetHat(Opaque, j) & SDL_HAT_UP) == SDL_HAT_UP));
				buttonGetters.Add(($"POV{j}D", () => (SDL_JoystickGetHat(Opaque, j) & SDL_HAT_DOWN) == SDL_HAT_DOWN));
				buttonGetters.Add(($"POV{j}L", () => (SDL_JoystickGetHat(Opaque, j) & SDL_HAT_LEFT) == SDL_HAT_LEFT));
				buttonGetters.Add(($"POV{j}R", () => (SDL_JoystickGetHat(Opaque, j) & SDL_HAT_RIGHT) == SDL_HAT_RIGHT));
			}

			return buttonGetters;
		}

		public void UpdateIndex(int index)
		{
			InputNamePrefix = IsGameController
				? $"X{index + 1} "
				: $"J{index + 1} ";
			DeviceIndex = index;
		}

		private SDL2Gamepad(int index)
		{
			if (SDL_IsGameController(index) == SDL_bool.SDL_TRUE)
			{
				Opaque = SDL_GameControllerOpen(index);
				HasRumble = SDL_GameControllerHasRumble(Opaque) == SDL_bool.SDL_TRUE;
				ButtonGetters = CreateGameControllerButtonGetters();
				IsGameController = true;
				InputNamePrefix = $"X{index + 1} ";
				DeviceName = SDL_GameControllerName(Opaque);
			}
			else
			{
				Opaque = SDL_JoystickOpen(index);
				HasRumble = SDL_JoystickHasRumble(Opaque) == SDL_bool.SDL_TRUE;
				ButtonGetters = CreateJoystickButtonGetters();
				IsGameController = false;
				InputNamePrefix = $"J{index + 1} ";
				DeviceName = SDL_JoystickName(Opaque);
			}

			DeviceIndex = index;
			InstanceID = SDL_JoystickGetDeviceInstanceID(index);

			Console.WriteLine($"Connected SDL gamepad, device index {index}, instance ID {InstanceID}, name {DeviceName}");
		}

		public IEnumerable<(string AxisID, int Value)> GetAxes()
		{
			//constant for adapting a +/- 32768 range to a +/-10000-based range
			const float f = 32768 / 10000.0f;
			static int Conv(short num) => (int)(num / f);

			// note: SDL has flipped meaning for the Y axis compared to DirectInput/XInput (-/+ for u/d instead of +/- for u/d)

			if (IsGameController)
			{
				return new[]
				{
					("LeftThumbX", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX))),
					("LeftThumbY", -Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY))),
					("RightThumbX", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX))),
					("RightThumbY", -Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY))),
					("LeftTrigger", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT))),
					("RightTrigger", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT))),
				};
			}

			List<(string AxisID, int Value)> values = new()
			{
				("X", Conv(SDL_JoystickGetAxis(Opaque, 0))),
				("Y", Conv(SDL_JoystickGetAxis(Opaque, 1))),
				("Z", Conv(SDL_JoystickGetAxis(Opaque, 2))),
				("W", Conv(SDL_JoystickGetAxis(Opaque, 3))),
				("V", Conv(SDL_JoystickGetAxis(Opaque, 4))),
				("S", Conv(SDL_JoystickGetAxis(Opaque, 5))),
				("Q", Conv(SDL_JoystickGetAxis(Opaque, 6))),
				("P", Conv(SDL_JoystickGetAxis(Opaque, 7))),
				("N", Conv(SDL_JoystickGetAxis(Opaque, 8))),
			};

			var naxes = SDL_JoystickNumAxes(Opaque);
			for (var i = 9; i < naxes; i++)
			{
				var j = i;
				values.Add(($"Axis{j}", Conv(SDL_JoystickGetAxis(Opaque, j))));
			}

			return values;
		}

		/// <remarks><paramref name="left"/> and <paramref name="right"/> are in 0..<see cref="int.MaxValue"/></remarks>
		public void SetVibration(int left, int right)
		{
			static ushort Conv(int i) => unchecked((ushort) ((i >> 15) & 0xFFFF));
			_ = IsGameController
				? SDL_GameControllerRumble(Opaque, Conv(left), Conv(right), uint.MaxValue)
				: SDL_JoystickRumble(Opaque, Conv(left), Conv(right), uint.MaxValue);
		}
	}
}

