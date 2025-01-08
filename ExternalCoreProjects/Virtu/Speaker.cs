namespace Jellyfish.Virtu
{
	public interface ISpeaker
	{
		void ToggleOutput();

		// ReSharper disable once UnusedMember.Global
		void Clear();

		// ReSharper disable once UnusedMember.Global
		void GetSamples(out short[] samples, out int nSamp);

		// ReSharper disable once UnusedMember.Global
		void Sync(IComponentSerializer ser);
	}

	// ReSharper disable once UnusedMember.Global
	public sealed class Speaker : ISpeaker
	{
		private const int CyclesPerFlush = 23;

		private readonly MachineEvents _events;
		private readonly ICpu _cpu;

		private bool _isHigh;
		private int _highCycles;
		private int _totalCycles;
		private long _lastCycles;

		// only relevant if trying to savestate mid-frame
		private readonly short[] _buffer = new short[4096];
		private int _position;

		public Speaker(MachineEvents events, ICpu cpu)
		{
			_events = events;
			_cpu = cpu;
			_events.AddEventDelegate(EventCallbacks.FlushOutput, FlushOutputEvent);
			_events.AddEvent(CyclesPerFlush * _cpu.Multiplier, EventCallbacks.FlushOutput);

			_isHigh = false;
			_highCycles = _totalCycles = 0;
		}

		public void Sync(IComponentSerializer ser)
		{
			ser.Sync(nameof(_isHigh), ref _isHigh);
			ser.Sync(nameof(_highCycles), ref _highCycles);
			ser.Sync(nameof(_totalCycles), ref _totalCycles);
			ser.Sync(nameof(_lastCycles), ref _lastCycles);
		}

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

		public void ToggleOutput()
		{
			UpdateCycles();
			_isHigh = !_isHigh;
		}

		private void FlushOutputEvent()
		{
			UpdateCycles();
			// TODO: better than simple decimation here!!
			Output(_highCycles * short.MaxValue / _totalCycles);
			_highCycles = _totalCycles = 0;

			_events.AddEvent(CyclesPerFlush * _cpu.Multiplier, EventCallbacks.FlushOutput);
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
	}
}
