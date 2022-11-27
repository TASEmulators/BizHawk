using System;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	[PortedCore(CoreNames.Ares64, "ares team, Near", "v130.1", "https://ares-emu.net/")]
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

			DeterministicEmulation = lp.DeterministicEmulationRequested || (!_syncSettings.UseRealTime);
			InitializeRtc(_syncSettings.InitialTime);

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

			static bool IsGBRom(byte[] rom)
			{
				// GB roms will have the nintendo logo at 0x104 - 0x133
				const string ninLogoSha1 = "0745FDEF34132D1B3D488CFBDF0379A39FD54B4C";
				return ninLogoSha1 == SHA1Checksum.ComputeDigestHex(new ReadOnlySpan<byte>(rom).Slice(0x104, 48));
			}

			var gbRoms = lp.Roms.FindAll(r => IsGBRom(r.FileData)).Select(r => r.FileData).ToList();
			var rom = lp.Roms.Find(r => !gbRoms.Contains(r.FileData) && (char)r.RomData[0x3B] is 'N' or 'C')?.RomData;
			var disk = lp.Roms.Find(r => !gbRoms.Contains(r.FileData) && (char)r.RomData[0x3B] is 'D' or 'E')?.RomData;

			if (rom is null && disk is null)
			{
				if (gbRoms.Count == 0 && lp.Roms.Count == 1) // let's just assume it's an N64 ROM then
				{
					rom = lp.Roms[0].RomData;
				}
				else
				{
					throw new Exception("Could not identify ROM or Disk with given files!");
				}
			}

			var regionByte = rom is null ? 0 : rom[0x3E];
			Region = regionByte switch
			{
				0x44 or 0x46 or 0x49 or 0x50 or 0x53 or 0x55 or 0x58 or 0x59 => DisplayType.PAL,
				_ => DisplayType.NTSC, // note that N64DD is only valid as NTSC
			};

			var pal = Region == DisplayType.PAL;

			if (pal)
			{
				VsyncNumerator = 50;
				VsyncDenominator = 1;
			}

			var pif = Zstd.DecompressZstdStream(new MemoryStream(pal ? Resources.PIF_PAL_ROM.Value : Resources.PIF_NTSC_ROM.Value)).ToArray();

			IsDD = disk is not null;
			byte[] ipl = null;
			if (IsDD)
			{
				ipl = _syncSettings.IPLVersion switch
				{
					LibAres64.IplVer.Japan => lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("N64DD", "IPL JPN")),
					LibAres64.IplVer.Dev => lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("N64DD", "IPL DEV")),
					LibAres64.IplVer.USA => lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("N64DD", "IPL USA")),
					_ => throw new InvalidOperationException(),
				};
			}

			byte[] GetGBRomOrNull(int n)
				=> n < gbRoms.Count ? gbRoms[n] : null;

			unsafe
			{
				fixed (byte*
					pifPtr = pif,
					iplPtr = ipl,
					romPtr = rom,
					diskPtr = disk,
					gb1RomPtr = GetGBRomOrNull(0),
					gb2RomPtr = GetGBRomOrNull(1),
					gb3RomPtr = GetGBRomOrNull(2),
					gb4RomPtr = GetGBRomOrNull(3)) 
				{
					var loadData = new LibAres64.LoadData
					{
						PifData = (IntPtr)pifPtr,
						PifLen = pif.Length,
						IplData = (IntPtr)iplPtr,
						IplLen = ipl?.Length ?? 0,
						RomData = (IntPtr)romPtr,
						RomLen = rom?.Length ?? 0,
						DiskData = (IntPtr)diskPtr,
						DiskLen = disk?.Length ?? 0,
						Gb1RomData = (IntPtr)gb1RomPtr,
						Gb1RomLen = GetGBRomOrNull(0)?.Length ?? 0,
						Gb2RomData = (IntPtr)gb2RomPtr,
						Gb2RomLen = GetGBRomOrNull(1)?.Length ?? 0,
						Gb3RomData = (IntPtr)gb3RomPtr,
						Gb3RomLen = GetGBRomOrNull(2)?.Length ?? 0,
						Gb4RomData = (IntPtr)gb4RomPtr,
						Gb4RomLen = GetGBRomOrNull(3)?.Length ?? 0,
					};
					if (!_core.Init(ref loadData, ControllerSettings, pal, _settings.Deinterlacer == LibAres64.DeinterlacerType.Bob, GetRtcTime(!DeterministicEmulation)))
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
		}

		public DisplayType Region { get; }

		public override ControllerDefinition ControllerDefinition => N64Controller;

		private ControllerDefinition N64Controller { get; }

		public LibAres64.ControllerType[] ControllerSettings { get; }

		public bool IsDD { get; }

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
					ret.AddXYPair($"P{i + 1} {{0}} Axis", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0, new CircularAxisConstraint("Natural Circle", $"P{i + 1} Y Axis", 127.0f));
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
