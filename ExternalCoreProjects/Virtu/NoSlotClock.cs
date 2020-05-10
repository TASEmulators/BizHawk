using System;

namespace Jellyfish.Virtu
{
	public interface ISlotClock
	{
		int Read(int address, int value);
		void Write(int address);

		// ReSharper disable once UnusedMember.Global
		void Sync(IComponentSerializer ser);
	}

	// ReSharper disable once UnusedMember.Global
	public sealed class NoSlotClock : ISlotClock
	{
		private readonly Video _video;

		private bool _clockEnabled;
		private bool _writeEnabled;
		private RingRegister _clockRegister;
		private RingRegister _comparisonRegister;

		public NoSlotClock(Video video)
		{
			_video = video;

			_clockEnabled = false;
			_writeEnabled = true;
			_clockRegister = new RingRegister(0x0, 0x1);
			_comparisonRegister = new RingRegister(ClockInitSequence, 0x1);
		}

		public void Sync(IComponentSerializer ser)
		{
			ser.Sync(nameof(_clockEnabled), ref _clockEnabled);
			ser.Sync(nameof(_writeEnabled), ref _writeEnabled);
			_clockRegister.Sync(ser);
			_comparisonRegister.Sync(ser);
		}

		public int Read(int address, int value)
		{
			// this may read or write the clock
			if ((address & 0x4) != 0)
			{
				return ReadClock(value);
			}

			WriteClock(address);
			return value;
		}

		public void Write(int address)
		{
			// this may read or write the clock
			if ((address & 0x4) != 0)
			{
				ReadClock(0);
			}
			else
			{
				WriteClock(address);
			}
		}

		private int ReadClock(int data)
		{
			// for a ROM, A2 high = read, and data out (if any) is on D0
			if (!_clockEnabled)
			{
				_comparisonRegister.Reset();
				_writeEnabled = true;
				return data;
			}

			data = _clockRegister.ReadBit(_video.ReadFloatingBus());
			if (_clockRegister.NextBit())
			{
				_clockEnabled = false;
			}

			return data;
		}

		private void WriteClock(int address)
		{
			// for a ROM, A2 low = write, and data in is on A0
			if (!_writeEnabled)
			{
				return;
			}

			if (!_clockEnabled)
			{
				if ((_comparisonRegister.CompareBit(address)))
				{
					if (_comparisonRegister.NextBit())
					{
						_clockEnabled = true;
						PopulateClockRegister();
					}
				}
				else
				{
					// mismatch ignores further writes
					_writeEnabled = false;
				}
			}
			else if (_clockRegister.NextBit())
			{
				// simulate writes, but our clock register is read-only
				_clockEnabled = false;
			}
		}

		private void PopulateClockRegister()
		{
			// all values are in packed BCD format (4 bits per decimal digit)
			var now = DateTime.Now;

			int centisecond = now.Millisecond / 10; // 00-99
			_clockRegister.WriteNibble(centisecond % 10);
			_clockRegister.WriteNibble(centisecond / 10);

			int second = now.Second; // 00-59
			_clockRegister.WriteNibble(second % 10);
			_clockRegister.WriteNibble(second / 10);

			int minute = now.Minute; // 00-59
			_clockRegister.WriteNibble(minute % 10);
			_clockRegister.WriteNibble(minute / 10);

			int hour = now.Hour; // 01-23
			_clockRegister.WriteNibble(hour % 10);
			_clockRegister.WriteNibble(hour / 10);

			int day = (int)now.DayOfWeek + 1; // 01-07 (1 = Sunday)
			_clockRegister.WriteNibble(day % 10);
			_clockRegister.WriteNibble(day / 10);

			int date = now.Day; // 01-31
			_clockRegister.WriteNibble(date % 10);
			_clockRegister.WriteNibble(date / 10);

			int month = now.Month; // 01-12
			_clockRegister.WriteNibble(month % 10);
			_clockRegister.WriteNibble(month / 10);

			int year = now.Year % 100; // 00-99
			_clockRegister.WriteNibble(year % 10);
			_clockRegister.WriteNibble(year / 10);
		}

		private const ulong ClockInitSequence = 0x5CA33AC55CA33AC5;

		private struct RingRegister
		{
			public RingRegister(ulong data, ulong mask)
			{
				_data = data;
				_mask = mask;
			}

			public void Sync(IComponentSerializer ser)
			{
				ser.Sync(nameof(_data), ref _data);
				ser.Sync(nameof(_mask), ref _mask);
			}

			public void Reset()
			{
				_mask = 0x1;
			}

			public void WriteNibble(int data)
			{
				WriteBits(data, 4);
			}

			private void WriteBits(int data, int count)
			{
				for (int i = 1; i <= count; i++)
				{
					WriteBit(data);
					NextBit();
					data >>= 1;
				}
			}

			private void WriteBit(int data)
			{
				_data = (data & 0x1) != 0
					? _data | _mask
					: _data & ~_mask;
			}

			public int ReadBit(int data)
			{
				return (_data & _mask) != 0
					? data | 0x1
					: data & ~0x1;
			}

			public bool CompareBit(int data)
			{
				return (_data & _mask) != 0 == ((data & 0x1) != 0);
			}

			public bool NextBit()
			{
				if ((_mask <<= 1) == 0)
				{
					_mask = 0x1;
					return true; // wrap
				}

				return false;
			}

			private ulong _data;
			private ulong _mask;
		}
	}
}
