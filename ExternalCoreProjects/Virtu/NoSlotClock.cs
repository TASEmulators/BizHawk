using System;
using System.IO;

namespace Jellyfish.Virtu
{
    public sealed class NoSlotClock : MachineComponent
    {
		public NoSlotClock() { }
        public NoSlotClock(Machine machine) :
            base(machine)
        {
        }

        public override void Initialize()
        {
            _clockEnabled = false;
            _writeEnabled = true;
            _clockRegister = new RingRegister(0x0, 0x1);
            _comparisonRegister = new RingRegister(ClockInitSequence, 0x1);
        }

        public int Read(int address, int data)
        {
            // this may read or write the clock
            if ((address & 0x4) != 0)
            {
                return ReadClock(data);
            }

            WriteClock(address);
            return data;
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

            data = _clockRegister.ReadBit(Machine.Video.ReadFloatingBus());
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

        private bool _clockEnabled;
        private bool _writeEnabled;
        private RingRegister _clockRegister;
        private RingRegister _comparisonRegister;

        private struct RingRegister
        {
            public RingRegister(ulong data, ulong mask)
            {
                _data = data;
                _mask = mask;
            }

            public void Reset()
            {
                _mask = 0x1;
            }

            public void WriteNibble(int data)
            {
                WriteBits(data, 4);
            }

            public void WriteBits(int data, int count)
            {
                for (int i = 1; i <= count; i++)
                {
                    WriteBit(data);
                    NextBit();
                    data >>= 1;
                }
            }

            public void WriteBit(int data)
            {
                _data = ((data & 0x1) != 0) ? (_data | _mask) : (_data & ~_mask);
            }

            public int ReadBit(int data)
            {
                return ((_data & _mask) != 0) ? (data | 0x1) : (data & ~0x1);
            }

            public bool CompareBit(int data)
            {
                return (((_data & _mask) != 0) == ((data & 0x1) != 0));
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

            public ulong Data { get { return _data; } } // no auto props
            public ulong Mask { get { return _mask; } }

            private ulong _data;
            private ulong _mask;
        }
    }
}
