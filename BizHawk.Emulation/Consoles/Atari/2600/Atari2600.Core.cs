using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.M6502;
using BizHawk.Emulation.Consoles.Atari;
using BizHawk.Emulation.Consoles.Atari._2600;

namespace BizHawk
{
	partial class Atari2600
	{
		public byte[] rom;
		public MOS6502X cpu;
		public M6532 m6532;
		public TIA tia;
		public byte[] ram = new byte[128];
		public MapperBase mapper;

		// The Atari 2600 memory mapper looks something like this...usually

		// N/A  Page #  
		// 000  0000000 000000

		// 0x0000-0x003F - TIA Registers
		// 0x0040-0x007F - TIA Registers (mirror)
		// 0x0080-0x00FF - 6532 RAM

		// 0x0100-0x01FF - Mirror of 0x00FF

		// 0x0200-0x023F - TIA Registers (mirror)
		// 0x0240-0x027F - TIA Registers (mirror)

		// 0x0280-0x029F - 6532 Registers
		// 0x02A0-0x02BF - 6532 Registers (mirror)
		// 0x02C0-0x02DF - 6532 Registers (mirror)
		// 0x02E0-0x02FF - 6532 Registers (mirror)

		// 0x0300-0x033F - TIA Registers (mirror)
		// 0x0340-0x037F - TIA Registers (mirror)

		// 0x0380-0x039F - 6532 Registers (mirror)
		// 0x03A0-0x03BF - 6532 Registers (mirror)
		// 0x03C0-0x03DF - 6532 Registers (mirror)
		// 0x03E0-0x03FF - 6532 Registers (mirror)

		// 0x0400-0x07FF - Mirror of 0x0000-0x03FF
		// 0x0800-0x0BFF - Mirror of 0x0000-0x03FF
		// 0x0C00-0x0FFF - Mirror of 0x0000-0x03FF

		// 0x1000-0x1FFF - ROM

		// If page# % 4 == 0 or 1, TIA
		// If page# % 4 == 2 or 3, 6532
		//   if (addr & 0x0200 == 0x0000 && addr & 0x1080 == 0x0080)
		//     RAM
		//   else
		//     registers
		// else
		//   ROM
		public byte BaseReadMemory(ushort addr)
		{
			addr = (ushort)(addr & 0x1FFF);
			if ((addr & 0x1080) == 0)
			{
				return tia.ReadMemory(addr);
			}
			else if ((addr & 0x1080) == 0x0080)
			{
				return m6532.ReadMemory(addr);
			}
			else
			{
				return rom[addr & 0x0FFF];
			}
		}

		public void BaseWriteMemory(ushort addr, byte value)
		{
			addr = (ushort)(addr & 0x1FFF);
			if ((addr & 0x1080) == 0)
			{
				tia.WriteMemory(addr, value);
			}
			else if ((addr & 0x1080) == 0x0080)
			{
				m6532.WriteMemory(addr, value);
			}
			else
			{
				Console.WriteLine("ROM write(?):  " + addr.ToString("x"));
			}
		}

		public byte ReadMemory(ushort addr)
		{
			return mapper.ReadMemory((ushort)(addr&0x1FFF));
		}

		public void WriteMemory(ushort addr, byte value)
		{
			mapper.WriteMemory((ushort)(addr & 0x1FFF), value);
		}

		public void HardReset()
		{
			//regenerate mapper here to make sure its state is entirely clean
			switch (game.GetOptionsDict()["m"])
			{
				case "4K": mapper = new m4K(); break;
				case "2K": mapper = new m2K(); break;
				case "CV": mapper = new mCV(); break;
				case "F8": mapper = new mF8(); break;
				case "F6": case "F6SC": mapper = new mF6(); break;
				case "F4": case "F4SC": mapper = new mF4(); break;
				case "FE": mapper = new mFE(); break;
				case "E0": mapper = new mE0(); break;
				case "3F": mapper = new m3F(); break;
				case "FA": mapper = new mFA(); break;
				case "E7": mapper = new mE7(); break;
				case "F0": mapper = new mF0(); break;
				case "UA": mapper = new mUA(); break;
				//Homebrew mappers
				case "3Fe": mapper = new m3Fe(); break;
				case "3E": mapper = new m3E(); break;
				case "0840": mapper = new m0840(); break;
				case "MC": mapper = new mMC(); break;
				case "EF": mapper = new mEF(); break;
				case "X07": mapper = new mX07(); break;
				case "4A50": mapper = new m4A50(); break;

				default: throw new InvalidOperationException("mapper not supported: " + game.GetOptionsDict()["m"]);
			}
			mapper.core = this;

			_lagcount = 0;
			cpu = new MOS6502X();
			//cpu.debug = true;
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			cpu.DummyReadMemory = ReadMemory;

			// Setup TIA
			//tia = new TIA(this, frameBuffer);
			tia = new TIA(this);
			// Setup 6532
			m6532 = new M6532(this);

			//setup the system state here. for instance..
			// Read from the reset vector for where to start
			cpu.PC = (ushort)(ReadMemory(0x1FFC) + (ReadMemory(0x1FFD) << 8)); //set the initial PC
			//cpu.PC = 0x0000; //set the initial PC

		}

		public void FrameAdvance(bool render)
		{
			_frame++;
			_islag = true;
			tia.frameComplete = false;
			while (tia.frameComplete == false)
			{
				tia.execute(1);
				tia.execute(1);
				tia.execute(1);

				m6532.timer.tick();
				cpu.ExecuteOne();
				//if (cpu.PendingCycles <= 0)
				//{
				//  //Console.WriteLine("Tia clocks: " + tia.scanlinePos + "    CPU pending: " + cpu.PendingCycles);
				//}
				//if (cpu.PendingCycles < 0)
				//{
				//  Console.WriteLine("------------Something went wrong------------");
				//}
				
			}

			if (_islag)
				LagCount++;
			//if (render == false) return;
		}

		public byte ReadControls1()
		{
			byte value = 0xFF;

			if (Controller["P1 Up"]) value &= 0xEF;
			if (Controller["P1 Down"]) value &= 0xDF;
			if (Controller["P1 Left"]) value &= 0xBF;
			if (Controller["P1 Right"]) value &= 0x7F;
			if (Controller["P1 Button"]) value &= 0xF7;
			_islag = false;
			return value;
		}

		public byte ReadControls2()
		{
			byte value = 0xFF;

			if (Controller["P2 Up"]) value &= 0xEF;
			if (Controller["P2 Down"]) value &= 0xDF;
			if (Controller["P2 Left"]) value &= 0xBF;
			if (Controller["P2 Right"]) value &= 0x7F;
			if (Controller["P2 Button"]) value &= 0xF7;
			_islag = false;
			return value;
		}

		private bool bw = false;
		private bool p0difficulty = true;
		private bool p1difficulty = true;

		public void SetBw(bool setting) { bw = setting; }
		public void SetP0Diff(bool setting) { p0difficulty = setting; }
		public void SetP1Diff(bool setting) { p1difficulty = setting; }

		public byte ReadConsoleSwitches()
		{
			byte value = 0xFF;

			bool select = Controller["Select"];
			bool reset = Controller["Reset"];

			if (reset) value &= 0xFE;
			if (select) value &= 0xFD;
			if (bw) value &= 0xF7;
			if (p0difficulty) value &= 0xBF;
			if (p1difficulty) value &= 0x7F;

			return value;
		}
	}

	public class MapperBase
	{
		public Atari2600 core;
		public virtual byte ReadMemory(ushort addr) { return core.BaseReadMemory(addr); }
		public virtual void WriteMemory(ushort addr, byte value) { core.BaseWriteMemory(addr, value); }
		public virtual void SyncState(Serializer ser) { }
	}
}