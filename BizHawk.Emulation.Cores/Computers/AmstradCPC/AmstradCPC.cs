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
                    Init(MachineType.CPC464, _files, ((AmstradCPCSyncSettings)syncSettings as AmstradCPCSyncSettings).AutoStartStopTape);
                    break;
                case MachineType.CPC6128:
                    ControllerDefinition = AmstradCPCControllerDefinition;
                    Init(MachineType.CPC6128, _files, ((AmstradCPCSyncSettings)syncSettings as AmstradCPCSyncSettings).AutoStartStopTape);
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
            ser.Register<IVideoProvider>(_machine.CRT);

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
                case "464ROM":
                    embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.cpc464_rom));
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

        private void Init(MachineType machineType, List<byte[]> files, bool autoTape)
        {
            _machineType = machineType;

            // setup the emulated model based on the MachineType
            switch (machineType)
            {
                case MachineType.CPC464:
                    _machine = new CPC464(this, _cpu, files, autoTape);
                    var _systemRom16 = GetFirmware(0x4000, "464ROM");
                    var romData16 = RomData.InitROM(machineType, _systemRom16);
                    _machine.InitROM(romData16);
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
