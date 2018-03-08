using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;
using BizHawk.Emulation.Cores.Components.Z80A;
using BizHawk.Emulation.Cores.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    [Core(
        "ZXHawk",
        "Asnivor",
        isPorted: false,
        isReleased: false)]
    [ServiceNotApplicable(typeof(IDriveLight))]
    public partial class ZXSpectrum : IDebuggable, IInputPollable, IStatable, IRegionable
    {
        [CoreConstructor("ZXSpectrum")]
        public ZXSpectrum(CoreComm comm, IEnumerable<byte[]> files, List<GameInfo> game, object settings, object syncSettings)
        {            
            var ser = new BasicServiceProvider(this);
            ServiceProvider = ser;    
            InputCallbacks = new InputCallbackSystem();
            MemoryCallbacks = new MemoryCallbackSystem(new[] { "System Bus" });

            CoreComm = comm;

            _gameInfo = game;

            _cpu = new Z80A();

            _tracer = new TraceBuffer { Header = _cpu.TraceHeader };

            //_file = file;
            _files = files?.ToList() ?? new List<byte[]>();

            if (settings == null)
                settings = new ZXSpectrumSettings();
            if (syncSettings == null)
                syncSettings = new ZXSpectrumSyncSettings();

            PutSyncSettings((ZXSpectrumSyncSettings)syncSettings ?? new ZXSpectrumSyncSettings());
            PutSettings((ZXSpectrumSettings)settings ?? new ZXSpectrumSettings());

            List <JoystickType> joysticks = new List<JoystickType>();
            joysticks.Add(((ZXSpectrumSyncSettings)syncSettings as ZXSpectrumSyncSettings).JoystickType1);
            joysticks.Add(((ZXSpectrumSyncSettings)syncSettings as ZXSpectrumSyncSettings).JoystickType2);
            joysticks.Add(((ZXSpectrumSyncSettings)syncSettings as ZXSpectrumSyncSettings).JoystickType3);

            deterministicEmulation = ((ZXSpectrumSyncSettings)syncSettings as ZXSpectrumSyncSettings).DeterministicEmulation;

            switch (SyncSettings.MachineType)
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
                case MachineType.ZXSpectrum128Plus3:
                    ControllerDefinition = ZXSpectrumControllerDefinition;
                    Init(MachineType.ZXSpectrum128Plus3, SyncSettings.BorderType, SyncSettings.TapeLoadSpeed, _files, joysticks);
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

            ser.Register<ITraceable>(_tracer);
            ser.Register<IDisassemblable>(_cpu);
            ser.Register<IVideoProvider>(_machine.ULADevice);

            SoundMixer = new SoundProviderMixer((ISoundProvider)_machine.BuzzerDevice);
            if (_machine.AYDevice != null)
                SoundMixer.AddSource(_machine.AYDevice);

            //dcf = new DCFilter(SoundMixer, 256);
            ser.Register<ISoundProvider>(SoundMixer);

            

            HardReset();

			SetupMemoryDomains();
        }
                
        public Action HardReset;
        public Action SoftReset;

        private readonly Z80A _cpu;
        private readonly TraceBuffer _tracer;
        public IController _controller;
        public SpectrumBase _machine;

        private List<GameInfo> _gameInfo;

        private SoundProviderMixer SoundMixer;

        private DCFilter dcf;

        //private byte[] _file;
        private readonly List<byte[]> _files;

        public bool DiagRom = true;

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
                case "PLUS3ROM":
                    embeddedRom = Util.DecompressGzipFile(new MemoryStream(Resources.ZX_plus3_rom));
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


        private void Init(MachineType machineType, BorderType borderType, TapeLoadSpeed tapeLoadSpeed, List<byte[]> files, List<JoystickType> joys)
        {
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
                case MachineType.ZXSpectrum128Plus3:
                    _machine = new ZX128Plus3(this, _cpu, borderType, files, joys);
                    var _systemRomP3 = GetFirmware(0x10000, "PLUS3ROM");
                    var romDataP3 = RomData.InitROM(machineType, _systemRomP3);
                    _machine.InitROM(romDataP3);
                    //System.Windows.Forms.MessageBox.Show("+3 is not working at all yet :/");
                    break;
            }
        }

        #region IRegionable

        public DisplayType Region => DisplayType.PAL;

        #endregion


    }
}
