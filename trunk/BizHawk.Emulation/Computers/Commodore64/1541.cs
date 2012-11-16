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

		// default 1800:
		// 07 00 1A FF 05 00 FF FF
		// 04 00 00 00 00 00 00 00
		// default 1C00:
		// 90 00 00 00 05 00 FF FF
		// 04 00 00 00 00 00 80 00

		private Cia cia;

		public MOS6502X cpu;
		public int cyclesPerRevolution;
		public int cyclesPerSecond;
		public Disk disk;
		public byte[] ram;
		public byte[] rom;
		public double rpm;
		public ChipSignals signal;
		public Via via0;
		public Via via1;

		public Drive1541(byte[] driveRom, Region driveRegion, Cia ciaInterface)
		{
			rom = new byte[driveRom.Length];
			Array.Copy(driveRom, rom, driveRom.Length);

			cia = ciaInterface;

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

			// attach VIA/CIA
			via0.Connect(cia.ConnectSerialPort(1));

			// set VIA values
			via0.Poke(0x0, 0x07);
			via0.Poke(0x2, 0x1A);
			via0.Poke(0x3, 0xFF);
			via0.Poke(0x4, 0x05);
			via0.Poke(0x6, 0xFF);
			via0.Poke(0x7, 0xFF);
			via0.Poke(0x8, 0x04);
			via0.Poke(0xE, 0x80);
			via1.Poke(0x0, 0x90);
			via1.Poke(0x4, 0x05);
			via1.Poke(0x6, 0xFF);
			via1.Poke(0x7, 0xFF);
			via1.Poke(0x8, 0x04);
			via1.Poke(0xE, 0x80);
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
			else if (addr >= 0x1800 && addr < 0x1C00)
			{
				return via0.Peek(addr);
			}
			else if (addr >= 0x1C00 && addr < 0x2000)
			{
				return via1.Peek(addr);
			}
			else if (addr >= 0xC000)
			{
				return rom[addr & 0x3FFF];
			}
			return 0xFF;
		}

		public byte PeekVia0(int addr)
		{
			return via0.Peek(addr);
		}

		public byte PeekVia1(int addr)
		{
			return via1.Peek(addr);
		}

		public void PerformCycle()
		{
			cpu.IRQ = via0.IRQ | via1.IRQ;
			cpu.ExecuteOne();
			via0.PerformCycle();
			via1.PerformCycle();
		}

		public void Poke(int addr, byte val)
		{
			addr &= 0xFFFF;
			if (addr < 0x0800)
			{
				ram[addr] = val;
			}
			else if (addr >= 0x1800 && addr < 0x1C00)
			{
				via0.Poke(addr, val);
			}
			else if (addr >= 0x1C00 && addr < 0x2000)
			{
				via1.Poke(addr, val);
			}
		}

		public void PokeVia0(int addr, byte val)
		{
			via0.Poke(addr, val);
		}

		public void PokeVia1(int addr, byte val)
		{
			via1.Poke(addr, val);
		}

		public byte Read(ushort addr)
		{
			if (addr < 0x0800)
			{
				return ram[addr];
			}
			else if (addr >= 0x1800 && addr < 0x1C00)
			{
				return via0.Read(addr);
			}
			else if (addr >= 0x1C00 && addr < 0x2000)
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
			else if (addr >= 0x1800 && addr < 0x1C00)
			{
				via0.Write(addr, val);
			}
			else if (addr >= 0x1C00 && addr < 0x2000)
			{
				via1.Write(addr, val);
			}
		}
	}
}
