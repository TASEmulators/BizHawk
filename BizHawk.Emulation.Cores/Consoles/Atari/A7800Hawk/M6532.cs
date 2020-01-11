using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Emulates the M6532 RIOT Chip
	public class M6532
	{
		public A7800Hawk Core { get; set; }

		public byte _ddRa = 0x00;
		public byte _ddRb = 0x00;
		public byte _outputA = 0x00;
		public byte _outputB = 0x00;

		public TimerData Timer;

		public M6532()
		{
			// arbitrary value to start with.
			Timer.Value = 0x73;
			Timer.PrescalerShift = 10;
			Timer.PrescalerCount = 1 << Timer.PrescalerShift;
		}

		public byte ReadMemory(ushort addr, bool peek)
		{
			if ((addr & 0x0200) == 0) // If not register select, read Ram
			{
				//return _core.Ram[(ushort)(addr & 0x007f)];
				return 0; 
			}

			var registerAddr = (ushort)(addr & 0x0007);
			if (registerAddr == 0x00)
			{
				Core._isLag = false;

				// Read Output reg A
				// Combine readings from player 1 and player 2
				// actually depends on setting in SWCHCNTA (aka DDRa)
				byte temp = (byte)(Core.p1_state | Core.p2_state);
				temp = (byte)(temp & ~_ddRa);
				temp = (byte)(temp + (_outputA & _ddRa));
				return temp;
			}
			
			if (registerAddr == 0x01)
			{
				// Read DDRA
				return _ddRa;
			}
			
			if (registerAddr == 0x02)
			{
				Core._isLag = false;

				// Read Output reg B
				byte temp = Core.con_state;
				temp = (byte)(temp & ~_ddRb);
				temp = (byte)(temp + (_outputB & _ddRb));
				return temp;
			}

			if (registerAddr == 0x03) // Read DDRB
			{
				return _ddRb;
			}
			
			if ((registerAddr & 0x5) == 0x4)
			{
				// Bit 0x0080 contains interrupt enable/disable
				Timer.InterruptEnabled = (addr & 0x0080) != 0;

				// The interrupt flag will be reset whenever the Timer is access by a read or a write
				// However, the reading of the timer at the same time the interrupt occurs will not reset the interrupt flag
				// (M6532 Datasheet)
				if (!(Timer.PrescalerCount == 0 && Timer.Value == 0))
				{
					Timer.InterruptFlag = false;
				}

				if (Timer.Overflowed)
				{
					Timer.Overflowed = false;
				}

				return Timer.Value;
			}

			// TODO: fix this to match real behaviour
			// This is an undocumented instruction whose behaviour is more dynamic then indicated here
			if ((registerAddr & 0x5) == 0x5)
			{
				// Read interrupt flag
				if (Timer.InterruptFlag) // Timer.InterruptEnabled && )
				{
					return 0xC0;
				}

				return 0x00;
			}

			return 0x3A;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if ((addr & 0x0200) == 0) // If the RS bit is not set, this is a ram write
			{
				//_core.Ram[(ushort)(addr & 0x007f)] = value;
			}
			else
			{
				// If bit 0x0010 is set, and bit 0x0004 is set, this is a timer write
				if ((addr & 0x0014) == 0x0014)
				{
					var registerAddr = (ushort)(addr & 0x0007);

					// Bit 0x0080 contains interrupt enable/disable
					Timer.InterruptEnabled = (addr & 0x0080) != 0;

					// The interrupt flag will be reset whenever the Timer is access by a read or a write
					// (M6532 datasheet)
					if (registerAddr == 0x04)
					{
						// Write to Timer/1
						Timer.PrescalerShift = 0;
						Timer.Value = value;
						Timer.PrescalerCount = 1; // << Timer.PrescalerShift;
						Timer.InterruptFlag = false;
					}
					else if (registerAddr == 0x05)
					{
						// Write to Timer/8
						Timer.PrescalerShift = 3;
						Timer.Value = value;
						Timer.PrescalerCount = 1; // << Timer.PrescalerShift;
						Timer.InterruptFlag = false;
					}
					else if (registerAddr == 0x06)
					{
						// Write to Timer/64
						Timer.PrescalerShift = 6;
						Timer.Value = value;
						Timer.PrescalerCount = 1; // << Timer.PrescalerShift;
						Timer.InterruptFlag = false;
					}
					else if (registerAddr == 0x07)
					{
						// Write to Timer/1024
						Timer.PrescalerShift = 10;
						Timer.Value = value;
						Timer.PrescalerCount = 1; // << Timer.PrescalerShift;
						Timer.InterruptFlag = false;
					}
				}

				// If bit 0x0004 is not set, bit 0x0010 is ignored and
				// these are register writes
				else if ((addr & 0x0004) == 0)
				{
					var registerAddr = (ushort)(addr & 0x0007);

					if (registerAddr == 0x00)
					{
						// Write Output reg A
						_outputA = value;
					}
					else if (registerAddr == 0x01)
					{
						// Write DDRA
						_ddRa = value;
					}
					else if (registerAddr == 0x02)
					{
						// Write Output reg B
						_outputB = value;
					}
					else if (registerAddr == 0x03)
					{
						// Write DDRB
						_ddRb = value;
					}
				}
			}
		}

		public void Reset()
		{
			// arbitrary value to start with.
			Timer.Value = 0x73;
			Timer.PrescalerShift = 10;
			Timer.PrescalerCount = 1 << Timer.PrescalerShift;

			_ddRa = 0x00;
			_ddRb = 0x00;
			_outputA = 0x00;
			_outputB = 0x00;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(M6532));
			ser.Sync("ddra", ref _ddRa);
			ser.Sync("ddrb", ref _ddRb);
			ser.Sync("OutputA", ref _outputA);
			ser.Sync("OutputB", ref _outputB);
			Timer.SyncState(ser);
			ser.EndSection();
		}

		public struct TimerData
		{
			public int PrescalerCount;
			public byte PrescalerShift;

			public byte Value;

			public bool InterruptEnabled;
			public bool InterruptFlag;
			public bool Overflowed;

			public void Tick()
			{
				PrescalerCount--;

				if ((PrescalerCount == 0) || Overflowed)
				{
					Value--;

					if (Value == 0xFF)
					{
						Overflowed = true;
						InterruptFlag = true;
					}

					if (PrescalerCount == 0)
					{
						PrescalerCount = 1 << PrescalerShift;
					}
				}
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("prescalerCount", ref PrescalerCount);
				ser.Sync("prescalerShift", ref PrescalerShift);
				ser.Sync("value", ref Value);
				ser.Sync("interruptEnabled", ref InterruptEnabled);
				ser.Sync("interruptFlag", ref InterruptFlag);
				ser.Sync("Overflowed", ref Overflowed);
			}
		}
	}
}