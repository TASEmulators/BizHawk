using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CPCHawk: Core Class
	/// * Main Initialization *
	/// </summary>
	[Core(CoreNames.CPCHawk, "Asnivor", isReleased: false)]
	public partial class AmstradCPC : IRegionable, IDriveLight
	{
		[CoreConstructor(VSystemID.Raw.AmstradCPC)]
		public AmstradCPC(CoreLoadParameters<AmstradCPCSettings, AmstradCPCSyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			CoreComm = lp.Comm;
			_gameInfo = lp.Roms.Select(r => r.Game).ToList();
			//_cpu = new Z80A<CpuLink>(default);
			_cpu = new LibFz80Wrapper();
			_tracer = new TraceBuffer(_cpu.TraceHeader);
			_files = lp.Roms.Select(r => r.RomData).ToList();

			var settings = lp.Settings ?? new AmstradCPCSettings();
			var syncSettings = lp.SyncSettings ?? new AmstradCPCSyncSettings();

			PutSyncSettings(syncSettings);
			PutSettings(settings);

			DeterministicEmulation = syncSettings.DeterministicEmulation;

			switch (SyncSettings.MachineType)
			{
				case MachineType.CPC464:
					ControllerDefinition = AmstradCPCControllerDefinition;
					Init(MachineType.CPC464, _files, syncSettings.AutoStartStopTape,
						syncSettings.BorderType);
					break;
				case MachineType.CPC6128:
					ControllerDefinition = AmstradCPCControllerDefinition;
					Init(MachineType.CPC6128, _files, syncSettings.AutoStartStopTape, syncSettings.BorderType);
					break;
				default:
					throw new InvalidOperationException("Machine not yet emulated");
			}

			HardReset = _machine.HardReset;
			SoftReset = _machine.SoftReset;

			//_cpu.SetCpuLink(new CpuLink(_machine));

			_cpu.ReadMemory = _machine.ReadMemory;
			_cpu.WriteMemory = _machine.WriteMemory;
			_cpu.ReadPort = _machine.ReadPort;
			_cpu.WritePort = _machine.WritePort;
			//_cpu.OnExecFetch = _machine.OnExecFetch;


			ser.Register<ITraceable>(_tracer);
			//ser.Register<IDisassemblable>(_cpu);
			ser.Register<IVideoProvider>(_machine.CRTScreen);
			ser.Register<IStatable>(new StateSerializer(SyncState));

			// initialize sound mixer and attach the various ISoundProvider devices
			SoundMixer = new SoundProviderMixer(32767 / 10, "Tape Audio", (ISoundProvider)_machine.TapeBuzzer);
			if (_machine.AYDevice != null)
				SoundMixer.AddSource(_machine.AYDevice, "AY-3-3912");

			// set audio device settings
			if (_machine.AYDevice != null && _machine.AYDevice.GetType() == typeof(AY38912))
			{
				((AY38912)_machine.AYDevice).PanningConfiguration = settings.AYPanConfig;
				_machine.AYDevice.Volume = settings.AYVolume;
			}

			if (_machine.TapeBuzzer != null)
			{
				((Beeper)_machine.TapeBuzzer).Volume = settings.TapeVolume;
			}

			ser.Register<ISoundProvider>(SoundMixer);

			HardReset();
			SetupMemoryDomains();
		}

		internal CoreComm CoreComm { get; }

		public Action HardReset;
		public Action SoftReset;

		//private readonly Z80A<CpuLink> _cpu;
		private readonly LibFz80Wrapper _cpu;
		private readonly TraceBuffer _tracer;
		public IController _controller;
		public CPCBase _machine;

		public List<GameInfo> _gameInfo;
		public readonly IList<GameInfo> _tapeInfo = new List<GameInfo>();
		public readonly IList<GameInfo> _diskInfo = new List<GameInfo>();

		private SoundProviderMixer SoundMixer;

		private readonly List<byte[]> _files;

		private byte[] GetFirmware(int length, params string[] names)
		{
			// Amstrad licensed ROMs are free to distribute and shipped with BizHawk
			byte[] embeddedRom = new byte[length];
			bool embeddedFound = true;
			switch (names.FirstOrDefault())
			{
				// CPC 464 ROMS
				case "OS464ROM":
					embeddedRom = Zstd.DecompressZstdStream(new MemoryStream(Resources.OS_464_ROM.Value)).ToArray();
					break;
				case "BASIC1-0ROM":
					embeddedRom = Zstd.DecompressZstdStream(new MemoryStream(Resources.CPC_BASIC_1_0_ROM.Value)).ToArray();
					break;

				// CPC 6128 ROMS
				case "OS6128ROM":
					embeddedRom = Zstd.DecompressZstdStream(new MemoryStream(Resources.CPC_OS_6128_ROM.Value)).ToArray();
					break;
				case "BASIC1-1ROM":
					embeddedRom = Zstd.DecompressZstdStream(new MemoryStream(Resources.CPC_BASIC_1_1_ROM.Value)).ToArray();
					break;
				case "AMSDOS0-5ROM":
					embeddedRom = Zstd.DecompressZstdStream(new MemoryStream(Resources.CPC_AMSDOS_0_5_ROM.Value)).ToArray();
					break;
				default:
					embeddedFound = false;
					break;
			}

			if (embeddedFound)
				return embeddedRom;

			// Embedded ROM not found, maybe this is a peripheral ROM?
			var result = names.Select(n => CoreComm.CoreFileProvider.GetFirmware(new("AmstradCPC", n))).FirstOrDefault(b => b != null && b.Length == length);
			if (result == null)
			{
				throw new MissingFirmwareException($"At least one of these firmware options is required: {string.Join(", ", names)}");
			}

			return result;
		}

		private MachineType _machineType;

		private void Init(MachineType machineType, List<byte[]> files, bool autoTape, BorderType bType)
		{
			_machineType = machineType;

			// setup the emulated model based on the MachineType
			switch (machineType)
			{
				case MachineType.CPC464:
					_machine = new CPC464(this, _cpu, files, autoTape, bType);
					var roms64 = new List<RomData>();
					roms64.Add(RomData.InitROM(MachineType.CPC464, GetFirmware(0x4000, "OS464ROM"), RomData.ROMChipType.Lower));
					roms64.Add(RomData.InitROM(MachineType.CPC464, GetFirmware(0x4000, "BASIC1-0ROM"), RomData.ROMChipType.Upper, 0));
					_machine.InitROM(roms64.ToArray());
					break;

				case MachineType.CPC6128:
					_machine = new CPC6128(this, _cpu, files, autoTape, bType);
					var roms128 = new List<RomData>();
					roms128.Add(RomData.InitROM(MachineType.CPC6128, GetFirmware(0x4000, "OS6128ROM"), RomData.ROMChipType.Lower));
					roms128.Add(RomData.InitROM(MachineType.CPC6128, GetFirmware(0x4000, "BASIC1-1ROM"), RomData.ROMChipType.Upper, 0));
					roms128.Add(RomData.InitROM(MachineType.CPC6128, GetFirmware(0x4000, "AMSDOS0-5ROM"), RomData.ROMChipType.Upper, 7));
					_machine.InitROM(roms128.ToArray());
					break;
			}
		}


		public DisplayType Region => DisplayType.PAL;

		public bool DriveLightEnabled => true;

		public bool DriveLightOn =>
			(_machine?.TapeDevice != null && _machine.TapeDevice.TapeIsPlaying)
			|| (_machine?.UPDDiskDevice != null && _machine.UPDDiskDevice.DriveLight);

		public string DriveLightIconDescription => "Disc Drive Activity";
	}
}
