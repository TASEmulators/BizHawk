using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// ZXHawk: Core Class
	/// * Main Initialization *
	/// </summary>
	[Core(CoreNames.ZXHawk, "Asnivor, Alyosha")]
	public partial class ZXSpectrum : IRegionable, IDriveLight
	{
		[CoreConstructor(VSystemID.Raw.ZXSpectrum)]
		public ZXSpectrum(
			CoreLoadParameters<ZXSpectrumSettings, ZXSpectrumSyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			CoreComm = lp.Comm;

			_gameInfo = lp.Roms.Select(r => r.Game).ToList();

			_cpu = new Z80A<CpuLink>(default);
			_tracer = new TraceBuffer(_cpu.TraceHeader);

			_files = lp.Roms.Select(r => r.RomData).ToList();

			var settings = lp.Settings ?? new ZXSpectrumSettings();
			var syncSettings = lp.SyncSettings ?? new ZXSpectrumSyncSettings();

			PutSyncSettings(syncSettings);
			PutSettings(settings);

			var joysticks = new List<JoystickType>
			{
				syncSettings.JoystickType1,
				syncSettings.JoystickType2,
				syncSettings.JoystickType3
			};

			DeterministicEmulation = syncSettings.DeterministicEmulation;

			if (lp.DeterministicEmulationRequested)
			{
				if (!DeterministicEmulation)
				{
					CoreComm.Notify("Forcing Deterministic Emulation", null);
				}

				DeterministicEmulation = lp.DeterministicEmulationRequested;
			}

			MachineType = SyncSettings.MachineType;

			switch (MachineType)
			{
				case MachineType.ZXSpectrum16:
					ControllerDefinition = ZXSpectrumControllerDefinition;
					Init(MachineType.ZXSpectrum16, SyncSettings.BorderType, SyncSettings.TapeLoadSpeed, _files, joysticks);
					break;
				case MachineType.ZXSpectrum48:
					ControllerDefinition = ZXSpectrumControllerDefinition;
					Init(MachineType.ZXSpectrum48, SyncSettings.BorderType, SyncSettings.TapeLoadSpeed, _files, joysticks);
					break;
				case MachineType.ZXSpectrum128:
					ControllerDefinition = ZXSpectrumControllerDefinition;
					Init(MachineType.ZXSpectrum128, SyncSettings.BorderType, SyncSettings.TapeLoadSpeed, _files, joysticks);
					break;
				case MachineType.ZXSpectrum128Plus2:
					ControllerDefinition = ZXSpectrumControllerDefinition;
					Init(MachineType.ZXSpectrum128Plus2, SyncSettings.BorderType, SyncSettings.TapeLoadSpeed, _files, joysticks);
					break;
				case MachineType.ZXSpectrum128Plus2a:
					ControllerDefinition = ZXSpectrumControllerDefinition;
					Init(MachineType.ZXSpectrum128Plus2a, SyncSettings.BorderType, SyncSettings.TapeLoadSpeed, _files, joysticks);
					break;
				case MachineType.ZXSpectrum128Plus3:
					ControllerDefinition = ZXSpectrumControllerDefinition;
					Init(MachineType.ZXSpectrum128Plus3, SyncSettings.BorderType, SyncSettings.TapeLoadSpeed, _files, joysticks);
					break;
				case MachineType.Pentagon128:
					ControllerDefinition = ZXSpectrumControllerDefinition;
					Init(MachineType.Pentagon128, SyncSettings.BorderType, SyncSettings.TapeLoadSpeed, _files, joysticks);
					break;
				default:
					throw new InvalidOperationException("Machine not yet emulated");
			}

			HardReset = _machine.HardReset;
			SoftReset = _machine.SoftReset;

			_cpu.SetCpuLink(new CpuLink(this, _machine));

			ser.Register<ITraceable>(_tracer);
			ser.Register<IDisassemblable>(_cpu);
			ser.Register<IVideoProvider>(_machine.ULADevice);

			// initialize sound mixer and attach the various ISoundProvider devices
			SoundMixer = new SyncSoundMixer(targetSampleCount: 882);
			SoundMixer.PinSource(_machine.BuzzerDevice, "System Beeper", 32767 / 10);
			SoundMixer.PinSource(_machine.TapeBuzzer, "Tape Audio", 32767 / 10);
			if (_machine.AYDevice != null)
			{
				SoundMixer.PinSource(_machine.AYDevice, "AY-3-3912");
			}

			// set audio device settings
			if (_machine.AYDevice != null && _machine.AYDevice.GetType() == typeof(AY38912))
			{
				((AY38912)_machine.AYDevice).PanningConfiguration = settings.AYPanConfig;
				_machine.AYDevice.Volume = settings.AYVolume;
			}

			if (_machine.BuzzerDevice != null)
			{
				_machine.BuzzerDevice.Volume = settings.EarVolume;
			}

			if (_machine.TapeBuzzer != null)
			{
				_machine.TapeBuzzer.Volume = settings.TapeVolume;
			}

			DCFilter dc = new DCFilter(SoundMixer, 512);
			ser.Register<ISoundProvider>(dc);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			HardReset();
			SetupMemoryDomains();
		}

		public Action HardReset;
		public Action SoftReset;

		private readonly Z80A<CpuLink> _cpu;
		private readonly TraceBuffer _tracer;
		public IController _controller;
		public SpectrumBase _machine;
		public MachineType MachineType;

		public List<GameInfo> _gameInfo;

		public readonly IList<GameInfo> _tapeInfo = new List<GameInfo>();
		public readonly IList<GameInfo> _diskInfo = new List<GameInfo>();

		private SyncSoundMixer SoundMixer;

		private readonly List<byte[]> _files;

		public bool DiagRom = false;

		private readonly List<string> diagRoms = new List<string>
		{
			@"\DiagROM.v28",
			@"\zx-diagnostics\testrom.bin"
		};
		private readonly int diagIndex = 1;

		internal CoreComm CoreComm { get; }

		private byte[] GetFirmware(int length, params string[] names)
		{
			if (DiagRom & File.Exists(Directory.GetCurrentDirectory() + diagRoms[diagIndex]))
			{
				var rom = File.ReadAllBytes(Directory.GetCurrentDirectory() + diagRoms[diagIndex]);
				return rom;
			}

			// Amstrad licensed ROMs are free to distribute and shipped with BizHawk
			byte[] embeddedRom = new byte[length];
			bool embeddedFound = true;
			switch (names.FirstOrDefault())
			{
				case "48ROM":
					embeddedRom = Zstd.DecompressZstdStream(new MemoryStream(Resources.ZX_48_ROM.Value)).ToArray();
					break;
				case "128ROM":
					embeddedRom = Zstd.DecompressZstdStream(new MemoryStream(Resources.ZX_128_ROM.Value)).ToArray();
					break;
				case "PLUS2ROM":
					embeddedRom = Zstd.DecompressZstdStream(new MemoryStream(Resources.ZX_plus2_rom.Value)).ToArray();
					break;
				case "PLUS2AROM":
				case "PLUS3ROM":
					embeddedRom = Zstd.DecompressZstdStream(new MemoryStream(Resources.ZX_plus2a_rom.Value)).ToArray();
					break;
				default:
					embeddedFound = false;
					break;
			}

			if (embeddedFound)
				return embeddedRom;

			// Embedded ROM not found, maybe this is a peripheral ROM?
			var result = names.Select(n => CoreComm.CoreFileProvider.GetFirmware(new("ZXSpectrum", n))).FirstOrDefault(b => b != null && b.Length == length);
			if (result == null)
			{
				throw new MissingFirmwareException($"At least one of these firmware options is required: {string.Join(", ", names)}");
			}

			return result;
		}

		private MachineType _machineType;

		private void Init(MachineType machineType, BorderType borderType, TapeLoadSpeed tapeLoadSpeed, List<byte[]> files, List<JoystickType> joys)
		{
			_machineType = machineType;

			// setup the emulated model based on the MachineType
			switch (machineType)
			{
				case MachineType.ZXSpectrum16:
					_machine = new ZX16(this, _cpu, borderType, files, joys);
					var _systemRom16 = GetFirmware(0x4000, "48ROM");
					var romData16 = RomData.InitROM(machineType, _systemRom16);
					_machine.InitROM(romData16);
					break;
				case MachineType.ZXSpectrum48:
					_machine = new ZX48(this, _cpu, borderType, files, joys);
					var _systemRom = GetFirmware(0x4000, "48ROM");
					var romData = RomData.InitROM(machineType, _systemRom);
					_machine.InitROM(romData);
					break;
				case MachineType.ZXSpectrum128:
					_machine = new ZX128(this, _cpu, borderType, files, joys);
					var _systemRom128 = GetFirmware(0x8000, "128ROM");
					var romData128 = RomData.InitROM(machineType, _systemRom128);
					_machine.InitROM(romData128);
					break;
				case MachineType.ZXSpectrum128Plus2:
					_machine = new ZX128Plus2(this, _cpu, borderType, files, joys);
					var _systemRomP2 = GetFirmware(0x8000, "PLUS2ROM");
					var romDataP2 = RomData.InitROM(machineType, _systemRomP2);
					_machine.InitROM(romDataP2);
					break;
				case MachineType.ZXSpectrum128Plus2a:
					_machine = new ZX128Plus2a(this, _cpu, borderType, files, joys);
					var _systemRomP4 = GetFirmware(0x10000, "PLUS2AROM");
					var romDataP4 = RomData.InitROM(machineType, _systemRomP4);
					_machine.InitROM(romDataP4);
					break;
				case MachineType.ZXSpectrum128Plus3:
					_machine = new ZX128Plus3(this, _cpu, borderType, files, joys);
					var _systemRomP3 = GetFirmware(0x10000, "PLUS3ROM");
					var romDataP3 = RomData.InitROM(machineType, _systemRomP3);
					_machine.InitROM(romDataP3);
					break;
				case MachineType.Pentagon128:
					_machine = new Pentagon128(this, _cpu, borderType, files, joys);
					var _systemRomPen128 = GetFirmware(0x8000, "PentagonROM");
					var _systemRomTrdos = GetFirmware(0x4000, "TRDOSROM");
					var conc = _systemRomPen128.Concat(_systemRomTrdos).ToArray();
					var romDataPen128 = RomData.InitROM(machineType, conc);
					_machine.InitROM(romDataPen128);
					break;
			}
		}

		public DisplayType Region => DisplayType.PAL;

		public bool DriveLightEnabled => true;

		public bool DriveLightOn =>
			_machine?.TapeDevice?.TapeIsPlaying == true
			|| _machine?.UPDDiskDevice?.DriveLight == true;

		public string DriveLightIconDescription => "Disc Drive Activity";
	}
}
