using System;

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
			Output(_highCycles * short.MaxValue / _totalCycles);
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


		private void Output(int data) // machine thread
		{
			data = (int)(data * 0.2);
			if (pos < buff.Length - 2)
			{
				buff[pos++] = (short)data;
				buff[pos++] = (short)data;
			}
		}

		[Newtonsoft.Json.JsonIgnore] // only relevant if trying to savestate midframe
		private readonly short[] buff = new short[4096];
		[Newtonsoft.Json.JsonIgnore] // only relevant if trying to savestate midframe
		private int pos;

		[System.Runtime.Serialization.OnDeserialized]
		public void OnDeserialized(System.Runtime.Serialization.StreamingContext context)
		{
			pos = 0;
		}

		public void Clear()
		{
			pos = 0;
		}

		public void GetSamples(out short[] samples, out int nSamp)
		{
			samples = buff;
			nSamp = pos / 2;
			pos = 0;
		}
	}
}
