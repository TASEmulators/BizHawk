using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Jellyfish.Virtu
{
	public interface ISpeaker
	{
		void ToggleOutput();
		void Clear();
		void GetSamples(out short[] samples, out int nSamp);
	}

	public sealed class Speaker : ISpeaker
	{
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private MachineEvents _events;

		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private ICpu _cpu;

		public Speaker() { }
		public Speaker(MachineEvents events, ICpu cpu)
		{
			_events = events;
			_cpu = cpu;
			_flushOutputEvent = FlushOutputEvent; // cache delegates; avoids garbage

			_events.AddEvent(CyclesPerFlush * _cpu.Multiplier, _flushOutputEvent);

			_isHigh = false;
			_highCycles = _totalCycles = 0;
		}

		private const int CyclesPerFlush = 23;

		private Action _flushOutputEvent;

		private bool _isHigh;
		private int _highCycles;
		private int _totalCycles;
		private long _lastCycles;

		[JsonIgnore] // only relevant if trying to savestate mid-frame
		private readonly short[] _buffer = new short[4096];

		[JsonIgnore] // only relevant if trying to savestate mid-frame
		private int _position;

		#region Api

		public void Clear()
		{
			_position = 0;
		}

		public void GetSamples(out short[] samples, out int nSamp)
		{
			samples = _buffer;
			nSamp = _position / 2;
			_position = 0;
		}

		#endregion

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

			_events.AddEvent(CyclesPerFlush * _cpu.Multiplier, _flushOutputEvent);
		}

		private void UpdateCycles()
		{
			int delta = (int)(_cpu.Cycles - _lastCycles);
			if (_isHigh)
			{
				_highCycles += delta;
			}
			_totalCycles += delta;
			_lastCycles = _cpu.Cycles;
		}

		private void Output(int data) // machine thread
		{
			data = (int)(data * 0.2);
			if (_position < _buffer.Length - 2)
			{
				_buffer[_position++] = (short)data;
				_buffer[_position++] = (short)data;
			}
		}


		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			_position = 0;
		}
	}
}
