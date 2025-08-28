using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Consoles.Panasonic3DO
{
	[PortedCore(
		name: CoreNames.Opera,
		author: "Libretro Team",
		portedVersion: "2025.03.08 (67a29e6)",
		portedUrl: "https://github.com/libretro/opera-libretro",
		isReleased: false)]
	public partial class Opera : WaterboxCore
	{
		private static readonly Configuration ConfigNTSC = new()
		{
			SystemId = VSystemID.Raw.Panasonic3DO,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibOpera.NTSC_WIDTH,
			DefaultHeight = LibOpera.NTSC_HEIGHT,
			MaxWidth = LibOpera.PAL2_WIDTH,
			MaxHeight = LibOpera.PAL2_HEIGHT,
			DefaultFpsNumerator = LibOpera.NTSC_VIDEO_NUMERATOR,
			DefaultFpsDenominator = LibOpera.NTSC_VIDEO_DENOMINATOR,
		};

		private static readonly Configuration ConfigPAL1 = new()
		{
			SystemId = VSystemID.Raw.Panasonic3DO,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibOpera.PAL1_WIDTH,
			DefaultHeight = LibOpera.PAL1_HEIGHT,
			MaxWidth = LibOpera.PAL1_WIDTH,
			MaxHeight = LibOpera.PAL1_HEIGHT,
			DefaultFpsNumerator = LibOpera.PAL1_VIDEO_NUMERATOR,
			DefaultFpsDenominator = LibOpera.PAL1_VIDEO_DENOMINATOR,
		};

		private static readonly Configuration ConfigPAL2 = new()
		{
			SystemId = VSystemID.Raw.Panasonic3DO,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibOpera.PAL2_WIDTH,
			DefaultHeight = LibOpera.PAL2_HEIGHT,
			MaxWidth = LibOpera.PAL2_WIDTH,
			MaxHeight = LibOpera.PAL2_HEIGHT,
			DefaultFpsNumerator = LibOpera.PAL2_VIDEO_NUMERATOR,
			DefaultFpsDenominator = LibOpera.PAL2_VIDEO_DENOMINATOR,
		};

		public override int VirtualWidth => BufferHeight * 4 / 3;

		[CoreConstructor(VSystemID.Raw.Panasonic3DO)]
		public Opera(CoreLoadParameters<object, SyncSettings> lp)
			: base(
				lp.Comm,
				lp.SyncSettings?.VideoStandard switch
				{
					null or VideoStandard.NTSC => ConfigNTSC,
					VideoStandard.PAL1 => ConfigPAL1,
					VideoStandard.PAL2 => ConfigPAL2,
					_ => throw new InvalidOperationException($"Unexpected value for sync setting {nameof(SyncSettings.VideoStandard)}"),
				})
		{
			DriveLightEnabled = true;

			if (lp.Discs.Count == 0)
			{
				throw new InvalidOperationException("No CDs provided for emulation");
			}

			_isMultidisc = lp.Discs.Count > 1;

			_CDReadCallback = CDRead;
			_CDSectorCountCallback = CDSectorCount;

			_discIndex = 0;
			_discNames = lp.Discs.Select(d => d.DiscName).ToArray();
			_cdReaders = lp.Discs.Select(d => new DiscSectorReader(d.DiscData)).ToArray();

			_cdSectorCounts = new int[_cdReaders.Length];
			for (var i = 0; i < _cdSectorCounts.Length; i++)
			{
				// 3DO weirdly seems to be expecting a sector count that's represents a flat MiB count
				// This sector count can be found within the first sector of the disc
				// This sector count can also be inferred by rounding down the leadout LBA to a 512 multiple
				_cdReaders[i].ReadLBA_2048(0, _sectorBuffer, 0);
				var sectorCountInDisc = BinaryPrimitives.ReadInt32BigEndian(_sectorBuffer.AsSpan(80, 4));
				var sectorCountFromLeadout = lp.Discs[i].DiscData.Session1.LeadoutLBA & ~0x1FF;
				if (sectorCountInDisc != sectorCountFromLeadout)
				{
					Console.WriteLine($"Mismatched sector count in {_discNames[i]} ({sectorCountInDisc} / {sectorCountFromLeadout})");
				}

				// upstream uses this always, so might as well
				_cdSectorCounts[i] = sectorCountInDisc;
			}

			Console.WriteLine($"[CD] Sector count: {_cdSectorCounts[0]}");

			_syncSettings = lp.SyncSettings ?? new();
			_activeSyncSettings = _syncSettings.Clone();
			ControllerDefinition = CreateControllerDefinition(_activeSyncSettings, _isMultidisc);

			var libOpera = PreInit<LibOpera>(
				new WaterboxOptions
				{
					Filename = "opera.wbx",
					SbrkHeapSizeKB = 256 * 1024,
					SealedHeapSizeKB = 1024,
					InvisibleHeapSizeKB = 1024,
					PlainHeapSizeKB = 1024,
					MmapHeapSizeKB = 256 * 1024,
					SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
					SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
				},
				[ _CDReadCallback, _CDSectorCountCallback ]);

			libOpera.SetCdCallbacks(_CDReadCallback, _CDSectorCountCallback);

			var biosType = _activeSyncSettings.SystemType switch
			{
				SystemType.Panasonic_FZ1_U => "Panasonic_FZ1_U",
				SystemType.Panasonic_FZ1_E => "Panasonic_FZ1_E",
				SystemType.Panasonic_FZ1_J => "Panasonic_FZ1_J",
				SystemType.Panasonic_FZ10_U => "Panasonic_FZ10_U",
				SystemType.Panasonic_FZ10_E => "Panasonic_FZ10_E",
				SystemType.Panasonic_FZ10_J => "Panasonic_FZ10_J",
				SystemType.Goldstar_GDO101P => "Goldstar_GDO101P",
				SystemType.Goldstar_FC1 => "Goldstar_FC1",
				SystemType.Sanyo_IMP21J_Try => "Sanyo_IMP21J_Try",
				SystemType.Sanyo_HC21 => "Sanyo_HC21",
				SystemType.Shootout_At_Old_Tucson => "Shootout_At_Old_Tucson",
				SystemType._3DO_NTSC_1fc2 => "3DO_NTSC_1fc2",
				_ => throw new InvalidOperationException($"Unknown {nameof(SystemType)}"),
			};

			var (biosData, biosInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(new(VSystemID.Raw.Panasonic3DO, biosType), "BIOS ROM files are required!");
			var biosFileName = biosInfo.Name + ".bin";
			_exe.AddReadonlyFile(biosData, biosFileName);

			var fontROMType = _activeSyncSettings.FontROM switch
			{
				FontROM.None => "None",
				FontROM.Kanji_ROM_Panasonic_FZ1 => "Kanji_ROM_Panasonic_FZ1",
				FontROM.Kanji_ROM_Panasonic_FZ10 => "Kanji_ROM_Panasonic_FZ10",
				_ => throw new InvalidOperationException($"Unknown {nameof(FontROM)}"),
			};

			var fontROMFileName = "None";
			if (fontROMType != "None")
			{
				var (fontROMData, fontROMInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(new(VSystemID.Raw.Panasonic3DO, fontROMType), "Font ROM files are required!");
				_exe.AddReadonlyFile(fontROMData, fontROMInfo.Name);
				fontROMFileName = fontROMInfo.Name;
			}

			Console.WriteLine($"Using BIOS ROM: '{biosFileName}', Font ROM: '{fontROMFileName}'");
			if (!libOpera.Init(
				gameFile: "3DO",
				biosFile: biosFileName,
				fontFile: fontROMFileName,
				port1Type: (int) _activeSyncSettings.Controller1Type,
				port2Type: (int) _activeSyncSettings.Controller2Type,
				videoStandard: (int) _activeSyncSettings.VideoStandard))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}

			PostInit();
		}

		private readonly LibOpera.CDReadCallback _CDReadCallback;
		private readonly LibOpera.CDSectorCountCallback _CDSectorCountCallback;

		private readonly bool _isMultidisc;

		private int _discIndex;
		private readonly string[] _discNames;
		private readonly DiscSectorReader[] _cdReaders;
		private readonly int[] _cdSectorCounts;

		private const int CD_SECTOR_SIZE = 2048;
		private readonly byte[] _sectorBuffer = new byte[CD_SECTOR_SIZE];

		private void SelectNextDisc()
		{
			_discIndex++;
			if (_discIndex == _cdReaders.Length)
			{
				_discIndex = _cdReaders.Length - 1;
			}

			CoreComm.Notify((uint)_discIndex < _cdReaders.Length
				? "No CDROM inserted"
				: $"Selected CDROM {_discIndex}: {_discNames[_discIndex]}", null);
		}

		private void SelectPrevDisc()
		{
			_discIndex--;
			if (_discIndex < -1)
			{
				_discIndex = -1;
			}

			CoreComm.Notify((uint)_discIndex < _cdReaders.Length
				? "No CDROM inserted"
				: $"Selected CDROM {_discIndex}: {_discNames[_discIndex]}", null);
		}

		private void CDRead(int lba, IntPtr dest)
		{
			if ((uint)_discIndex < _cdReaders.Length)
			{
				_cdReaders[_discIndex].ReadLBA_2048(lba, _sectorBuffer, 0);
				Marshal.Copy(_sectorBuffer, 0, dest, CD_SECTOR_SIZE);
			}

			DriveLightOn = true;
		}

		private int CDSectorCount()
		{
			if ((uint)_discIndex < _cdSectorCounts.Length)
			{
				return _cdSectorCounts[_discIndex];
			}

			return -1;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibOpera.FrameInfo();

			if (_isMultidisc)
			{
				if (controller.IsPressed("Next Disc")) SelectNextDisc();
				if (controller.IsPressed("Prev Disc")) SelectPrevDisc();
			}

			DriveLightOn = false;
			fi.port1 = ProcessController(1, _activeSyncSettings.Controller1Type, controller);
			fi.port2 = ProcessController(2, _activeSyncSettings.Controller2Type, controller);

			fi.isReset = controller.IsPressed("Reset") ? 1 : 0;

			return fi;
		}

		private static LibOpera.GameInput ProcessController(int port, ControllerType type, IController controller)
		{
			var gameInput = default(LibOpera.GameInput);

			switch (type)
			{
				case ControllerType.None:
					break;

				case ControllerType.Gamepad:
					gameInput.gamepad.up = controller.IsPressed($"P{port} {JoystickButtons.Up}") ? 1 : 0;
					gameInput.gamepad.down = controller.IsPressed($"P{port} {JoystickButtons.Down}") ? 1 : 0;
					gameInput.gamepad.left = controller.IsPressed($"P{port} {JoystickButtons.Left}") ? 1 : 0;
					gameInput.gamepad.right = controller.IsPressed($"P{port} {JoystickButtons.Right}") ? 1 : 0;
					gameInput.gamepad.buttonX = controller.IsPressed($"P{port} {JoystickButtons.ButtonX}") ? 1 : 0;
					gameInput.gamepad.buttonP = controller.IsPressed($"P{port} {JoystickButtons.ButtonP}") ? 1 : 0;
					gameInput.gamepad.buttonA = controller.IsPressed($"P{port} {JoystickButtons.ButtonA}") ? 1 : 0;
					gameInput.gamepad.buttonB = controller.IsPressed($"P{port} {JoystickButtons.ButtonB}") ? 1 : 0;
					gameInput.gamepad.buttonC = controller.IsPressed($"P{port} {JoystickButtons.ButtonC}") ? 1 : 0;
					gameInput.gamepad.buttonL = controller.IsPressed($"P{port} {JoystickButtons.ButtonL}") ? 1 : 0;
					gameInput.gamepad.buttonR = controller.IsPressed($"P{port} {JoystickButtons.ButtonR}") ? 1 : 0;
					break;

				case ControllerType.Mouse:
					gameInput.mouse.dX = controller.AxisValue($"P{port} {Inputs.MouseX}");
					gameInput.mouse.dY = controller.AxisValue($"P{port} {Inputs.MouseY}");
					gameInput.mouse.leftButton = controller.IsPressed($"P{port} {Inputs.MouseLeftButton}") ? 1 : 0;
					gameInput.mouse.middleButton = controller.IsPressed($"P{port} {Inputs.MouseMiddleButton}") ? 1 : 0;
					gameInput.mouse.rightButton = controller.IsPressed($"P{port} {Inputs.MouseRightButton}") ? 1 : 0;
					gameInput.mouse.fourthButton = controller.IsPressed($"P{port} {Inputs.MouseFourthButton}") ? 1 : 0;
					break;

				case ControllerType.FlightStick:
					gameInput.flightStick.up = controller.IsPressed($"P{port} {FlightStickButtons.Up}") ? 1 : 0;
					gameInput.flightStick.down = controller.IsPressed($"P{port} {FlightStickButtons.Down}") ? 1 : 0;
					gameInput.flightStick.left = controller.IsPressed($"P{port} {FlightStickButtons.Left}") ? 1 : 0;
					gameInput.flightStick.right = controller.IsPressed($"P{port} {FlightStickButtons.Right}") ? 1 : 0;
					gameInput.flightStick.fire = controller.IsPressed($"P{port} {FlightStickButtons.Fire}") ? 1 : 0;
					gameInput.flightStick.buttonA = controller.IsPressed($"P{port} {FlightStickButtons.ButtonA}") ? 1 : 0;
					gameInput.flightStick.buttonB = controller.IsPressed($"P{port} {FlightStickButtons.ButtonB}") ? 1 : 0;
					gameInput.flightStick.buttonC = controller.IsPressed($"P{port} {FlightStickButtons.ButtonC}") ? 1 : 0;
					gameInput.flightStick.buttonX = controller.IsPressed($"P{port} {FlightStickButtons.ButtonX}") ? 1 : 0;
					gameInput.flightStick.buttonP = controller.IsPressed($"P{port} {FlightStickButtons.ButtonP}") ? 1 : 0;
					gameInput.flightStick.leftTrigger = controller.IsPressed($"P{port} {FlightStickButtons.LeftTrigger}") ? 1 : 0;
					gameInput.flightStick.rightTrigger = controller.IsPressed($"P{port} {FlightStickButtons.RightTrigger}") ? 1 : 0;
					gameInput.flightStick.horizontalAxis = controller.AxisValue($"P{port} {Inputs.FlighStickHorizontalAxis}");
					gameInput.flightStick.verticalAxis = controller.AxisValue($"P{port} {Inputs.FlighStickVerticalAxis}");
					gameInput.flightStick.altitudeAxis = controller.AxisValue($"P{port} {Inputs.FlighStickAltitudeAxis}");
					break;

				case ControllerType.LightGun:
					gameInput.lightGun.trigger = controller.IsPressed($"P{port} {LightGunButtons.Trigger}") ? 1 : 0;
					gameInput.lightGun.select = controller.IsPressed($"P{port} {LightGunButtons.Select}") ? 1 : 0;
					gameInput.lightGun.reload = controller.IsPressed($"P{port} {LightGunButtons.Reload}") ? 1 : 0;
					gameInput.lightGun.isOffScreen = controller.IsPressed($"P{port} {LightGunButtons.IsOffScreen}") ? 1 : 0;
					gameInput.lightGun.screenX = controller.AxisValue($"P{port} {Inputs.LightGunScreenX}");
					gameInput.lightGun.screenY = controller.AxisValue($"P{port} {Inputs.LightGunScreenY}");
					break;

				case ControllerType.ArcadeLightGun:
					gameInput.arcadeLightGun.trigger = controller.IsPressed($"P{port} {ArcadeLightGunButtons.Trigger}") ? 1 : 0;
					gameInput.arcadeLightGun.select = controller.IsPressed($"P{port} {ArcadeLightGunButtons.Select}") ? 1 : 0;
					gameInput.arcadeLightGun.start = controller.IsPressed($"P{port} {ArcadeLightGunButtons.Start}") ? 1 : 0;
					gameInput.arcadeLightGun.reload = controller.IsPressed($"P{port} {ArcadeLightGunButtons.Reload}") ? 1 : 0;
					gameInput.arcadeLightGun.auxA = controller.IsPressed($"P{port} {ArcadeLightGunButtons.AuxA}") ? 1 : 0;
					gameInput.arcadeLightGun.isOffScreen = controller.IsPressed($"P{port} {ArcadeLightGunButtons.IsOffScreen}") ? 1 : 0;
					gameInput.arcadeLightGun.screenX = controller.AxisValue($"P{port} {Inputs.LightGunScreenX}");
					gameInput.arcadeLightGun.screenY = controller.AxisValue($"P{port} {Inputs.LightGunScreenY}");
					break;

				case ControllerType.OrbatakTrackball:
					gameInput.orbatakTrackball.startP1 = controller.IsPressed($"P{port} {OrbatakTrackballButtons.StartP1}") ? 1 : 0;
					gameInput.orbatakTrackball.startP2 = controller.IsPressed($"P{port} {OrbatakTrackballButtons.StartP2}") ? 1 : 0;
					gameInput.orbatakTrackball.coinP1 = controller.IsPressed($"P{port} {OrbatakTrackballButtons.CoinP1}") ? 1 : 0;
					gameInput.orbatakTrackball.coinP2 = controller.IsPressed($"P{port} {OrbatakTrackballButtons.CoinP2}") ? 1 : 0;
					gameInput.orbatakTrackball.service = controller.IsPressed($"P{port} {OrbatakTrackballButtons.Service}") ? 1 : 0;
					gameInput.orbatakTrackball.dX = controller.AxisValue($"P{port} {Inputs.TrackballPosX}");
					gameInput.orbatakTrackball.dY = controller.AxisValue($"P{port} {Inputs.TrackballPosY}");
					break;

				default:
					throw new InvalidOperationException();
			}

			return gameInput;
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_discIndex);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_discIndex = reader.ReadInt32();
		}
	}
}
