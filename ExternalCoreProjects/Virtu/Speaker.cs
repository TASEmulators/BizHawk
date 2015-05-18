using System;
using System.IO;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    public sealed class Speaker : MachineComponent
    {
		public Speaker() { }
        public Speaker(Machine machine) :
            base(machine)
        {
            _flushOutputEvent = FlushOutputEvent; // cache delegates; avoids garbage
        }

        public override void Initialize()
        {
			AudioService = new Services.AudioService();

            Machine.Events.AddEvent(CyclesPerFlush * Machine.Cpu.Multiplier, _flushOutputEvent);
        }

        public override void Reset()
        {
            _isHigh = false;
            _highCycles = _totalCycles = 0;
        }

        public void ToggleOutput()
        {
            UpdateCycles();
            _isHigh ^= true;
        }

        private void FlushOutputEvent()
        {
            UpdateCycles();
			// TODO: better than simple decimation here!!
            AudioService.Output(_highCycles * short.MaxValue / _totalCycles);
            _highCycles = _totalCycles = 0;

            Machine.Events.AddEvent(CyclesPerFlush * Machine.Cpu.Multiplier, _flushOutputEvent);
        }

        private void UpdateCycles()
        {
            int delta = (int)(Machine.Cpu.Cycles - _lastCycles);
            if (_isHigh)
            {
                _highCycles += delta;
            }
            _totalCycles += delta;
            _lastCycles = Machine.Cpu.Cycles;
        }

        private const int CyclesPerFlush = 23;

        private Action _flushOutputEvent;

        private bool _isHigh;
        private int _highCycles;
        private int _totalCycles;
        private long _lastCycles;

		public AudioService AudioService { get; private set; }
    }
}
