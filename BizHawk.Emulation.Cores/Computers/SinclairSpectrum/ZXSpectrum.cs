using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;
using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;
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
            
            switch (Settings.MachineType)
            {
                case MachineType.ZXSpectrum48:
                    ControllerDefinition = ZXSpectrumControllerDefinition48;                    
                    Init(MachineType.ZXSpectrum48, Settings.BorderType, Settings.TapeLoadSpeed);
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

            ser.Register<ITraceable>(_tracer);
            ser.Register<IDisassemblable>(_cpu);
            ser.Register<IVideoProvider>(_machine);
            ser.Register<ISoundProvider>(_machine.BuzzerDevice);

            HardReset();

            

            List<string> romDis = new List<string>();
            List<DISA> disas = new List<DISA>();
            for (int i = 0x00; i < 0x4000; i++)
            {
                DISA d = new DISA();
                ushort size;
                d.Dis = _cpu.Disassemble((ushort)i, _machine.ReadMemory, out size);
                d.Size = size;
                disas.Add(d);
                romDis.Add(d.Dis);
                //i = i + size - 1;
                //romDis.Add(s);
            }
        }

        public class DISA
        {
            public ushort Size { get; set; }
            public string Dis { get; set; }
        }

        //private int _cyclesPerFrame;

        public Action HardReset;
        public Action SoftReset;

        private readonly Z80A _cpu;
        private byte[] _systemRom;
        private readonly TraceBuffer _tracer;
        public IController _controller;
        private SpectrumBase _machine;

        

        

        private byte[] GetFirmware(int length, params string[] names)
        {
            var result = names.Select(n => CoreComm.CoreFileProvider.GetFirmware("ZXSpectrum", n, false)).FirstOrDefault(b => b != null && b.Length == length);
            if (result == null)
            {
                throw new MissingFirmwareException($"At least one of these firmwares is required: {string.Join(", ", names)}");
            }

            return result;
        }


        private void Init(MachineType machineType, BorderType borderType, TapeLoadSpeed tapeLoadSpeed)
        {
            // setup the emulated model based on the MachineType
            switch (machineType)
            {
                case MachineType.ZXSpectrum48:
                    _machine = new ZX48(this, _cpu);
                    _systemRom = GetFirmware(0x4000, "48ROM");
                    _machine.FillMemory(_systemRom, 0);
                    break;
            }
        }

        #region IRegionable

        public DisplayType Region => DisplayType.PAL;

        #endregion


    }
}
