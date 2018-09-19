using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using BizHawk.Emulation.Cores.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPCHawk: Core Class
    /// * Main Initialization *
    /// </summary>
    [Core(
        "CPCHawk",
        "Asnivor",
        isPorted: false,
        isReleased: false)]
    public partial class AmstradCPC : IRegionable, IDriveLight
    {
        public AmstradCPC(CoreComm comm, IEnumerable<byte[]> files, List<GameInfo> game, object settings, object syncSettings)
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
                settings = new AmstradCPCSettings();
            if (syncSettings == null)
                syncSettings = new AmstradCPCSyncSettings();

            PutSyncSettings((AmstradCPCSyncSettings)syncSettings ?? new AmstradCPCSyncSettings());
            PutSettings((AmstradCPCSettings)settings ?? new AmstradCPCSettings());

            deterministicEmulation = ((AmstradCPCSyncSettings)syncSettings as AmstradCPCSyncSettings).DeterministicEmulation;

            switch (SyncSettings.MachineType)
            {
                case MachineType.CPC464:
                    ControllerDefinition = AmstradCPCControllerDefinition;
                    Init(MachineType.CPC464, _files, ((AmstradCPCSyncSettings)syncSettings as AmstradCPCSyncSettings).AutoStartStopTape,
                        ((AmstradCPCSyncSettings)syncSettings as AmstradCPCSyncSettings).BorderType);
                    break;
                case MachineType.CPC6128:
                    ControllerDefinition = AmstradCPCControllerDefinition;
                    Init(MachineType.CPC6128, _files, ((AmstradCPCSyncSettings)syncSettings as AmstradCPCSyncSettings).AutoStartStopTape, ((AmstradCPCSyncSettings)syncSettings as AmstradCPCSyncSettings).BorderType);
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
            _cpu.IRQACKCallback = _machine.GateArray.IORQA;
            //_cpu.OnExecFetch = _machine.CPUMon.OnExecFetch;

            ser.Register<ITraceable>(_tracer);
            ser.Register<IDisassemblable>(_cpu);
            ser.Register<IVideoProvider>(_machine.GateArray);

            // initialize sound mixer and attach the various ISoundProvider devices
            SoundMixer = new SoundProviderMixer((int)(32767 / 10), "Tape Audio", (ISoundProvider)_machine.TapeBuzzer);
            if (_machine.AYDevice != null)
                SoundMixer.AddSource(_machine.AYDevice, "AY-3-3912");

            // set audio device settings
            if (_machine.AYDevice != null && _machine.AYDevice.GetType() == typeof(AY38912))
            {
                ((AY38912)_machine.AYDevice as AY38912).PanningConfiguration = ((AmstradCPCSettings)settings as AmstradCPCSettings).AYPanConfig;
                _machine.AYDevice.Volume = ((AmstradCPCSettings)settings as AmstradCPCSettings).AYVolume;
            }

            if (_machine.TapeBuzzer != null)
            {
                ((Beeper)_machine.TapeBuzzer as Beeper).Volume = ((AmstradCPCSettings)settings as AmstradCPCSettings).TapeVolume;
            }

            ser.Register<ISoundProvider>(SoundMixer);

            HardReset();
            SetupMemoryDomains();
        }

        public Action HardReset;
        public Action SoftReset;

        private readonly Z80A _cpu;
        private readonly TraceBuffer _tracer;
        public IController _controller;
        public CPCBase _machine;

        public List<GameInfo> _gameInfo;
        public List<GameInfo> _tapeInfo = new List<GameInfo>();
        public List<GameInfo> _diskInfo = new List<GameInfo>();

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
                    embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.OS_464_ROM));
                    break;
                case "BASIC1-0ROM":
                    embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.CPC_BASIC_1_0_ROM));
                    break;

                // CPC 6128 ROMS
                case "OS6128ROM":
                    embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.CPC_OS_6128_ROM));
                    break;
                case "BASIC1-1ROM":
                    embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.CPC_BASIC_1_1_ROM));
                    break;
                case "AMSDOS0-5ROM":
                    embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.CPC_AMSDOS_0_5_ROM));
                    break;
                default:
                    embeddedFound = false;
                    break;
            }

            if (embeddedFound)
                return embeddedRom;

            // Embedded ROM not found, maybe this is a peripheral ROM?
            var result = names.Select(n => CoreComm.CoreFileProvider.GetFirmware("AmstradCPC", n, false)).FirstOrDefault(b => b != null && b.Length == length);
            if (result == null)
            {
                throw new MissingFirmwareException($"At least one of these firmwares is required: {string.Join(", ", names)}");
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
                    List<RomData> roms64 = new List<RomData>();
                    roms64.Add(RomData.InitROM(MachineType.CPC464, GetFirmware(0x4000, "OS464ROM"), RomData.ROMChipType.Lower));
                    roms64.Add(RomData.InitROM(MachineType.CPC464, GetFirmware(0x4000, "BASIC1-0ROM"), RomData.ROMChipType.Upper, 0));
                    _machine.InitROM(roms64.ToArray());
                    break;

                case MachineType.CPC6128:
                    _machine = new CPC6128(this, _cpu, files, autoTape, bType);
                    List<RomData> roms128 = new List<RomData>();
                    roms128.Add(RomData.InitROM(MachineType.CPC6128, GetFirmware(0x4000, "OS6128ROM"), RomData.ROMChipType.Lower));
                    roms128.Add(RomData.InitROM(MachineType.CPC6128, GetFirmware(0x4000, "BASIC1-1ROM"), RomData.ROMChipType.Upper, 0));
                    roms128.Add(RomData.InitROM(MachineType.CPC6128, GetFirmware(0x4000, "AMSDOS0-5ROM"), RomData.ROMChipType.Upper, 7));
                    _machine.InitROM(roms128.ToArray());
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
