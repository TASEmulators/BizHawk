using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using BizHawk.Emulation.Cores.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Emulation.Cores.Components;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// ZXHawk: Core Class
	/// * Main Initialization *
	/// </summary>
	[Core(
		"ZXHawk",
		"Asnivor, Alyosha",
		isPorted: false,
		isReleased: true)]
	public partial class ZXSpectrum : IRegionable, IDriveLight
	{
		public ZXSpectrum(CoreComm comm, IEnumerable<byte[]> files, List<GameInfo> game, object settings, object syncSettings, bool? deterministic)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			InputCallbacks = new InputCallbackSystem();
			MemoryCallbacks = new MemoryCallbackSystem(new[] { "System Bus" });

			CoreComm = comm;

			_gameInfo = game;

			_cpu = new Z80A();

			_tracer = new TraceBuffer { Header = _cpu.TraceHeader };

			_files = files?.ToList() ?? new List<byte[]>();

			if (settings == null)
				settings = new ZXSpectrumSettings();
			if (syncSettings == null)
				syncSettings = new ZXSpectrumSyncSettings();

			PutSyncSettings((ZXSpectrumSyncSettings)syncSettings ?? new ZXSpectrumSyncSettings());
			PutSettings((ZXSpectrumSettings)settings ?? new ZXSpectrumSettings());

			List<JoystickType> joysticks = new List<JoystickType>();
			joysticks.Add(((ZXSpectrumSyncSettings)syncSettings).JoystickType1);
			joysticks.Add(((ZXSpectrumSyncSettings)syncSettings).JoystickType2);
			joysticks.Add(((ZXSpectrumSyncSettings)syncSettings).JoystickType3);

			deterministicEmulation = ((ZXSpectrumSyncSettings)syncSettings).DeterministicEmulation;

			if (deterministic != null && deterministic == true)
			{
				if (deterministicEmulation == false)
				{
					CoreComm.Notify("Forcing Deterministic Emulation");
				}

				deterministicEmulation = deterministic.Value;
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

			_cpu.MemoryCallbacks = MemoryCallbacks;

			HardReset = _machine.HardReset;
			SoftReset = _machine.SoftReset;

			_cpu.FetchMemory = _machine.ReadMemory;
			_cpu.ReadMemory = _machine.ReadMemory;
			_cpu.WriteMemory = _machine.WriteMemory;
			_cpu.ReadHardware = _machine.ReadPort;
			_cpu.WriteHardware = _machine.WritePort;
			_cpu.FetchDB = _machine.PushBus;
			_cpu.OnExecFetch = _machine.CPUMon.OnExecFetch;

			ser.Register<ITraceable>(_tracer);
			ser.Register<IDisassemblable>(_cpu);
			ser.Register<IVideoProvider>(_machine.ULADevice);

			// initialize sound mixer and attach the various ISoundProvider devices
			SoundMixer = new SyncSoundMixer(targetSampleCount: 882);
			SoundMixer.PinSource(_machine.BuzzerDevice, "System Beeper", (int)(32767 / 10));
			SoundMixer.PinSource(_machine.TapeBuzzer, "Tape Audio", (int)(32767 / 10));
			if (_machine.AYDevice != null)
			{
				SoundMixer.PinSource(_machine.AYDevice, "AY-3-3912");
			}

			// set audio device settings
			if (_machine.AYDevice != null && _machine.AYDevice.GetType() == typeof(AY38912))
			{
				((AY38912)_machine.AYDevice).PanningConfiguration = ((ZXSpectrumSettings)settings).AYPanConfig;
				_machine.AYDevice.Volume = ((ZXSpectrumSettings)settings).AYVolume;
			}

			if (_machine.BuzzerDevice != null)
			{
				_machine.BuzzerDevice.Volume = ((ZXSpectrumSettings)settings).EarVolume;
			}

			if (_machine.TapeBuzzer != null)
			{
				_machine.TapeBuzzer.Volume = ((ZXSpectrumSettings)settings).TapeVolume;
			}

			DCFilter dc = new DCFilter(SoundMixer, 512);
			ser.Register<ISoundProvider>(dc);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			HardReset();
			SetupMemoryDomains();
		}

		public Action HardReset;
		public Action SoftReset;

		private readonly Z80A _cpu;
		private readonly TraceBuffer _tracer;
		public IController _controller;
		public SpectrumBase _machine;
		public MachineType MachineType;

		public List<GameInfo> _gameInfo;

		public List<GameInfo> _tapeInfo = new List<GameInfo>();
		public List<GameInfo> _diskInfo = new List<GameInfo>();

		private SyncSoundMixer SoundMixer;

		private readonly List<byte[]> _files;

		public bool DiagRom = false;

		private List<string> diagRoms = new List<string>
		{
			@"\DiagROM.v28",
			@"\zx-diagnostics\testrom.bin"
		};
		private int diagIndex = 1;

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
					embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.ZX_48_ROM));
					break;
				case "128ROM":
					embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.ZX_128_ROM));
					break;
				case "PLUS2ROM":
					embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.ZX_plus2_rom));
					break;
				case "PLUS2AROM":
					embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.ZX_plus2a_rom));
					break;
				case "PLUS3ROM":
					byte[] r0 = Util.DecompressGzipFile(new MemoryStream(Resources.Spectrum3_V4_0_ROM0_bin));
					byte[] r1 = Util.DecompressGzipFile(new MemoryStream(Resources.Spectrum3_V4_0_ROM1_bin));
					byte[] r2 = Util.DecompressGzipFile(new MemoryStream(Resources.Spectrum3_V4_0_ROM2_bin));
					byte[] r3 = Util.DecompressGzipFile(new MemoryStream(Resources.Spectrum3_V4_0_ROM3_bin));
					embeddedRom = r0.Concat(r1).Concat(r2).Concat(r3).ToArray();
					break;
				default:
					embeddedFound = false;
					break;
			}

			if (embeddedFound)
				return embeddedRom;

			// Embedded ROM not found, maybe this is a peripheral ROM?
			var result = names.Select(n => CoreComm.CoreFileProvider.GetFirmware("ZXSpectrum", n, false)).FirstOrDefault(b => b != null && b.Length == length);
			if (result == null)
			{
				throw new MissingFirmwareException($"At least one of these firmwares is required: {string.Join(", ", names)}");
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
					//System.Windows.Forms.MessageBox.Show("+3 is not working at all yet :/");
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

		#region IRegionable

		public DisplayType Region => DisplayType.PAL;

		#endregion

		#region IDriveLight

		public bool DriveLightEnabled
		{
			get
			{
				return true;
			}
		}

		public bool DriveLightOn
		{
			get
			{
				if (_machine != null &&
					(_machine.TapeDevice != null && _machine.TapeDevice.TapeIsPlaying) ||
					(_machine.UPDDiskDevice != null && _machine.UPDDiskDevice.DriveLight))
					return true;

				return false;
			}
		}

		#endregion

	}
}
