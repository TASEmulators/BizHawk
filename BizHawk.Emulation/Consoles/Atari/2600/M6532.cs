using System;
using System.Globalization;
using System.IO;
using BizHawk.Emulation.CPUs.M6507;

namespace BizHawk.Emulation.Consoles.Atari
{
	// Emulates the M6532 RIOT Chip
	public partial class M6532
	{
		MOS6507 Cpu;
		public byte[] ram;
		public int timerStartValue;
		public int timerFinishedCycles;
		public int timerShift;
		Atari2600 core;


		public M6532(MOS6507 cpu, byte[] ram, Atari2600 core)
		{
			Cpu = cpu;
			this.ram = ram;
			this.core = core;

			// Apparently this will break for some games (Solaris and H.E.R.O.). We shall see
			timerFinishedCycles = 0;

		}

		public byte ReadMemory(ushort addr)
		{
			ushort maskedAddr;

			if ((addr & 0x1080) == 0x0080 && (addr & 0x0200) == 0x0000)
			{
				maskedAddr = (ushort)(addr & 0x007f);
				Console.WriteLine("6532 ram read: " + maskedAddr.ToString("x"));
				return ram[maskedAddr];
			}
			else
			{
				maskedAddr = (ushort)(addr & 0x0007);
				if (maskedAddr == 0x04 || maskedAddr == 0x06)
				{
					Console.WriteLine("6532 timer read: " + maskedAddr.ToString("x"));

					// Calculate the current value on the timer
					int timerCurrentValue = timerFinishedCycles - Cpu.TotalExecutedCycles;

					// If the timer has not finished, shift the value down for the game
					if (Cpu.TotalExecutedCycles < timerFinishedCycles)
					{
						return (byte)(((timerCurrentValue) >> timerShift) & 0xFF);
					}
					// Other wise, return the last 8 bits from how long ago it triggered
					else
					{
						return (byte)(timerCurrentValue & 0xFF);
					}
				}
				else
				{
					Console.WriteLine("6532 register read: " + maskedAddr.ToString("x"));
					if (maskedAddr == 0x00) // SWCHA
					{
						return core.ReadControls1();
						//return 0xFF;
					}
					else if (maskedAddr == 0x01) // SWACNT
					{
						
					}
					else if (maskedAddr == 0x02) // SWCHB
					{
						return 0x3F;
					}
					else if (maskedAddr == 0x03) // SWBCNT
					{

					}
				}
			}

			return 0x3A;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			ushort maskedAddr;

			if ((addr & 0x1080) == 0x0080 && (addr & 0x0200) == 0x0000)
			{
				maskedAddr = (ushort)(addr & 0x007f);
				Console.WriteLine("6532 ram write: " + maskedAddr.ToString("x"));
				ram[maskedAddr] = value;
			}
			else
			{
				maskedAddr = (ushort)(addr & 0x0007);
				if ((addr & 0x14) == 0x14)
				{
					int[] shift = new int[] {0,3,6,10};
					timerShift = shift[addr & 0x03];

					// Store the number of cycles for the timer
					timerStartValue = value << timerShift;

					// Calculate when the timer will be finished
					timerFinishedCycles = timerStartValue + Cpu.TotalExecutedCycles;

					Console.WriteLine("6532 timer write:  " + maskedAddr.ToString("x"));
				}
				else
				{
					Console.WriteLine("6532 register write: " + maskedAddr.ToString("x"));
				}
			}
		}
	}
}