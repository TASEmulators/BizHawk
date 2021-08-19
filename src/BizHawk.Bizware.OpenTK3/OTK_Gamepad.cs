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

		/// <summary>The things that use <see cref="GetAxes"/> (analog controls) appear to require values -10000.0..10000.0 rather than the -1.0..1.0 that OpenTK returns (although even then the results may be slightly outside of these bounds)</summary>
		private static float ConstrainFloatInput(float num)
		{
			if (num > 1) return 10000.0f;
			if (num < -1) return -10000.0f;
			return num * 10000.0f;
		}

		/// <summary>Signals whether OpenTK returned a GUID for this device</summary>
		private readonly bool _guidObtained;

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
			_guidObtained = guid is not null;

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
			InitializeMappings();
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

		public IEnumerable<(string AxisID, float Value)> GetAxes()
		{
			if (MappedGamePad)
			{
				// automapping identified - use OpenTKGamePad
				yield return ("LeftThumbX", ConstrainFloatInput(state.ThumbSticks.Left.X));
				yield return ("LeftThumbY", ConstrainFloatInput(state.ThumbSticks.Left.Y));
				yield return ("RightThumbX", ConstrainFloatInput(state.ThumbSticks.Right.X));
				yield return ("RightThumbY", ConstrainFloatInput(state.ThumbSticks.Right.Y));
				yield return ("LeftTrigger", ConstrainFloatInput(state.Triggers.Left));
				yield return ("RightTrigger", ConstrainFloatInput(state.Triggers.Right));
				yield break;
			}
			else
			{
				// use Joystick
				yield return ("X", ConstrainFloatInput(jState.GetAxis(0)));
				yield return ("Y", ConstrainFloatInput(jState.GetAxis(1)));
				yield return ("Z", ConstrainFloatInput(jState.GetAxis(2)));
				yield return ("W", ConstrainFloatInput(jState.GetAxis(3)));
				yield return ("V", ConstrainFloatInput(jState.GetAxis(4)));
				yield return ("S", ConstrainFloatInput(jState.GetAxis(5)));
				yield return ("Q", ConstrainFloatInput(jState.GetAxis(6)));
				yield return ("P", ConstrainFloatInput(jState.GetAxis(7)));
				yield return ("N", ConstrainFloatInput(jState.GetAxis(8)));

				for (var i = 9; i < 64; i++)
				{
					var j = i;
					yield return ($"Axis{j.ToString()}", ConstrainFloatInput(jState.GetAxis(j)));
				}

				yield break;
			}
		}

		/// <summary>Contains name and delegate function for all buttons, hats and axis</summary>
		public readonly List<ButtonObject> buttonObjects = new List<ButtonObject>();

		private void AddItem(string name, Func<bool> pressed) =>
			buttonObjects.Add(new ButtonObject
			{
				ButtonName = name,
				ButtonAction = pressed
			});

		public struct ButtonObject
		{
			public string ButtonName;
			public Func<bool> ButtonAction;
		}

		/// <summary>
		/// Setup mappings prior to button initialization.<br/>
		/// This is also here in case in the future we want users to be able to supply their own mappings for a device, perhaps via an input form. Possibly wishful thinking/overly complex.
		/// </summary>
		private void InitializeMappings()
		{
			if (_guidObtained)
			{
				// placeholder for if/when we figure out how to supply OpenTK with custom GamePadConfigurationDatabase entries
			}

			// currently OpenTK has an internal database of mappings for the GamePad class: https://github.com/opentk/opentk/blob/master/src/OpenTK/Input/GamePadConfigurationDatabase.cs
			if (MappedGamePad)
			{
				// internal map detected - use OpenTKGamePad
				InitializeGamePadControls();
			}
			else
			{
				// no internal map detected - use Joystick
				InitializeJoystickControls();
			}
		}

		private void InitializeJoystickControls()
		{
			// OpenTK's GetAxis returns float values (as opposed to the shorts of SlimDX)
			const float ConversionFactor = 1.0f / short.MaxValue;
			const float dzp = 20000 * ConversionFactor;
			const float dzn = -20000 * ConversionFactor;
			//const float dzt = 0.6f;

			// axis
			AddItem("X+", () => jState.GetAxis(0) >= dzp);
			AddItem("X-", () => jState.GetAxis(0) <= dzn);
			AddItem("Y+", () => jState.GetAxis(1) >= dzp);
			AddItem("Y-", () => jState.GetAxis(1) <= dzn);
			AddItem("Z+", () => jState.GetAxis(2) >= dzp);
			AddItem("Z-", () => jState.GetAxis(2) <= dzn);
			AddItem("W+", () => jState.GetAxis(3) >= dzp);
			AddItem("W-", () => jState.GetAxis(3) <= dzn);
			AddItem("V+", () => jState.GetAxis(4) >= dzp);
			AddItem("V-", () => jState.GetAxis(4) <= dzn);
			AddItem("S+", () => jState.GetAxis(5) >= dzp);
			AddItem("S-", () => jState.GetAxis(5) <= dzn);
			AddItem("Q+", () => jState.GetAxis(6) >= dzp);
			AddItem("Q-", () => jState.GetAxis(6) <= dzn);
			AddItem("P+", () => jState.GetAxis(7) >= dzp);
			AddItem("P-", () => jState.GetAxis(7) <= dzn);
			AddItem("N+", () => jState.GetAxis(8) >= dzp);
			AddItem("N-", () => jState.GetAxis(8) <= dzn);
			// should be enough axis, but just in case:
			for (var i = 9; i < 64; i++)
			{
				var j = i;
				AddItem($"Axis{j.ToString()}+", () => jState.GetAxis(j) >= dzp);
				AddItem($"Axis{j.ToString()}-", () => jState.GetAxis(j) <= dzn);
			}

			// buttons
			for (var i = 0; i < (_joystickCapabilities?.ButtonCount ?? 0); i++)
			{
				var j = i;
				AddItem($"B{i + 1}", () => jState.GetButton(j) == ButtonState.Pressed);
			}

			// hats
			AddItem("POV1U", () => jState.GetHat(JoystickHat.Hat0).IsUp);
			AddItem("POV1D", () => jState.GetHat(JoystickHat.Hat0).IsDown);
			AddItem("POV1L", () => jState.GetHat(JoystickHat.Hat0).IsLeft);
			AddItem("POV1R", () => jState.GetHat(JoystickHat.Hat0).IsRight);
			AddItem("POV2U", () => jState.GetHat(JoystickHat.Hat1).IsUp);
			AddItem("POV2D", () => jState.GetHat(JoystickHat.Hat1).IsDown);
			AddItem("POV2L", () => jState.GetHat(JoystickHat.Hat1).IsLeft);
			AddItem("POV2R", () => jState.GetHat(JoystickHat.Hat1).IsRight);
			AddItem("POV3U", () => jState.GetHat(JoystickHat.Hat2).IsUp);
			AddItem("POV3D", () => jState.GetHat(JoystickHat.Hat2).IsDown);
			AddItem("POV3L", () => jState.GetHat(JoystickHat.Hat2).IsLeft);
			AddItem("POV3R", () => jState.GetHat(JoystickHat.Hat2).IsRight);
			AddItem("POV4U", () => jState.GetHat(JoystickHat.Hat3).IsUp);
			AddItem("POV4D", () => jState.GetHat(JoystickHat.Hat3).IsDown);
			AddItem("POV4L", () => jState.GetHat(JoystickHat.Hat3).IsLeft);
			AddItem("POV4R", () => jState.GetHat(JoystickHat.Hat3).IsRight);
		}

		private void InitializeGamePadControls()
		{
			// OpenTK's ThumbSticks contain float values (as opposed to the shorts of SlimDX)
			const float ConversionFactor = 1.0f / short.MaxValue;
			const float dzp = 20000 * ConversionFactor;
			const float dzn = -20000 * ConversionFactor;
			const float dzt = 0.6f;

			// buttons
			AddItem("A", () => state.Buttons.A == ButtonState.Pressed);
			AddItem("B", () => state.Buttons.B == ButtonState.Pressed);
			AddItem("X", () => state.Buttons.X == ButtonState.Pressed);
			AddItem("Y", () => state.Buttons.Y == ButtonState.Pressed);
			AddItem("Guide", () => state.Buttons.BigButton == ButtonState.Pressed);
			AddItem("Start", () => state.Buttons.Start == ButtonState.Pressed);
			AddItem("Back", () => state.Buttons.Back == ButtonState.Pressed);
			AddItem("LeftThumb", () => state.Buttons.LeftStick == ButtonState.Pressed);
			AddItem("RightThumb", () => state.Buttons.RightStick == ButtonState.Pressed);
			AddItem("LeftShoulder", () => state.Buttons.LeftShoulder == ButtonState.Pressed);
			AddItem("RightShoulder", () => state.Buttons.RightShoulder == ButtonState.Pressed);

			// dpad
			AddItem("DpadUp", () => state.DPad.Up == ButtonState.Pressed);
			AddItem("DpadDown", () => state.DPad.Down == ButtonState.Pressed);
			AddItem("DpadLeft", () => state.DPad.Left == ButtonState.Pressed);
			AddItem("DpadRight", () => state.DPad.Right == ButtonState.Pressed);

			// sticks
			AddItem("LStickUp", () => state.ThumbSticks.Left.Y >= dzp);
			AddItem("LStickDown", () => state.ThumbSticks.Left.Y <= dzn);
			AddItem("LStickLeft", () => state.ThumbSticks.Left.X <= dzn);
			AddItem("LStickRight", () => state.ThumbSticks.Left.X >= dzp);
			AddItem("RStickUp", () => state.ThumbSticks.Right.Y >= dzp);
			AddItem("RStickDown", () => state.ThumbSticks.Right.Y <= dzn);
			AddItem("RStickLeft", () => state.ThumbSticks.Right.X <= dzn);
			AddItem("RStickRight", () => state.ThumbSticks.Right.X >= dzp);

			// triggers
			AddItem("LeftTrigger", () => state.Triggers.Left > dzt);
			AddItem("RightTrigger", () => state.Triggers.Right > dzt);
		}

		/// <remarks><paramref name="left"/> and <paramref name="right"/> are in 0..<see cref="int.MaxValue"/></remarks>
		public void SetVibration(int left, int right)
		{
			const double SCALE = 1.0 / int.MaxValue;
			static float Conv(int i) => (float) (i * SCALE);
			OpenTKGamePad.SetVibration(_deviceIndex, Conv(left), Conv(right));
		}
	}
}

