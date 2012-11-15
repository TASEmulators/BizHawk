using BizHawk.Emulation.CPUs.M6502;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class Drive1541
	{
		// the 1541 drive:
		//
		// 2kb ram, mapped 0000-07FF
		// two 6522 VIA chips, mapped at 1800 (communication to C64) and 1C00 (drive mechanics)
		// drive ROM, mapped C000-FFFF

		public MOS6502X cpu;
		public int cyclesPerRevolution;
		public int cyclesPerSecond;
		public Disk disk;
		public byte[] ram;
		public byte[] rom;
		public double rpm;
		public Via via0;
		public Via via1;

		public Drive1541(byte[] driveRom, Region driveRegion)
		{
			rom = new byte[driveRom.Length];
			Array.Copy(driveRom, rom, driveRom.Length);

			switch (driveRegion)
			{
				case Region.NTSC:
					cyclesPerSecond = 14318181 / 14;
					break;
				case Region.PAL:
					cyclesPerSecond = 14318181 / 18;
					break;
			}
			HardReset();
		}

		public void Eject()
		{
			disk = null;
		}

		public void HardReset()
		{
			cpu = new MOS6502X();
			cpu.PC = (ushort)(Read(0xFFFC) + (Read(0xFFFD) << 8));
			cpu.ReadMemory = Read;
			cpu.WriteMemory = Write;
			cpu.DummyReadMemory = Read;

			ram = new byte[0x800];
			via0 = new Via();
			via1 = new Via();
			SetRPM(300.0);
		}

		public void Insert(Disk newDisk)
		{
			disk = newDisk;
		}

		public byte Peek(int addr)
		{
			addr &= 0xFFFF;
			if (addr < 0x0800)
			{
				return ram[addr];
			}
			else if (addr >= 0x1800 && addr < 0x1810)
			{
				return via0.Peek(addr);
			}
			else if (addr >= 0x1C00 && addr < 0x1C10)
			{
				return via1.Peek(addr);
			}
			else if (addr >= 0xC000)
			{
				return rom[addr & 0x3FFF];
			}
			return 0xFF;
		}

		public void PerformCycle()
		{
			cpu.ExecuteOne();
		}

		public void Poke(int addr, byte val)
		{
			addr &= 0xFFFF;
			if (addr < 0x0800)
			{
				ram[addr] = val;
			}
			else if (addr >= 0x1800 && addr < 0x1810)
			{
				via0.Poke(addr, val);
			}
			else if (addr >= 0x1C00 && addr < 0x1C10)
			{
				via1.Poke(addr, val);
			}
		}

		public byte Read(ushort addr)
		{
			if (addr < 0x0800)
			{
				return ram[addr];
			}
			else if (addr >= 0x1800 && addr < 0x1810)
			{
				return via0.Read(addr);
			}
			else if (addr >= 0x1C00 && addr < 0x1C10)
			{
				return via1.Read(addr);
			}
			else if (addr >= 0xC000)
			{
				return rom[addr & 0x3FFF];
			}
			return 0xFF;
		}

		public void SetRPM(double newRPM)
		{
			rpm = newRPM;
			cyclesPerRevolution = (int)((double)cyclesPerSecond / newRPM / (double)60);
		}

		public void Write(ushort addr, byte val)
		{
			if (addr < 0x0800)
			{
				ram[addr] = val;
			}
			else if (addr >= 0x1800 && addr < 0x1810)
			{
				via0.Write(addr, val);
			}
			else if (addr >= 0x1C00 && addr < 0x1C10)
			{
				via1.Write(addr, val);
			}
		}
	}
}
