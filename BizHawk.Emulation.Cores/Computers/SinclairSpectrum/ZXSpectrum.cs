using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;
using BizHawk.Emulation.Cores.Components.Z80A;
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
        public ZXSpectrum(CoreComm comm, byte[] file, object settings, object syncSettings)
        {
            PutSyncSettings((ZXSpectrumSyncSettings)syncSettings ?? new ZXSpectrumSyncSettings());
            PutSettings((ZXSpectrumSettings)settings ?? new ZXSpectrumSettings());

            var ser = new BasicServiceProvider(this);
            ServiceProvider = ser;    
            InputCallbacks = new InputCallbackSystem();

            CoreComm = comm;

            _cpu = new Z80A();

            _tracer = new TraceBuffer { Header = _cpu.TraceHeader };

            _file = file;

            switch (Settings.MachineType)
            {
                case MachineType.ZXSpectrum48:
                    ControllerDefinition = ZXSpectrumControllerDefinition;                    
                    Init(MachineType.ZXSpectrum48, Settings.BorderType, SyncSettings.TapeLoadSpeed, _file);
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
            ser.Register<IVideoProvider>(_machine);
            ser.Register<ISoundProvider>(_machine.BuzzerDevice);

            HardReset();

			SetupMemoryDomains();
        }
                
        public Action HardReset;
        public Action SoftReset;

        private readonly Z80A _cpu;
        private readonly TraceBuffer _tracer;
        public IController _controller;
        private SpectrumBase _machine;

        private byte[] _file;


        public bool DiagRom = false;

        private byte[] GetFirmware(int length, params string[] names)
        {
            if (DiagRom & File.Exists(Directory.GetCurrentDirectory() + @"\DiagROM.v28"))
            {
                var rom = File.ReadAllBytes(Directory.GetCurrentDirectory() + @"\DiagROM.v28");
                return rom;
            }

            var result = names.Select(n => CoreComm.CoreFileProvider.GetFirmware("ZXSpectrum", n, false)).FirstOrDefault(b => b != null && b.Length == length);
            if (result == null)
            {
                throw new MissingFirmwareException($"At least one of these firmwares is required: {string.Join(", ", names)}");
            }

            return result;
        }


        private void Init(MachineType machineType, BorderType borderType, TapeLoadSpeed tapeLoadSpeed, byte[] file)
        {
            // setup the emulated model based on the MachineType
            switch (machineType)
            {
                case MachineType.ZXSpectrum48:
                    _machine = new ZX48(this, _cpu, file);
                    var _systemRom = GetFirmware(0x4000, "48ROM");
                    var romData = RomData.InitROM(machineType, _systemRom);
                    _machine.InitROM(romData);
                    break;
            }
        }

        #region IRegionable

        public DisplayType Region => DisplayType.PAL;

        #endregion


    }
}
