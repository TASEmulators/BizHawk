using System;
using System.Globalization;
using System.IO;

namespace BizHawk.Emulation.Consoles.Atari
{
	// Emulates the M6532 RIOT Chip
	public partial class M6532
	{
		Atari2600 core;

		public int timerCyclesRemaining = 0;
		public int timerShift = 0;

		bool interruptEnabled = false;
		bool interruptFlag = false;

		public byte ddra = 0x00;
		public byte ddrb = 0x00;

		public bool resetOccured = false;

		public M6532(Atari2600 core)
		{
			this.core = core;

			// Apparently starting the timer at 0 will break for some games (Solaris and H.E.R.O.). We shall see
			timerCyclesRemaining = 0;
			interruptEnabled = false;
			interruptFlag = false;
		}

		public void tick()
		{
			timerCyclesRemaining--;
			if (timerCyclesRemaining == 0 && interruptEnabled)
			{
				interruptFlag = true;
			}
		}

		public byte ReadMemory(ushort addr)
		{
			// Register Select (?)
			bool RS = (addr & 0x0200) != 0;

			if (!RS)
			{
				// Read Ram
				ushort maskedAddr = (ushort)(addr & 0x007f);
				return core.ram[maskedAddr];
			}
			else
			{
				ushort registerAddr = (ushort)(addr & 0x0007);
				if (registerAddr == 0x00)
				{
					// Read Output reg A
					// Combine readings from player 1 and player 2
					byte temp = (byte)(core.ReadControls1() & 0xF0 | ((core.ReadControls2() >> 4) & 0x0F));
					temp = (byte)(temp & ~ddra);
					return temp;
				}
				else if (registerAddr == 0x01)
				{
					// Read DDRA
					return ddra;
				}
				else if (registerAddr == 0x02)
				{
					// Read Output reg B
					byte temp = core.ReadConsoleSwitches();
					temp = (byte)(temp & ~ddrb);
					return temp;

					/*
					// TODO: Rewrite this!
					bool temp = resetOccured;
					resetOccured = false;
					return (byte)(0x0A | (temp ? 0x00 : 0x01));
					 * */
				}
				else if (registerAddr == 0x03)
				{
					// Read DDRB
					return ddrb;
				}
				else if ((registerAddr & 0x5) == 0x4)
				{
					// Bit 0x0080 contains interrupt enable/disable
					interruptEnabled = (addr & 0x0080) != 0;

					// The interrupt flag will be reset whenever the Timer is access by a read or a write
					// However, the reading of the timer at the same time the interrupt occurs will not reset the interrupt flag
					// (M6532 Datasheet)
					if (timerCyclesRemaining != 0)
					{
						interruptFlag = false;
					}	

					// If there is still time on the timer (or its 0), return the lowest byte
					if (timerCyclesRemaining >= 0)
					{
						return (byte)(((timerCyclesRemaining) >> timerShift) & 0xFF);
					}
					else
					{
						return (byte)(timerCyclesRemaining & 0xFF);
					}

				}
				else if ((registerAddr & 0x5) == 0x5)
				{
					// Read interrupt flag
					if (interruptEnabled && interruptFlag)
					{
						return 0x00;
					}
					else
					{
						return 0x80;
					}
				}
			}

			return 0x3A;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			// Register Select (?)
			bool RS = (addr & 0x0200) != 0;

			// If the RS bit is not set, this is a ram write
			if (!RS)
			{
				ushort maskedAddr = (ushort)(addr & 0x007f);
				core.ram[maskedAddr] = value;
			}
			else
			{
				// If bit 0x0010 is set, and bit 0x0004 is set, this is a timer write
				if ((addr & 0x0014) == 0x0014)
				{
					ushort registerAddr = (ushort)(addr & 0x0007);

					// Bit 0x0080 contains interrupt enable/disable
					interruptEnabled = (addr & 0x0080) != 0;

					// The interrupt flag will be reset whenever the Timer is access by a read or a write
					// (M6532 datasheet)

					if (registerAddr == 0x04)
					{
						// Write to Timer/1
						timerShift = 0;
						timerCyclesRemaining = value << timerShift;
						interruptFlag = false;
					}
					else if (registerAddr == 0x05)
					{
						// Write to Timer/8
						timerShift = 3;
						timerCyclesRemaining = value << timerShift;
						interruptFlag = false;
					}
					else if (registerAddr == 0x06)
					{
						// Write to Timer/64
						timerShift = 6;
						timerCyclesRemaining = value << timerShift;
						interruptFlag = false;
					}
					else if (registerAddr == 0x07)
					{
						// Write to Timer/1024
						timerShift = 10;
						timerCyclesRemaining = value << timerShift;
						interruptFlag = false;
					}
				}
				// If bit 0x0004 is not set, bit 0x0010 is ignored and
				// these are register writes
				else if ((addr & 0x0004) == 0)
				{
					ushort registerAddr = (ushort)(addr & 0x0007);

					if (registerAddr == 0x00)
					{
						// Write Output reg A
					}
					else if (registerAddr == 0x01)
					{
						// Write DDRA
						ddra = value;
					}
					else if (registerAddr == 0x02)
					{
						// Write Output reg B
					}
					else if (registerAddr == 0x03)
					{
						// Write DDRB
						ddrb = value;
					}
				}
			}
		}
	}
}