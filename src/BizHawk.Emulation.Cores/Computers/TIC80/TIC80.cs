using System.Collections.Generic;
using System.Collections.ObjectModel;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.TIC80
{
	[PortedCore(CoreNames.TIC80, "nesbox", "v1.0.2164", "https://tic80.com/")]
	public sealed partial class TIC80 : WaterboxCore
	{
		private readonly LibTIC80 _core;

		[CoreConstructor(VSystemID.Raw.TIC80)]
		public TIC80(CoreLoadParameters<TIC80Settings, TIC80SyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth = 240,
				DefaultHeight = 136,
				MaxWidth = 256,
				MaxHeight = 144,
				MaxSamples = 1024,
				DefaultFpsNumerator = 60,
				DefaultFpsDenominator = 1,
				SystemId = VSystemID.Raw.TIC80,
			})
		{
			_settings = lp.Settings ?? new();
			_syncSettings = lp.SyncSettings ?? new();

			if (!_settings.Crop)
			{
				BufferWidth = 256;
				BufferHeight = 144;
			}

			_core = PreInit<LibTIC80>(new WaterboxOptions
			{
				Filename = "tic80.wbx",
				SbrkHeapSizeKB = 64 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 64 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			var rom = lp.Roms[0].FileData;
			var inputsActive = new[]
			{
				_syncSettings.Gamepad1,
				_syncSettings.Gamepad2,
				_syncSettings.Gamepad3,
				_syncSettings.Gamepad4,
				_syncSettings.Mouse,
				_syncSettings.Keyboard,
			};

			if (!_core.Init(rom, rom.Length, inputsActive))
			{
				throw new InvalidOperationException("Init returned false!");
			}

			// note: InputsActive is mutated in Init call
			// any items not available for the game will be set to false
			ControllerDefinition = CreateControllerDefinition(inputsActive);
			InputsActive = Array.AsReadOnly(inputsActive);
			PostInit();

			DeterministicEmulation = lp.DeterministicEmulationRequested || !_syncSettings.UseRealTime;
			InitializeRtc(_syncSettings.InitialTime);
		}

		public readonly ReadOnlyCollection<bool> InputsActive;

		private static readonly IReadOnlyCollection<KeyValuePair<string, LibTIC80.TIC80Keys>> KeyMap = MakeKeyMap();

		private static IReadOnlyCollection<KeyValuePair<string, LibTIC80.TIC80Keys>> MakeKeyMap()
		{
			var enumValues = Enum.GetValues(typeof(LibTIC80.TIC80Keys));
			var ret = new KeyValuePair<string, LibTIC80.TIC80Keys>[enumValues.Length - 1];
			for (var i = 0; i < ret.Length; i++)
			{
				var val = enumValues.GetValue(i + 1);
				var name = Enum.GetName(typeof(LibTIC80.TIC80Keys), val)!.TrimStart('_').Replace('_', ' ');
				ret[i] = new(name, (LibTIC80.TIC80Keys)val);
			}

			return Array.AsReadOnly(ret);
		}

		private static ControllerDefinition CreateControllerDefinition(bool[] inputsActive)
		{
			var ret = new ControllerDefinition("TIC-80 Controller");

			for (var i = 0; i < 4; i++)
			{
				if (inputsActive[i])
				{
					foreach (var b in Enum.GetValues(typeof(LibTIC80.TIC80Gamepad)))
					{
						ret.BoolButtons.Add($"P{i + 1} {Enum.GetName(typeof(LibTIC80.TIC80Gamepad), b)}");
					}
				}
			}

			if (inputsActive[4])
			{
				ret.AddXYPair("Mouse Position {0}", AxisPairOrientation.RightAndDown, (-128).RangeTo(127), 0);
				ret.BoolButtons.Add("Mouse Left Click");
				ret.BoolButtons.Add("Mouse Middle Click");
				ret.BoolButtons.Add("Mouse Right Click");
				ret.AddXYPair("Mouse Scroll {0}", AxisPairOrientation.RightAndUp, (-32).RangeTo(31), 0);

				foreach (var n in ret.BoolButtons)
				{
					if (n.StartsWithOrdinal("Mouse"))
					{
						ret.CategoryLabels[n] = "Mouse";
					}
				}

				foreach (var n in ret.Axes.Keys)
				{
					if (n.StartsWithOrdinal("Mouse"))
					{
						ret.CategoryLabels[n] = "Mouse";
					}
				}
			}

			if (inputsActive[5])
			{
				foreach (var k in Enum.GetValues(typeof(LibTIC80.TIC80Keys)))
				{
					var name = Enum.GetName(typeof(LibTIC80.TIC80Keys), k)!.TrimStart('_').Replace('_', ' ');
					if (name is "Unknown") continue;
					ret.BoolButtons.Add(name);
					ret.CategoryLabels[name] = "Keyboard";
				}
			}

			ret.BoolButtons.Add("Reset");

			return ret.MakeImmutable();
		}

		private static void GetGamepads(IController controller, ref LibTIC80.TIC80Inputs inputs)
		{
			var gamepads = new LibTIC80.TIC80Gamepad[4];
			for (var i = 0; i < 4; i++)
			{
				gamepads[i] = 0;
				foreach (var b in Enum.GetValues(typeof(LibTIC80.TIC80Gamepad)))
				{
					if (controller.IsPressed($"P{i + 1} {Enum.GetName(typeof(LibTIC80.TIC80Gamepad), b)}"))
					{
						gamepads[i] |= (LibTIC80.TIC80Gamepad)b;
					}
				}
			}

			inputs.P1Gamepad = gamepads[0];
			inputs.P2Gamepad = gamepads[1];
			inputs.P3Gamepad = gamepads[2];
			inputs.P4Gamepad = gamepads[3];
		}

		private static ushort GetMouseButtons(IController controller)
		{
			ushort ret = 0;

			if (controller.IsPressed("Mouse Left Click"))
			{
				ret |= 0x0001;
			}

			if (controller.IsPressed("Mouse Middle Click"))
			{
				ret |= 0x0002;
			}

			if (controller.IsPressed("Mouse Right Click"))
			{
				ret |= 0x0004;
			}

			var x = (ushort)((sbyte)controller.AxisValue("Mouse Scroll X") + 32);
			ret |= (ushort)(x << 3);

			var y = (ushort)((sbyte)controller.AxisValue("Mouse Scroll Y") + 32);
			ret |= (ushort)(y << (3 + 6));

			return ret;
		}

		private static void GetKeys(IController controller, ref LibTIC80.TIC80Inputs inputs)
		{
			var keys = new LibTIC80.TIC80Keys[4];
			var i = 0;
			foreach (var kvp in KeyMap)
			{
				if (controller.IsPressed(kvp.Key))
				{
					keys[i++] = kvp.Value;
					if (i == keys.Length)
					{
						break;
					}
				}
			}

			inputs.Key1 = keys[0];
			inputs.Key2 = keys[1];
			inputs.Key3 = keys[2];
			inputs.Key4 = keys[3];
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var inputs = new LibTIC80.TIC80Inputs
			{
				MouseX = (byte)(sbyte)controller.AxisValue("Mouse Position X"),
				MouseY = (byte)(sbyte)controller.AxisValue("Mouse Position Y"),
				MouseButtons = GetMouseButtons(controller),
			};

			// a reset will unset relative mode, but we won't know that until the reset actually happens
			if (_core.IsMouseRelative() && !controller.IsPressed("Reset"))
			{
				inputs.MouseButtons |= 0x8000;
			}
			else
			{
				// convert (-128, 127) to (0, 255)
				inputs.MouseX += 128;
				inputs.MouseY += 128;

				// mouse Y is supposed to be contrained to 0-143 (i.e. screen height range)
				// mouse X has the full range regardless (since screen width is 256)
				inputs.MouseY = (byte)(inputs.MouseY * 143 / 255);
			}

			GetGamepads(controller, ref inputs);
			GetKeys(controller, ref inputs);
			_core.SetInputs(ref inputs);

			return new LibTIC80.FrameInfo
			{
				Time = GetRtcTime(!DeterministicEmulation),
				Crop = _settings.Crop,
				Reset = controller.IsPressed("Reset"),
			};
		}
	}
}
