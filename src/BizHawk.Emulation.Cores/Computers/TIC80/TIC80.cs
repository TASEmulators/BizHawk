using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.TIC80
{
	[PortedCore(CoreNames.TIC80, "nesbox", "v1.0.2164", "https://tic80.com/", isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), })]
	public partial class TIC80 : WaterboxCore
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
				SbrkHeapSizeKB = 2 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 64 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			var rom = lp.Roms[0].FileData;

			if (!_core.Init(rom, rom.Length))
			{
				throw new InvalidOperationException("Init returned false!");
			}

			PostInit();

			DeterministicEmulation = lp.DeterministicEmulationRequested || (!_syncSettings.UseRealTime);
			InitializeRtc(_syncSettings.InitialTime);
		}

		private static readonly List<KeyValuePair<string, LibTIC80.TIC80Keys>> KeyMap = new();

		public override ControllerDefinition ControllerDefinition => TIC80Controller;

		private static readonly ControllerDefinition TIC80Controller = CreateControllerDefinition();

		private static ControllerDefinition CreateControllerDefinition()
		{
			var ret = new ControllerDefinition("TIC-80 Controller");

			for (int i = 0; i < 4; i++)
			{
				foreach (var b in Enum.GetValues(typeof(LibTIC80.TIC80Gamepad)))
				{
					ret.BoolButtons.Add($"P{i + 1} {Enum.GetName(typeof(LibTIC80.TIC80Gamepad), b)}");
				}
			}

			ret.AddXYPair("Mouse Position {0}", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0);
			ret.BoolButtons.Add("Mouse Left Click");
			ret.BoolButtons.Add("Mouse Middle Click");
			ret.BoolButtons.Add("Mouse Right Click");
			ret.AddXYPair("Mouse Scroll {0}", AxisPairOrientation.RightAndUp, (-32).RangeTo(31), 0);
			ret.BoolButtons.Add("Mouse Relative Toggle");

			foreach (var n in ret.BoolButtons)
			{
				if (n.StartsWith("Mouse"))
				{
					ret.CategoryLabels[n] = "Mouse";
				}
			}

			foreach (var n in ret.Axes.Keys)
			{
				if (n.StartsWith("Mouse"))
				{
					ret.CategoryLabels[n] = "Mouse";
				}
			}

			foreach (var k in Enum.GetValues(typeof(LibTIC80.TIC80Keys)))
			{
				var name = Enum.GetName(typeof(LibTIC80.TIC80Keys), k).TrimStart('_').Replace('_', ' ');
				if (name is "Unknown") continue;
				KeyMap.Add(new(name, (LibTIC80.TIC80Keys)k));
				ret.BoolButtons.Add(name);
				ret.CategoryLabels[name] = "Keyboard";
			}

			ret.BoolButtons.Add("Reset");

			return ret.MakeImmutable();
		}

		private static void GetGamepads(IController controller, ref LibTIC80.TIC80Inputs inputs)
		{
			var gamepads = new LibTIC80.TIC80Gamepad[4];
			for (int i = 0; i < 4; i++)
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
				ret |= 0x8000;
			}
			if (controller.IsPressed("Mouse Middle Click"))
			{
				ret |= 0x4000;
			}
			if (controller.IsPressed("Mouse Right Click"))
			{
				ret |= 0x2000;
			}
			var x = (ushort)((sbyte)controller.AxisValue("Mouse Scroll X") + 32);
			ret |= (ushort)(x << 7);
			var y = (ushort)((sbyte)controller.AxisValue("Mouse Scroll Y") + 32);
			ret |= (ushort)(y << 1);
			if (controller.IsPressed("Mouse Relative Toggle"))
			{
				ret |= 0x0001;
			}
			return ret;
		}

		private static void GetKeys(IController controller, ref LibTIC80.TIC80Inputs inputs)
		{
			var keys = new LibTIC80.TIC80Keys[4];
			int i = 0;
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
				MouseX = (sbyte)controller.AxisValue("Mouse Position X"),
				MouseY = (sbyte)controller.AxisValue("Mouse Position Y"),
				MouseButtons = GetMouseButtons(controller),
			};

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
