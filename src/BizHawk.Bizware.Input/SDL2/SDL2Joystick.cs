using System;
using System.Collections.Generic;
using System.Linq;

using static SDL2.SDL;

namespace BizHawk.Bizware.Input
{
	/// <summary>
	/// SDL2 Joystick Handler
	/// TODO: This probably should just be merged with SDL2Gamepad in retrospect
	/// </summary>
	public class SDL2Joystick
	{
		private static readonly List<SDL2Joystick> Joysticks = new();

		private readonly IntPtr Joystick;

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
			
			for (int i = 0, j = 0; i < njoysticks; i++)
			{
				if (SDL_IsGameController(i) == SDL_bool.SDL_FALSE)
				{
					Joysticks.Add(new(i, j++));
				}
			}
		}

		public static void Deinitialize()
		{
			foreach (var stick in Joysticks)
			{
				SDL_JoystickClose(stick.Joystick);
			}

			Joysticks.Clear();
		}

		public static void Refresh()
		{
			var changed = false;

			var njoysticks = SDL_NumJoysticks();
			var nnotgamecontrollers = 0;
			for (var i = 0; i < njoysticks; i++)
			{
				if (SDL_IsGameController(i) == SDL_bool.SDL_FALSE)
				{
					if (Joysticks.Count == nnotgamecontrollers)
					{
						changed = true;
						break;
					}

					if (SDL_JoystickGetDeviceInstanceID(i) != Joysticks[nnotgamecontrollers].InstanceId)
					{
						changed = true;
						break;
					}

					nnotgamecontrollers++;
				}
			}

			changed |= Joysticks.Count != nnotgamecontrollers;

			if (changed)
			{
				Deinitialize();
				Initialize();
			}
		}

		public static IEnumerable<SDL2Joystick> EnumerateDevices()
			=> Joysticks.Where(stick => SDL_JoystickGetAttached(stick.Joystick) == SDL_bool.SDL_TRUE).ToList();

		private SDL2Joystick(int sdlIndex, int inputIndex)
		{
			Joystick = SDL_JoystickOpen(sdlIndex);
			InstanceId = SDL_JoystickGetDeviceInstanceID(sdlIndex);
			HasRumble = SDL_JoystickHasRumble(Joystick) == SDL_bool.SDL_TRUE;

			InputNamePrefix = $"J{inputIndex + 1} ";

			// Setup mappings prior to button initialization.
			List<(string ButtonName, Func<bool> GetIsPressed)> buttonGetters = new();

			const float dzp = 20000;
			const float dzn = -20000;

			// axes
			buttonGetters.Add(("X+", () => SDL_JoystickGetAxis(Joystick, 0) >= dzp));
			buttonGetters.Add(("X-", () => SDL_JoystickGetAxis(Joystick, 0) <= dzn));
			buttonGetters.Add(("Y+", () => SDL_JoystickGetAxis(Joystick, 1) >= dzp));
			buttonGetters.Add(("Y-", () => SDL_JoystickGetAxis(Joystick, 1) <= dzn));
			buttonGetters.Add(("Z+", () => SDL_JoystickGetAxis(Joystick, 2) >= dzp));
			buttonGetters.Add(("Z-", () => SDL_JoystickGetAxis(Joystick, 2) <= dzn));
			buttonGetters.Add(("W+", () => SDL_JoystickGetAxis(Joystick, 3) >= dzp));
			buttonGetters.Add(("W-", () => SDL_JoystickGetAxis(Joystick, 3) <= dzn));
			buttonGetters.Add(("V+", () => SDL_JoystickGetAxis(Joystick, 4) >= dzp));
			buttonGetters.Add(("V-", () => SDL_JoystickGetAxis(Joystick, 4) <= dzn));
			buttonGetters.Add(("S+", () => SDL_JoystickGetAxis(Joystick, 5) >= dzp));
			buttonGetters.Add(("S-", () => SDL_JoystickGetAxis(Joystick, 5) <= dzn));
			buttonGetters.Add(("Q+", () => SDL_JoystickGetAxis(Joystick, 6) >= dzp));
			buttonGetters.Add(("Q-", () => SDL_JoystickGetAxis(Joystick, 6) <= dzn));
			buttonGetters.Add(("P+", () => SDL_JoystickGetAxis(Joystick, 7) >= dzp));
			buttonGetters.Add(("P-", () => SDL_JoystickGetAxis(Joystick, 7) <= dzn));
			buttonGetters.Add(("N+", () => SDL_JoystickGetAxis(Joystick, 8) >= dzp));
			buttonGetters.Add(("N-", () => SDL_JoystickGetAxis(Joystick, 8) <= dzn));
			var naxes = SDL_JoystickNumAxes(Joystick);
			for (var i = 9; i < naxes; i++)
			{
				var j = i;
				buttonGetters.Add(($"Axis{j.ToString()}+", () => SDL_JoystickGetAxis(Joystick, j) >= dzp));
				buttonGetters.Add(($"Axis{j.ToString()}-", () => SDL_JoystickGetAxis(Joystick, j) <= dzn));
			}

			// buttons
			var nbuttons = SDL_JoystickNumButtons(Joystick);
			for (var i = 0; i < nbuttons; i++)
			{
				var j = i;
				buttonGetters.Add(($"B{i + 1}", () => SDL_JoystickGetButton(Joystick, j) == 1));
			}

			// hats
			var nhats = SDL_JoystickNumHats(Joystick);
			for (var i = 0; i < nhats; i++)
			{
				var j = i;
				buttonGetters.Add(($"POV{j.ToString()}U", () => (SDL_JoystickGetHat(Joystick, j) & SDL_HAT_UP) == SDL_HAT_UP));
				buttonGetters.Add(($"POV{j.ToString()}D", () => (SDL_JoystickGetHat(Joystick, j) & SDL_HAT_DOWN) == SDL_HAT_DOWN));
				buttonGetters.Add(($"POV{j.ToString()}L", () => (SDL_JoystickGetHat(Joystick, j) & SDL_HAT_LEFT) == SDL_HAT_LEFT));
				buttonGetters.Add(($"POV{j.ToString()}R", () => (SDL_JoystickGetHat(Joystick, j) & SDL_HAT_RIGHT) == SDL_HAT_RIGHT));
			}

			ButtonGetters = buttonGetters;
		}

		public IEnumerable<(string AxisID, int Value)> GetAxes()
		{
			//constant for adapting a +/- 32768 range to a +/-10000-based range
			const float f = 32768 / 10000.0f;
			static int Conv(short num) => (int)(num / f);


			List<(string AxisID, int Value)> values = new()
			{
				("X", Conv(SDL_JoystickGetAxis(Joystick, 0))),
				("Y", Conv(SDL_JoystickGetAxis(Joystick, 1))),
				("Z", Conv(SDL_JoystickGetAxis(Joystick, 2))),
				("W", Conv(SDL_JoystickGetAxis(Joystick, 3))),
				("V", Conv(SDL_JoystickGetAxis(Joystick, 4))),
				("S", Conv(SDL_JoystickGetAxis(Joystick, 5))),
				("Q", Conv(SDL_JoystickGetAxis(Joystick, 6))),
				("P", Conv(SDL_JoystickGetAxis(Joystick, 7))),
				("N", Conv(SDL_JoystickGetAxis(Joystick, 8))),
			};

			var naxes = SDL_JoystickNumAxes(Joystick);
			for (var i = 9; i < naxes; i++)
			{
				var j = i;
				values.Add(($"Axis{j.ToString()}", Conv(SDL_JoystickGetAxis(Joystick, j))));
			}

			return values;
		}

		/// <remarks><paramref name="left"/> and <paramref name="right"/> are in 0..<see cref="int.MaxValue"/></remarks>
		public void SetVibration(int left, int right)
		{
			static ushort Conv(int i) => unchecked((ushort) ((i >> 15) & 0xFFFF));
			_ = SDL_JoystickRumble(Joystick, Conv(left), Conv(right), uint.MaxValue);
		}
	}
}

