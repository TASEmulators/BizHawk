using System;
using System.Collections.Generic;
using System.Linq;

using static SDL2.SDL;

namespace BizHawk.Bizware.Input
{
	/// <summary>
	/// SDL2 Game Controller Handler
	/// </summary>
	internal class SDL2GameController
	{
		private static readonly List<SDL2GameController> GameControllers = new();

		private readonly IntPtr GameController;

		/// <summary>Device unique instance Id</summary>
		private readonly int InstanceId;

		/// <summary>For use in keybind boxes</summary>
		public readonly string InputNamePrefix;

		/// <summary>Has rumble</summary>
		public readonly bool HasRumble;

		/// <summary>Contains name and delegate function for all buttons, hats and axis</summary>
		public readonly IReadOnlyCollection<(string ButtonName, Func<bool> GetIsPressed)> ButtonGetters;

		public static void Initialize()
		{
			var njoysticks = SDL_NumJoysticks();
			for (var i = 0; i < njoysticks; i++)
			{
				if (SDL_IsGameController(i) == SDL_bool.SDL_TRUE)
				{
					GameControllers.Add(new(i, GameControllers.Count));
				}
			}
		}

		public static void Deinitialize()
		{
			foreach (var controller in GameControllers)
			{
				SDL_GameControllerClose(controller.GameController);
			}

			GameControllers.Clear();
		}

		public static void Refresh()
		{
			var changed = false;

			var njoysticks = SDL_NumJoysticks();
			var ngamecontrollers = 0;
			for (var i = 0; i < njoysticks; i++)
			{
				if (SDL_IsGameController(i) == SDL_bool.SDL_TRUE)
				{
					if (GameControllers.Count == ngamecontrollers)
					{
						changed = true;
						break;
					}

					if (SDL_JoystickGetDeviceInstanceID(i) != GameControllers[ngamecontrollers].InstanceId)
					{
						changed = true;
						break;
					}

					ngamecontrollers++;
				}
			}

			changed |= GameControllers.Count != ngamecontrollers;

			if (changed)
			{
				Deinitialize();
				Initialize();
			}
		}

		public static IEnumerable<SDL2GameController> EnumerateDevices()
			=> GameControllers.Where(controller => SDL_GameControllerGetAttached(controller.GameController) == SDL_bool.SDL_TRUE).ToList();

		private SDL2GameController(int sdlIndex, int inputIndex)
		{
			GameController = SDL_GameControllerOpen(sdlIndex);
			InstanceId = SDL_JoystickGetDeviceInstanceID(sdlIndex);
			HasRumble = SDL_GameControllerHasRumble(GameController) == SDL_bool.SDL_TRUE;

			InputNamePrefix = $"X{inputIndex + 1} ";

			// Setup mappings prior to button initialization.
			List<(string ButtonName, Func<bool> GetIsPressed)> buttonGetters = new();

			const int dzp = 20000;
			const int dzn = -20000;
			const int dzt = 5000;

			// buttons
			buttonGetters.Add(("A", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A) == 1));
			buttonGetters.Add(("B", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B) == 1));
			buttonGetters.Add(("X", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X) == 1));
			buttonGetters.Add(("Y", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y) == 1));
			buttonGetters.Add(("Back", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK) == 1));
			buttonGetters.Add(("Guide", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE) == 1));
			buttonGetters.Add(("Start", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START) == 1));
			buttonGetters.Add(("LeftThumb", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK) == 1));
			buttonGetters.Add(("RightThumb", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK) == 1));
			buttonGetters.Add(("LeftShoulder", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER) == 1));
			buttonGetters.Add(("RightShoulder", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER) == 1));
			buttonGetters.Add(("DpadUp", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP) == 1));
			buttonGetters.Add(("DpadDown", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN) == 1));
			buttonGetters.Add(("DpadLeft", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT) == 1));
			buttonGetters.Add(("DpadRight", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT) == 1));
			buttonGetters.Add(("Misc", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_MISC1) == 1));
			buttonGetters.Add(("Paddle1", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE1) == 1));
			buttonGetters.Add(("Paddle2", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE2) == 1));
			buttonGetters.Add(("Paddle3", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE3) == 1));
			buttonGetters.Add(("Paddle4", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE4) == 1));
			buttonGetters.Add(("Touchpad", () => SDL_GameControllerGetButton(GameController, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_TOUCHPAD) == 1));

			// sticks
			buttonGetters.Add(("LStickUp", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) >= dzp));
			buttonGetters.Add(("LStickDown", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) <= dzn));
			buttonGetters.Add(("LStickLeft", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) <= dzn));
			buttonGetters.Add(("LStickRight", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) >= dzp));
			buttonGetters.Add(("RStickUp", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) >= dzp));
			buttonGetters.Add(("RStickDown", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) <= dzn));
			buttonGetters.Add(("RStickLeft", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) <= dzn));
			buttonGetters.Add(("RStickRight", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) >= dzp));

			// triggers
			buttonGetters.Add(("LeftTrigger", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT) > dzt));
			buttonGetters.Add(("RightTrigger", () => SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT) > dzt));

			ButtonGetters = buttonGetters;
		}

		public IEnumerable<(string AxisID, int Value)> GetAxes()
		{
			//constant for adapting a +/- 32768 range to a +/-10000-based range
			const float f = 32768 / 10000.0f;
			static int Conv(short num) => (int)(num / f);

			return new[]
			{
				("LeftThumbX", Conv(SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX))),
				("LeftThumbY", Conv(SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY))),
				("RightThumbX", Conv(SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX))),
				("RightThumbY", Conv(SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY))),
				("LeftTrigger", Conv(SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT))),
				("RightTrigger", Conv(SDL_GameControllerGetAxis(GameController, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT))),
			};
		}

		/// <remarks><paramref name="left"/> and <paramref name="right"/> are in 0..<see cref="int.MaxValue"/></remarks>
		public void SetVibration(int left, int right)
		{
			static ushort Conv(int i) => unchecked((ushort) ((i >> 15) & 0xFFFF));
			_ = SDL_GameControllerRumble(GameController, Conv(left), Conv(right), uint.MaxValue);
		}
	}
}

