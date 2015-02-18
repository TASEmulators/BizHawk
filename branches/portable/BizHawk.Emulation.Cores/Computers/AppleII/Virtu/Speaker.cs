using System;
using System.IO;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    public sealed class Speaker : MachineComponent
    {
        public Speaker(Machine machine) :
            base(machine)
        {
            _flushOutputEvent = FlushOutputEvent; // cache delegates; avoids garbage
        }

        public override void Initialize()
        {
            _audioService = Machine.Services.GetService<AudioService>();

            Volume = 0.5f;
            Machine.Events.AddEvent(CyclesPerFlush * Machine.Cpu.Multiplier, _flushOutputEvent);
        }

        public override void Reset()
        {
            _audioService.Reset();
            _isHigh = false;
            _highCycles = _totalCycles = 0;
        }

        public override void LoadState(BinaryReader reader, Version version)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            Volume = reader.ReadSingle();
        }

        public override void SaveState(BinaryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.Write(Volume);
        }

        public void ToggleOutput()
        {
            UpdateCycles();
            _isHigh ^= true;
        }

        private void FlushOutputEvent()
        {
            UpdateCycles();
            _audioService.Output(_highCycles * short.MaxValue / _totalCycles); // quick and dirty decimation
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

        public float Volume { get { return _volume; } set { _volume = value; _audioService.SetVolume(_volume); } }

        private const int CyclesPerFlush = 23;

        private Action _flushOutputEvent;

        private AudioService _audioService;

        private bool _isHigh;
        private int _highCycles;
        private int _totalCycles;
        private long _lastCycles;
        private float _volume;
    }
}
