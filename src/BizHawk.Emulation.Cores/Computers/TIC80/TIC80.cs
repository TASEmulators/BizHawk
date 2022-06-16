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
		public TIC80(CoreLoadParameters<TIC80Settings, object> lp)
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
		}

		private static readonly List<KeyValuePair<string, LibTIC80.TI80Keys>> KeyMap = new();

		public override ControllerDefinition ControllerDefinition => TI80Controller;

		private static readonly ControllerDefinition TI80Controller = CreateControllerDefinition();

		private static ControllerDefinition CreateControllerDefinition()
		{
			var ret = new ControllerDefinition("TIC-80 Controller");

			for (int i = 0; i < 4; i++)
			{
				foreach (var b in Enum.GetValues(typeof(LibTIC80.TI80Gamepad)))
				{
					ret.BoolButtons.Add($"P{i + 1} {Enum.GetName(typeof(LibTIC80.TI80Gamepad), b)}");
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

			foreach (var k in Enum.GetValues(typeof(LibTIC80.TI80Keys)))
			{
				var name = Enum.GetName(typeof(LibTIC80.TI80Keys), k).TrimStart('_').Replace('_', ' ');
				if (name is "Unknown") continue;
				KeyMap.Add(new(name, (LibTIC80.TI80Keys)k));
				ret.BoolButtons.Add(name);
				ret.CategoryLabels[name] = "Keyboard";
			}

			return ret.MakeImmutable();
		}

		private static void GetGamepads(IController controller, LibTIC80.TI80Gamepad[] gamepads)
		{
			for (int i = 0; i < 4; i++)
			{
				gamepads[i] = 0;
				foreach (var b in Enum.GetValues(typeof(LibTIC80.TI80Gamepad)))
				{
					if (controller.IsPressed($"P{i + 1} {Enum.GetName(typeof(LibTIC80.TI80Gamepad), b)}"))
					{
						gamepads[i] |= (LibTIC80.TI80Gamepad)b;
					}
				}
			}
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

		private static void GetKeys(IController controller, LibTIC80.TI80Keys[] keys)
		{
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
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var ret = new LibTIC80.FrameInfo
			{
				MouseX = (sbyte)controller.AxisValue("Mouse Position X"),
				MouseY = (sbyte)controller.AxisValue("Mouse Position Y"),
				MouseButtons = GetMouseButtons(controller),
				
				Crop = _settings.Crop,
			};

			GetGamepads(controller, ret.Gamepads);
			GetKeys(controller, ret.Keys);

			return ret;
		}
	}
}
