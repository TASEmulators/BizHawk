using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	[PortedCore(CoreNames.Ares64, "ares team, Near", "v138", "https://ares-emu.net/")]
	public partial class Ares64 : WaterboxCore, IRegionable
	{
		private readonly LibAres64 _core;
		private readonly Ares64Disassembler _disassembler;

		[CoreConstructor(VSystemID.Raw.N64)]
		public Ares64(CoreLoadParameters<Ares64Settings, Ares64SyncSettings> lp)
			: base(lp.Comm, new()
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
			var interpreter = lp.Game.GetBool("ares_force_cpu_interpreter", false) || _syncSettings.CPUEmulation == LibAres64.CpuType.Interpreter;

			_core = PreInit<LibAres64>(new()
			{
				Filename = $"ares64_{(interpreter ? "interpreter" : "recompiler")}.wbx",
				SbrkHeapSizeKB = 2 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 22 * 1024,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 512 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			static bool IsGBRom(byte[] rom)
			{
				// GB roms will have the nintendo logo at 0x104 - 0x133
				const string ninLogoSha1 = "0745FDEF34132D1B3D488CFBDF0379A39FD54B4C";
				return ninLogoSha1 == SHA1Checksum.ComputeDigestHex(new ReadOnlySpan<byte>(rom).Slice(0x104, 48));
			}

			// TODO: this is normally handled frontend side
			// except XML files don't go through RomGame
			// (probably should, but needs refactoring)
			foreach (var r in lp.Roms) _ = N64RomByteswapper.ToZ64Native(r.RomData); // no-op if N64 magic bytes not present

			var gbRoms = lp.Roms.FindAll(r => IsGBRom(r.FileData)).Select(r => r.FileData).ToArray();
			var rom = lp.Roms.Find(r => !gbRoms.Contains(r.FileData) && (char)r.RomData[0x3B] is 'N' or 'C')?.RomData;
			var (disk, error) = TransformDisk(lp.Roms.Find(r => !gbRoms.Contains(r.FileData) && r.RomData != rom)?.FileData);

			if (rom is null && disk is null)
			{
				if (gbRoms.Length == 0 && lp.Roms.Count == 1) // let's just assume it's an N64 ROM then
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
					LibAres64.IplVer.Japan => lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("N64DD", "IPL_JPN")),
					LibAres64.IplVer.Dev => lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("N64DD", "IPL_DEV")),
					LibAres64.IplVer.USA => lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("N64DD", "IPL_USA")),
					_ => throw new InvalidOperationException(),
				};
			}

			byte[] GetGBRomOrNull(int n)
				=> n < gbRoms.Length ? gbRoms[n] : null;

			unsafe
			{
				fixed (byte*
					pifPtr = pif,
					iplPtr = ipl,
					romPtr = rom,
					diskPtr = disk,
					diskErrorPtr = error,
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
						DiskErrorData = (IntPtr)diskErrorPtr,
						DiskErrorLen = error?.Length ?? 0,
						Gb1RomData = (IntPtr)gb1RomPtr,
						Gb1RomLen = GetGBRomOrNull(0)?.Length ?? 0,
						Gb2RomData = (IntPtr)gb2RomPtr,
						Gb2RomLen = GetGBRomOrNull(1)?.Length ?? 0,
						Gb3RomData = (IntPtr)gb3RomPtr,
						Gb3RomLen = GetGBRomOrNull(2)?.Length ?? 0,
						Gb4RomData = (IntPtr)gb4RomPtr,
						Gb4RomLen = GetGBRomOrNull(3)?.Length ?? 0,
					};
					if (!_core.Init(ref loadData, ControllerSettings, pal, GetRtcTime(!DeterministicEmulation)))
					{
						throw new InvalidOperationException("Init returned false!");
					}
				}
			}

			PostInit();

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
			LibAres64.FrameInfo fi = new()
			{
				Time = GetRtcTime(!DeterministicEmulation),
				Reset = controller.IsPressed("Reset"),
				Power = controller.IsPressed("Power"),
				
				BobDeinterlacer = _settings.Deinterlacer == LibAres64.DeinterlacerType.Bob,
				FastVI = _settings.FastVI,
				SkipDraw = !render,
			};
			for (int i = 0; i < 4; i++)
			{
				var peripheral = ControllerSettings[i];
				if (peripheral is LibAres64.ControllerType.Unplugged) continue;
				var num = i + 1;
				if (peripheral is LibAres64.ControllerType.Rumblepak)
				{
					controller.SetHapticChannelStrength($"P{num} Rumble Pak", _core.GetRumbleStatus(i) ? int.MaxValue : 0);
				}
				var buttonsState = GetButtons(controller, num);
				var stickXState = unchecked((short) controller.AxisValue($"P{num} X Axis"));
				var stickYState = unchecked((short) controller.AxisValue($"P{num} Y Axis"));
				switch (num)
				{
					case 1:
						fi.P1Buttons = buttonsState;
						fi.P1XAxis = stickXState;
						fi.P1YAxis = stickYState;
						break;
					case 2:
						fi.P2Buttons = buttonsState;
						fi.P2XAxis = stickXState;
						fi.P2YAxis = stickYState;
						break;
					case 3:
						fi.P3Buttons = buttonsState;
						fi.P3XAxis = stickXState;
						fi.P3YAxis = stickYState;
						break;
					case 4:
						fi.P4Buttons = buttonsState;
						fi.P4XAxis = stickXState;
						fi.P4YAxis = stickYState;
						break;
				}
			}
			return fi;
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
			=> _core.PostLoadState();

		// creates an "error table" for the disk
		// see https://github.com/ares-emulator/ares/blob/09aa6346c71a770fc68e9540d86156dd4769d677/mia/medium/nintendo-64dd.cpp#L102-L189
		private static byte[] CreateErrorTable(byte[] disk)
		{
			static bool RepeatCheck(ReadOnlySpan<byte> slice, int size, int repeat)
			{
				for (int i = 0; i < size; i++)
				{
					for (int j = 0; j < repeat; j++)
					{
						if (slice[i] != slice[(j * size) + i])
						{
							return false;
						}
					}
				}

				return true;
			}

			var ret = new byte[1175 * 2 * 2];
			ret[12] = 0;
			var systemBlocks = new[] { 0, 1, 8, 9 };

			for (int i = 0; i < 4; i++)
			{
				var systemOffset = systemBlocks[i] * 0x4D08;
				if (disk[systemOffset + 0x00] is not 0xE8 and not 0x22) continue;
				if (disk[systemOffset + 0x01] is not 0x48 and not 0x63) continue;
				if (disk[systemOffset + 0x02] is not 0xD3 and not 0xEE) continue;
				if (disk[systemOffset + 0x03] is not 0x16 and not 0x56) continue;
				if (disk[systemOffset + 0x04] != 0x10) continue;
				if (disk[systemOffset + 0x05] <= 0x0F) continue;
				if (disk[systemOffset + 0x05] >= 0x17) continue;
				if (disk[systemOffset + 0x18] != 0xFF) continue;
				if (disk[systemOffset + 0x19] != 0xFF) continue;
				if (disk[systemOffset + 0x1A] != 0xFF) continue;
				if (disk[systemOffset + 0x1B] != 0xFF) continue;
				if (disk[systemOffset + 0x1C] != 0x80) continue;
				if (!RepeatCheck(new(disk, systemOffset, 0xE8 * 0x55), 0xE8, 0x55)) continue;
				ret[systemBlocks[i]] = 0;
				ret[12] = 1;
			}

			if (ret[12] == 0)
			{
				for (int i = 0; i < 4; i++)
				{
					var systemBlock = disk.Length == 0x3DEC800 ? (systemBlocks[i] + 2) ^ 1 : (systemBlocks[i] + 2);
					var systemOffset = systemBlock * 0x4D08;
					ret[systemBlocks[i] + 2] = 1;
					if (disk[systemOffset + 0x00] != 0x00) continue;
					if (disk[systemOffset + 0x01] != 0x00) continue;
					if (disk[systemOffset + 0x02] != 0x00) continue;
					if (disk[systemOffset + 0x03] != 0x00) continue;
					if (disk[systemOffset + 0x04] != 0x10) continue;
					if (disk[systemOffset + 0x05] <= 0x0F) continue;
					if (disk[systemOffset + 0x05] >= 0x17) continue;
					if (disk[systemOffset + 0x18] != 0xFF) continue;
					if (disk[systemOffset + 0x19] != 0xFF) continue;
					if (disk[systemOffset + 0x1A] != 0xFF) continue;
					if (disk[systemOffset + 0x1B] != 0xFF) continue;
					if (disk[systemOffset + 0x1C] != 0x80) continue;
					if (!RepeatCheck(new(disk, systemOffset, 0xC0 * 0x55), 0xC0, 0x55)) continue;
					ret[systemBlocks[i] + 2] = 0;
				}
			}

			for (int i = 0; i < 2; i++)
			{
				var diskIdOffset = (14 + i) * 0x4D08;
				ret[14 + i] = 1;
				if (!RepeatCheck(new(disk, diskIdOffset, 0xE8 * 0x55), 0xE8, 0x55)) continue;
				ret[14 + i] = 0;
			}

			return ret;
		}

		private static readonly int[] _blockSizeTable = new[] { 0x4D08, 0x47B8, 0x4510, 0x3FC0, 0x3A70, 0x3520, 0x2FD0, 0x2A80, 0x2530 };
		private static readonly int[,] _vzoneLbaTable = new[,]
		{
			{ 0x0124, 0x0248, 0x035A, 0x047E, 0x05A2, 0x06B4, 0x07C6, 0x08D8, 0x09EA, 0x0AB6, 0x0B82, 0x0C94, 0x0DA6, 0x0EB8, 0x0FCA, 0x10DC },
			{ 0x0124, 0x0248, 0x035A, 0x046C, 0x057E, 0x06A2, 0x07C6, 0x08D8, 0x09EA, 0x0AFC, 0x0BC8, 0x0C94, 0x0DA6, 0x0EB8, 0x0FCA, 0x10DC },
			{ 0x0124, 0x0248, 0x035A, 0x046C, 0x057E, 0x0690, 0x07A2, 0x08C6, 0x09EA, 0x0AFC, 0x0C0E, 0x0CDA, 0x0DA6, 0x0EB8, 0x0FCA, 0x10DC },
			{ 0x0124, 0x0248, 0x035A, 0x046C, 0x057E, 0x0690, 0x07A2, 0x08B4, 0x09C6, 0x0AEA, 0x0C0E, 0x0D20, 0x0DEC, 0x0EB8, 0x0FCA, 0x10DC },
			{ 0x0124, 0x0248, 0x035A, 0x046C, 0x057E, 0x0690, 0x07A2, 0x08B4, 0x09C6, 0x0AD8, 0x0BEA, 0x0D0E, 0x0E32, 0x0EFE, 0x0FCA, 0x10DC },
			{ 0x0124, 0x0248, 0x035A, 0x046C, 0x057E, 0x0690, 0x07A2, 0x086E, 0x0980, 0x0A92, 0x0BA4, 0x0CB6, 0x0DC8, 0x0EEC, 0x1010, 0x10DC },
			{ 0x0124, 0x0248, 0x035A, 0x046C, 0x057E, 0x0690, 0x07A2, 0x086E, 0x093A, 0x0A4C, 0x0B5E, 0x0C70, 0x0D82, 0x0E94, 0x0FB8, 0x10DC },
		};
		private static readonly int[,] _vzone2pzoneTable = new[,]
		{
			{ 0x0, 0x1, 0x2, 0x9, 0x8, 0x3, 0x4, 0x5, 0x6, 0x7, 0xF, 0xE, 0xD, 0xC, 0xB, 0xA },
			{ 0x0, 0x1, 0x2, 0x3, 0xA, 0x9, 0x8, 0x4, 0x5, 0x6, 0x7, 0xF, 0xE, 0xD, 0xC, 0xB },
			{ 0x0, 0x1, 0x2, 0x3, 0x4, 0xB, 0xA, 0x9, 0x8, 0x5, 0x6, 0x7, 0xF, 0xE, 0xD, 0xC },
			{ 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0xC, 0xB, 0xA, 0x9, 0x8, 0x6, 0x7, 0xF, 0xE, 0xD },
			{ 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0xD, 0xC, 0xB, 0xA, 0x9, 0x8, 0x7, 0xF, 0xE },
			{ 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0xE, 0xD, 0xC, 0xB, 0xA, 0x9, 0x8, 0xF },
			{ 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0xF, 0xE, 0xD, 0xC, 0xB, 0xA, 0x9, 0x8 },
		};
		private static readonly int[] _trackPhysicalTable = new[] { 0x000, 0x09E, 0x13C, 0x1D1, 0x266, 0x2FB, 0x390, 0x425, 0x091, 0x12F, 0x1C4, 0x259, 0x2EE, 0x383, 0x418, 0x48A };
		private static readonly int[] _startOffsetTable = new[] { 0x0, 0x5F15E0, 0xB79D00, 0x10801A0, 0x1523720, 0x1963D80, 0x1D414C0, 0x20BBCE0, 0x23196E0, 0x28A1E00, 0x2DF5DC0, 0x3299340, 0x36D99A0, 0x3AB70E0, 0x3E31900, 0x4149200 };

		// converts ndd to "mame format" (which ares uses)
		// see https://github.com/ares-emulator/ares/blob/09aa6346c71a770fc68e9540d86156dd4769d677/mia/medium/nintendo-64dd.cpp#L191-L302
		private static (byte[] Disk, byte[] Error) TransformDisk(byte[] disk)
		{
			if (disk is null) return default;

			// already in mame format
			if (disk.Length == 0x435B0C0) return (disk, CreateErrorTable(disk));

			// ndd is always 0x3DEC800 bytes apparently?
			if (disk.Length != 0x3DEC800) return default;

			// need the error table for this
			var errorTable = CreateErrorTable(disk);

			// "system area check"
			var systemCheck = false;
			var systemBlocks = new[] { 9, 8, 1, 0 };
			var systemOffset = 0;
			for (int i = 0; i < 4; i++)
			{
				if (errorTable[12] == 0)
				{
					var systemBlock = systemBlocks[i] + 2;
					if (errorTable[systemBlock] == 0)
					{
						systemCheck = true;
						systemOffset = (systemBlock ^ 1) * 0x4D08;
					}
				}
				else
				{
					var systemBlock = systemBlocks[i];
					if (errorTable[systemBlock] == 0)
					{
						systemCheck = true;
						systemOffset = systemBlock * 0x4D08;
					}
				}
			}

			if (!systemCheck) return default;

			var dataFormat = new ReadOnlySpan<byte>(disk, systemOffset, 0xE8);

			var diskIndex = 0;
			var ret = new byte[0x435B0C0];

			var type = dataFormat[5] & 0xF;
			var vzone = 0;

			for (int lba = 0; lba < 0x10DC; lba++)
			{
				if (lba >= _vzoneLbaTable[type, vzone]) vzone++;
				var pzoneCalc = _vzone2pzoneTable[type, vzone];
				var headCalc = pzoneCalc > 7;

				var lba_vzone = lba;
				if (vzone > 0) lba_vzone -= _vzoneLbaTable[type, vzone - 1];

				var trackStart = _trackPhysicalTable[headCalc ? pzoneCalc - 8 : pzoneCalc];
				var trackCalc = _trackPhysicalTable[pzoneCalc];
				if (headCalc) trackCalc -= (lba_vzone >> 1);
				else trackCalc += (lba_vzone >> 1);

				var defectOffset = 0;
				if (pzoneCalc > 0) defectOffset = dataFormat[8 + pzoneCalc - 1];
				var defectAmount = dataFormat[8 + pzoneCalc] - defectOffset;

				while ((defectAmount != 0) && ((dataFormat[0x20 + defectOffset] + trackStart) <= trackCalc))
				{
					trackCalc++;
					defectOffset++;
					defectAmount--;
				}

				var blockCalc = ((lba & 3) == 0 || (lba & 3) == 3) ? 0 : 1;

				var offsetCalc = _startOffsetTable[pzoneCalc];
				offsetCalc += (trackCalc - trackStart) * _blockSizeTable[headCalc ? pzoneCalc - 7 : pzoneCalc] * 2;
				offsetCalc += _blockSizeTable[headCalc ? pzoneCalc - 7 : pzoneCalc] * blockCalc;

				var blockSize = _blockSizeTable[headCalc ? pzoneCalc - 7 : pzoneCalc];
				for (int i = 0; i < blockSize; i++)
				{
					ret[offsetCalc + i] = disk[diskIndex++];
				}
			}

			return (ret, errorTable);
		}
	}
}
