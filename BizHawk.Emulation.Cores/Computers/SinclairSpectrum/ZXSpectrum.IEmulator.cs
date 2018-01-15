using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZXSpectrum : IEmulator
    {
        public IEmulatorServiceProvider ServiceProvider { get; }

        public ControllerDefinition ControllerDefinition { get; set; }

        public void FrameAdvance(IController controller, bool render, bool renderSound)
        {            
            _controller = controller;

            if (_tracer.Enabled)
            {
                _cpu.TraceCallback = s => _tracer.Put(s);
            }
            else
            {
                _cpu.TraceCallback = null;
            }

            _machine.ExecuteFrame();
        }

        public int Frame => _machine.FrameCount;

        public string SystemId => "ZXSpectrum";

        public bool DeterministicEmulation => true;

        public void ResetCounters()
        {
            _machine.FrameCount = 0;
            _lagCount = 0;
            _isLag = false;
        }

        public CoreComm CoreComm { get; }

        public void Dispose()
        {
            if (_machine != null)
            {
                _machine = null;
            }
        }
    }
}
