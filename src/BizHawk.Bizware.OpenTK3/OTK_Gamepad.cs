using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using BizHawk.Common;

using OpenTK.Input;

using OpenTKGamePad = OpenTK.Input.GamePad;

namespace BizHawk.Bizware.OpenTK3
{
	/// <summary>
	/// Modified OpenTK Gamepad Handler<br/>
	/// The jump from OpenTK 1.x to 3.x broke the original <see cref="Joystick">OpenTK.Input.Joystick</see> implementation, but we gain <see cref="OpenTKGamePad">OpenTK.Input.GamePad</see> support on Unix. However, the gamepad auto-mapping is a little suspect, so we use both methods.<br/>
	/// As a side-effect, it should make it easier to implement virtual→host haptics in the future.
	/// </summary>
	public class OTK_GamePad
	{
		/// <remarks>They don't have a way to query this for some reason. 4 is the minimum promised.</remarks>
		private const int MAX_GAMEPADS = 4;

		private static readonly object _syncObj = new object();

		private static readonly OTK_GamePad[] Devices = new OTK_GamePad[MAX_GAMEPADS];

		private static volatile bool initialized = false;

		public static void Initialize()
		{
			for (var i = 0; i < MAX_GAMEPADS; i++) OpenTKGamePad.GetState(i); // not sure if this is important to do at this time, but the processing which used to be done here included calls to OpenTK, so I left this no-op just in case --yoshi
			initialized = true;
		}

		public static IEnumerable<OTK_GamePad> EnumerateDevices()
		{
			if (!initialized) return Enumerable.Empty<OTK_GamePad>();
			lock (_syncObj) return Devices.Where(pad => pad is not null).ToList();
		}

		public static void UpdateAll()
		{
			static void DropAt(int index, IList<OTK_GamePad> devices)
			{
				var known = devices[index];
				devices[index] = null;
				Console.WriteLine(known is null ? $"Dropped gamepad X{index + 1}/J{index + 1}" : $"Dropped gamepad {known.InputNamePrefixShort}: was {known.MappingsDatabaseName}");
			}
			if (!initialized) return;
			lock (_syncObj)
			{
				for (var tryIndex = 0; tryIndex < MAX_GAMEPADS; tryIndex++)
				{
					var known = Devices[tryIndex];
					try
					{
						var isConnectedAtIndex = OpenTKGamePad.GetState(tryIndex).IsConnected || Joystick.GetState(tryIndex).IsConnected;
						if (known is not null)
						{
							if (isConnectedAtIndex) known.Update();
							else DropAt(tryIndex, Devices);
						}
						else
						{
							if (isConnectedAtIndex)
							{
								var newConn = Devices[tryIndex] = new(tryIndex);
								Console.WriteLine($"Connected new gamepad {newConn.InputNamePrefixShort}: {newConn.MappingsDatabaseName}");
							}
							// else was and remains disconnected, move along
						}
					}
					catch (Exception e)
					{
						Util.DebugWriteLine($"caught {e.GetType().FullName} while enumerating OpenTK gamepads");
						DropAt(tryIndex, Devices);
					}
				}
			}
		}

		/// <summary>The OpenTK device index</summary>
		private readonly int _deviceIndex;

		/// <summary>The object returned by <see cref="OpenTKGamePad.GetCapabilities"/></summary>
		private readonly GamePadCapabilities? _gamePadCapabilities;

		/// <summary>The object returned by <see cref="Joystick.GetCapabilities"/></summary>
		private readonly JoystickCapabilities? _joystickCapabilities;

		public readonly IReadOnlyCollection<string> HapticsChannels;

		/// <summary>For use in keybind boxes</summary>
		public readonly string InputNamePrefix;

		/// <summary>as <see cref="InputNamePrefix"/> but without the trailing space</summary>
		private readonly string InputNamePrefixShort;

		/// <summary>Public check on whether mapped gamepad config is being used</summary>
		public bool MappedGamePad => _gamePadCapabilities?.IsMapped == true;

		/// <summary>GUID from <see cref="Joystick"/> (also used for DB) and name from <see cref="OpenTKGamePad"/> via DB</summary>
		private readonly string MappingsDatabaseName;

		/// <summary>Gamepad Device state information - updated constantly</summary>
		private GamePadState state;

		/// <summary>Joystick Device state information - updated constantly</summary>
		private JoystickState jState;

		private OTK_GamePad(int deviceIndex)
		{
			_deviceIndex = deviceIndex;

			Guid? guid = null;
			if (Joystick.GetState(_deviceIndex).IsConnected)
			{
				guid = Joystick.GetGuid(_deviceIndex);
				_joystickCapabilities = Joystick.GetCapabilities(_deviceIndex);
			}

			string name;
			if (OpenTKGamePad.GetState(_deviceIndex).IsConnected)
			{
				name = OpenTKGamePad.GetName(_deviceIndex);
				_gamePadCapabilities = OpenTKGamePad.GetCapabilities(_deviceIndex);
			}
			else
			{
				name = "OTK GamePad Undetermined Name";
			}
			HapticsChannels = _gamePadCapabilities != null && _gamePadCapabilities.Value.HasLeftVibrationMotor && _gamePadCapabilities.Value.HasRightVibrationMotor
				? new[] { "Left", "Right" } // two haptic motors
				: new[] { "Mono" }; // one or zero haptic motors -- in the latter case, pretend it's mono anyway as that doesn't seem to cause problems
			InputNamePrefixShort = $"{(MappedGamePad ? "X" : "J")}{_deviceIndex + 1}";
			InputNamePrefix = $"{InputNamePrefixShort} ";
			MappingsDatabaseName = $"{guid ?? Guid.Empty} {name}";
			Update();

			// Setup mappings prior to button initialization.
			List<(string ButtonName, Func<bool> GetIsPressed)> buttonGetters = new();

			if (guid is not null)
			{
				// placeholder for if/when we figure out how to supply OpenTK with custom GamePadConfigurationDatabase entries
			}

			// currently OpenTK has an internal database of mappings for the GamePad class: https://github.com/opentk/opentk/blob/master/src/OpenTK/Input/GamePadConfigurationDatabase.cs
			if (MappedGamePad)
			{
				// internal map detected - use OpenTKGamePad

				// OpenTK's ThumbSticks contain float values (as opposed to the shorts of SlimDX)
				const float ConversionFactor = 1.0f / short.MaxValue;
				const float dzp = 20000 * ConversionFactor;
				const float dzn = -20000 * ConversionFactor;
				const float dzt = 0.6f;

				// buttons
				buttonGetters.Add(("A", () => state.Buttons.A == ButtonState.Pressed));
				buttonGetters.Add(("B", () => state.Buttons.B == ButtonState.Pressed));
				buttonGetters.Add(("X", () => state.Buttons.X == ButtonState.Pressed));
				buttonGetters.Add(("Y", () => state.Buttons.Y == ButtonState.Pressed));
				buttonGetters.Add(("Guide", () => state.Buttons.BigButton == ButtonState.Pressed));
				buttonGetters.Add(("Start", () => state.Buttons.Start == ButtonState.Pressed));
				buttonGetters.Add(("Back", () => state.Buttons.Back == ButtonState.Pressed));
				buttonGetters.Add(("LeftThumb", () => state.Buttons.LeftStick == ButtonState.Pressed));
				buttonGetters.Add(("RightThumb", () => state.Buttons.RightStick == ButtonState.Pressed));
				buttonGetters.Add(("LeftShoulder", () => state.Buttons.LeftShoulder == ButtonState.Pressed));
				buttonGetters.Add(("RightShoulder", () => state.Buttons.RightShoulder == ButtonState.Pressed));

				// dpad
				buttonGetters.Add(("DpadUp", () => state.DPad.Up == ButtonState.Pressed));
				buttonGetters.Add(("DpadDown", () => state.DPad.Down == ButtonState.Pressed));
				buttonGetters.Add(("DpadLeft", () => state.DPad.Left == ButtonState.Pressed));
				buttonGetters.Add(("DpadRight", () => state.DPad.Right == ButtonState.Pressed));

				// sticks
				buttonGetters.Add(("LStickUp", () => state.ThumbSticks.Left.Y >= dzp));
				buttonGetters.Add(("LStickDown", () => state.ThumbSticks.Left.Y <= dzn));
				buttonGetters.Add(("LStickLeft", () => state.ThumbSticks.Left.X <= dzn));
				buttonGetters.Add(("LStickRight", () => state.ThumbSticks.Left.X >= dzp));
				buttonGetters.Add(("RStickUp", () => state.ThumbSticks.Right.Y >= dzp));
				buttonGetters.Add(("RStickDown", () => state.ThumbSticks.Right.Y <= dzn));
				buttonGetters.Add(("RStickLeft", () => state.ThumbSticks.Right.X <= dzn));
				buttonGetters.Add(("RStickRight", () => state.ThumbSticks.Right.X >= dzp));

				// triggers
				buttonGetters.Add(("LeftTrigger", () => state.Triggers.Left > dzt));
				buttonGetters.Add(("RightTrigger", () => state.Triggers.Right > dzt));
			}
			else
			{
				// no internal map detected - use Joystick

				// OpenTK's GetAxis returns float values (as opposed to the shorts of SlimDX)
				const float ConversionFactor = 1.0f / short.MaxValue;
				const float dzp = 20000 * ConversionFactor;
				const float dzn = -20000 * ConversionFactor;
//				const float dzt = 0.6f;

				// axis
				buttonGetters.Add(("X+", () => jState.GetAxis(0) >= dzp));
				buttonGetters.Add(("X-", () => jState.GetAxis(0) <= dzn));
				buttonGetters.Add(("Y+", () => jState.GetAxis(1) >= dzp));
				buttonGetters.Add(("Y-", () => jState.GetAxis(1) <= dzn));
				buttonGetters.Add(("Z+", () => jState.GetAxis(2) >= dzp));
				buttonGetters.Add(("Z-", () => jState.GetAxis(2) <= dzn));
				buttonGetters.Add(("W+", () => jState.GetAxis(3) >= dzp));
				buttonGetters.Add(("W-", () => jState.GetAxis(3) <= dzn));
				buttonGetters.Add(("V+", () => jState.GetAxis(4) >= dzp));
				buttonGetters.Add(("V-", () => jState.GetAxis(4) <= dzn));
				buttonGetters.Add(("S+", () => jState.GetAxis(5) >= dzp));
				buttonGetters.Add(("S-", () => jState.GetAxis(5) <= dzn));
				buttonGetters.Add(("Q+", () => jState.GetAxis(6) >= dzp));
				buttonGetters.Add(("Q-", () => jState.GetAxis(6) <= dzn));
				buttonGetters.Add(("P+", () => jState.GetAxis(7) >= dzp));
				buttonGetters.Add(("P-", () => jState.GetAxis(7) <= dzn));
				buttonGetters.Add(("N+", () => jState.GetAxis(8) >= dzp));
				buttonGetters.Add(("N-", () => jState.GetAxis(8) <= dzn));
				// should be enough axes, but just in case:
				for (var i = 9; i < 64; i++)
				{
					var j = i;
					buttonGetters.Add(($"Axis{j.ToString()}+", () => jState.GetAxis(j) >= dzp));
					buttonGetters.Add(($"Axis{j.ToString()}-", () => jState.GetAxis(j) <= dzn));
				}

				// buttons
				for (int i = 0, l = _joystickCapabilities?.ButtonCount ?? 0; i < l; i++)
				{
					var j = i;
					buttonGetters.Add(($"B{i + 1}", () => jState.GetButton(j) == ButtonState.Pressed));
				}

				// hats
				buttonGetters.Add(("POV1U", () => jState.GetHat(JoystickHat.Hat0).IsUp));
				buttonGetters.Add(("POV1D", () => jState.GetHat(JoystickHat.Hat0).IsDown));
				buttonGetters.Add(("POV1L", () => jState.GetHat(JoystickHat.Hat0).IsLeft));
				buttonGetters.Add(("POV1R", () => jState.GetHat(JoystickHat.Hat0).IsRight));
				buttonGetters.Add(("POV2U", () => jState.GetHat(JoystickHat.Hat1).IsUp));
				buttonGetters.Add(("POV2D", () => jState.GetHat(JoystickHat.Hat1).IsDown));
				buttonGetters.Add(("POV2L", () => jState.GetHat(JoystickHat.Hat1).IsLeft));
				buttonGetters.Add(("POV2R", () => jState.GetHat(JoystickHat.Hat1).IsRight));
				buttonGetters.Add(("POV3U", () => jState.GetHat(JoystickHat.Hat2).IsUp));
				buttonGetters.Add(("POV3D", () => jState.GetHat(JoystickHat.Hat2).IsDown));
				buttonGetters.Add(("POV3L", () => jState.GetHat(JoystickHat.Hat2).IsLeft));
				buttonGetters.Add(("POV3R", () => jState.GetHat(JoystickHat.Hat2).IsRight));
				buttonGetters.Add(("POV4U", () => jState.GetHat(JoystickHat.Hat3).IsUp));
				buttonGetters.Add(("POV4D", () => jState.GetHat(JoystickHat.Hat3).IsDown));
				buttonGetters.Add(("POV4L", () => jState.GetHat(JoystickHat.Hat3).IsLeft));
				buttonGetters.Add(("POV4R", () => jState.GetHat(JoystickHat.Hat3).IsRight));
			}

			ButtonGetters = buttonGetters;
		}

		public void Update()
		{
			// update both here just in case
			var tmpState = OpenTKGamePad.GetState(_deviceIndex);
			DebugState(tmpState);
			state = tmpState;
			var tmpJstate = Joystick.GetState(_deviceIndex);
			DebugState(tmpJstate);
			jState = tmpJstate;
		}

		[Conditional("DEBUG")]
		private void DebugState(GamePadState tmpState)
		{
			if (!tmpState.Equals(state)) Console.WriteLine($"GamePad State:\t{tmpState}");
		}

		[Conditional("DEBUG")]
		private void DebugState(JoystickState tmpJstate)
		{
			if (!tmpJstate.Equals(jState)) Console.WriteLine($"Joystick State:\t{tmpJstate}");
		}

		public IReadOnlyCollection<(string AxisID, int Value)> GetAxes()
		{
			// The host input stack appears to require values -10000.0..10000.0 rather than the -1.0..1.0 that OpenTK returns (although even then the results may be slightly outside of these bounds)
			static int ConstrainFloatInput(float num) => num switch
			{
				> 1 => 10000,
				< -1 => -10000,
				_ => (int) (num * 10000.0f)
			};

			if (MappedGamePad)
			{
				// automapping identified - use OpenTKGamePad
				return new[]
				{
					("LeftThumbX", ConstrainFloatInput(state.ThumbSticks.Left.X)),
					("LeftThumbY", ConstrainFloatInput(state.ThumbSticks.Left.Y)),
					("RightThumbX", ConstrainFloatInput(state.ThumbSticks.Right.X)),
					("RightThumbY", ConstrainFloatInput(state.ThumbSticks.Right.Y)),
					("LeftTrigger", ConstrainFloatInput(state.Triggers.Left)),
					("RightTrigger", ConstrainFloatInput(state.Triggers.Right)),
				};
			}

			// else use Joystick
			List<(string AxisID, int Value)> values = new()
			{
				("X", ConstrainFloatInput(jState.GetAxis(0))),
				("Y", ConstrainFloatInput(jState.GetAxis(1))),
				("Z", ConstrainFloatInput(jState.GetAxis(2))),
				("W", ConstrainFloatInput(jState.GetAxis(3))),
				("V", ConstrainFloatInput(jState.GetAxis(4))),
				("S", ConstrainFloatInput(jState.GetAxis(5))),
				("Q", ConstrainFloatInput(jState.GetAxis(6))),
				("P", ConstrainFloatInput(jState.GetAxis(7))),
				("N", ConstrainFloatInput(jState.GetAxis(8))),
			};

			for (var i = 9; i < 64; i++)
			{
				var j = i;
				values.Add(($"Axis{j.ToString()}", ConstrainFloatInput(jState.GetAxis(j))));
			}

			return values;
		}

		/// <summary>Contains name and delegate function for all buttons, hats and axis</summary>
		public readonly IReadOnlyCollection<(string ButtonName, Func<bool> GetIsPressed)> ButtonGetters;

		/// <remarks><paramref name="left"/> and <paramref name="right"/> are in 0..<see cref="int.MaxValue"/></remarks>
		public void SetVibration(int left, int right)
		{
			const double SCALE = 1.0 / int.MaxValue;
			static float Conv(int i) => (float) (i * SCALE);
			OpenTKGamePad.SetVibration(_deviceIndex, Conv(left), Conv(right));
		}
	}
}

