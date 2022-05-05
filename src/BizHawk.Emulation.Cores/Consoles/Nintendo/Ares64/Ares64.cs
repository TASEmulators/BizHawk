using System;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	[PortedCore(CoreNames.Ares64, "ares team, Near", "v127", "https://ares-emulator.github.io/")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), })]
	public partial class Ares64 : WaterboxCore, IRegionable
	{
		private readonly LibAres64 _core;
		private readonly Ares64Disassembler _disassembler;

		[CoreConstructor(VSystemID.Raw.N64)]
		public Ares64(CoreLoadParameters<Ares64Settings, Ares64SyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth = 640,
				DefaultHeight = 480,
				MaxWidth = 640,
				MaxHeight = 576,
				MaxSamples = 1024,
				DefaultFpsNumerator = 60,
				DefaultFpsDenominator = 1,
				SystemId = VSystemID.Raw.N64,
			})
		{
			_settings = lp.Settings ?? new();
			_syncSettings = lp.SyncSettings ?? new();

			ControllerSettings = new[]
			{
				_syncSettings.P1Controller,
				_syncSettings.P2Controller,
				_syncSettings.P3Controller,
				_syncSettings.P4Controller,
			};

			N64Controller = CreateControllerDefinition(ControllerSettings);
			_tracecb = MakeTrace;

			_core = PreInit<LibAres64>(new WaterboxOptions
			{
				Filename = "ares64.wbx",
				SbrkHeapSizeKB = 2 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 6 * 1024,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 512 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new[] { _tracecb, });

			var rom = lp.Roms[0].RomData;

			Region = rom[0x3E] switch
			{
				0x44 or 0x46 or 0x49 or 0x50 or 0x53 or 0x55 or 0x58 or 0x59 => DisplayType.PAL,
				_ => DisplayType.NTSC,
			};

			var pal = Region == DisplayType.PAL;

			if (pal)
			{
				VsyncNumerator = 50;
				VsyncDenominator = 1;
			}

			LibAres64.LoadFlags loadFlags = 0;
			if (_syncSettings.RestrictAnalogRange)
				loadFlags |= LibAres64.LoadFlags.RestrictAnalogRange;
			if (pal)
				loadFlags |= LibAres64.LoadFlags.Pal;
			if (_settings.Deinterlacer == LibAres64.DeinterlacerType.Bob)
				loadFlags |= LibAres64.LoadFlags.BobDeinterlace;

			var pif = Util.DecompressGzipFile(new MemoryStream(pal ? Resources.PIF_PAL_ROM.Value : Resources.PIF_NTSC_ROM.Value));

			var gbRoms = new byte[][] { null, null, null, null };
			var numGbRoms = lp.Roms.Count - 1;
			for (int i = 0; i < numGbRoms; i++)
			{
				gbRoms[i] = lp.Roms[i + 1].RomData;
			}

			unsafe
			{
				fixed (byte* pifPtr = pif, romPtr = rom, gb1RomPtr = gbRoms[0], gb2RomPtr = gbRoms[1], gb3RomPtr = gbRoms[2], gb4RomPtr = gbRoms[3]) 
				{
					var loadData = new LibAres64.LoadData()
					{
						PifData = (IntPtr)pifPtr,
						PifLen = pif.Length,
						RomData = (IntPtr)romPtr,
						RomLen = rom.Length,
						Gb1RomData = (IntPtr)gb1RomPtr,
						Gb1RomLen = gbRoms[0]?.Length ?? 0,
						Gb2RomData = (IntPtr)gb2RomPtr,
						Gb2RomLen = gbRoms[1]?.Length ?? 0,
						Gb3RomData = (IntPtr)gb3RomPtr,
						Gb3RomLen = gbRoms[2]?.Length ?? 0,
						Gb4RomData = (IntPtr)gb4RomPtr,
						Gb4RomLen = gbRoms[3]?.Length ?? 0,
					};
					if (!_core.Init(ref loadData, ControllerSettings, loadFlags))
					{
						throw new InvalidOperationException("Init returned false!");
					}
				}
			}

			PostInit();

			Tracer = new TraceBuffer("r3400: PC, mnemonic, operands, registers (GPRs, MultLO, MultHI)");
			_serviceProvider.Register(Tracer);

			_disassembler = new(_core);
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			DeterministicEmulation = lp.DeterministicEmulationRequested || (!_syncSettings.UseRealTime);
			InitializeRtc(_syncSettings.InitialTime);
		}

		public DisplayType Region { get; }

		public override ControllerDefinition ControllerDefinition => N64Controller;

		private ControllerDefinition N64Controller { get; }

		public LibAres64.ControllerType[] ControllerSettings { get; }

		private static ControllerDefinition CreateControllerDefinition(LibAres64.ControllerType[] controllerSettings)
		{
			var ret = new ControllerDefinition("Nintendo 64 Controller");
			for (int i = 0; i < 4; i++)
			{
				if (controllerSettings[i] == LibAres64.ControllerType.Mouse)
				{
					ret.BoolButtons.Add($"P{i + 1} Mouse Right");
					ret.BoolButtons.Add($"P{i + 1} Mouse Left");
					ret.AddXYPair($"P{i + 1} {{0}} Axis", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0);
				}
				else if (controllerSettings[i] != LibAres64.ControllerType.Unplugged)
				{
					ret.BoolButtons.Add($"P{i + 1} DPad U");
					ret.BoolButtons.Add($"P{i + 1} DPad D");
					ret.BoolButtons.Add($"P{i + 1} DPad L");
					ret.BoolButtons.Add($"P{i + 1} DPad R");
					ret.BoolButtons.Add($"P{i + 1} Start");
					ret.BoolButtons.Add($"P{i + 1} Z");
					ret.BoolButtons.Add($"P{i + 1} B");
					ret.BoolButtons.Add($"P{i + 1} A");
					ret.BoolButtons.Add($"P{i + 1} C Up");
					ret.BoolButtons.Add($"P{i + 1} C Down");
					ret.BoolButtons.Add($"P{i + 1} C Left");
					ret.BoolButtons.Add($"P{i + 1} C Right");
					ret.BoolButtons.Add($"P{i + 1} L");
					ret.BoolButtons.Add($"P{i + 1} R");
					ret.AddXYPair($"P{i + 1} {{0}} Axis", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0);
					if (controllerSettings[i] == LibAres64.ControllerType.Rumblepak)
					{
						ret.HapticsChannels.Add($"P{i + 1} Rumble Pak");
					}
				}
			}
			ret.BoolButtons.Add("Reset");
			ret.BoolButtons.Add("Power");
			return ret.MakeImmutable();
		}

		private static LibAres64.Buttons GetButtons(IController controller, int num)
		{
			LibAres64.Buttons ret = 0;

			if (controller.IsPressed($"P{num} DPad U"))
				ret |= LibAres64.Buttons.UP;
			if (controller.IsPressed($"P{num} DPad D"))
				ret |= LibAres64.Buttons.DOWN;
			if (controller.IsPressed($"P{num} DPad L"))
				ret |= LibAres64.Buttons.LEFT;
			if (controller.IsPressed($"P{num} DPad R"))
				ret |= LibAres64.Buttons.RIGHT;
			if (controller.IsPressed($"P{num} B") || controller.IsPressed($"P{num} Mouse Right"))
				ret |= LibAres64.Buttons.B;
			if (controller.IsPressed($"P{num} A") || controller.IsPressed($"P{num} Mouse Left"))
				ret |= LibAres64.Buttons.A;
			if (controller.IsPressed($"P{num} C Up"))
				ret |= LibAres64.Buttons.C_UP;
			if (controller.IsPressed($"P{num} C Down"))
				ret |= LibAres64.Buttons.C_DOWN;
			if (controller.IsPressed($"P{num} C Left"))
				ret |= LibAres64.Buttons.C_LEFT;
			if (controller.IsPressed($"P{num} C Right"))
				ret |= LibAres64.Buttons.C_RIGHT;
			if (controller.IsPressed($"P{num} L"))
				ret |= LibAres64.Buttons.L;
			if (controller.IsPressed($"P{num} R"))
				ret |= LibAres64.Buttons.R;
			if (controller.IsPressed($"P{num} Z"))
				ret |= LibAres64.Buttons.Z;
			if (controller.IsPressed($"P{num} Start"))
				ret |= LibAres64.Buttons.START;

			return ret;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			_core.SetTraceCallback(Tracer.IsEnabled() ? _tracecb : null);

			for (int i = 0; i < 4; i++)
			{
				if (ControllerSettings[i] == LibAres64.ControllerType.Rumblepak)
				{
					controller.SetHapticChannelStrength($"P{i + 1} Rumble Pak", _core.GetRumbleStatus(i) ? int.MaxValue : 0);
				}
			}

			return new LibAres64.FrameInfo
			{
				Time = GetRtcTime(!DeterministicEmulation),

				P1Buttons = GetButtons(controller, 1),
				P2Buttons = GetButtons(controller, 2),
				P3Buttons = GetButtons(controller, 3),
				P4Buttons = GetButtons(controller, 4),

				P1XAxis = (short)controller.AxisValue("P1 X Axis"),
				P1YAxis = (short)controller.AxisValue("P1 Y Axis"),

				P2XAxis = (short)controller.AxisValue("P2 X Axis"),
				P2YAxis = (short)controller.AxisValue("P2 Y Axis"),

				P3XAxis = (short)controller.AxisValue("P3 X Axis"),
				P3YAxis = (short)controller.AxisValue("P3 Y Axis"),

				P4XAxis = (short)controller.AxisValue("P4 X Axis"),
				P4YAxis = (short)controller.AxisValue("P4 Y Axis"),

				Reset = controller.IsPressed("Reset"),
				Power = controller.IsPressed("Power"),
			};
		}
	}
}
