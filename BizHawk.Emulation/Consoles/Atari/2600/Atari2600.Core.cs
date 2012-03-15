using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.M6507;
using BizHawk.Emulation.Consoles.Atari;

namespace BizHawk
{
	partial class Atari2600
	{
		public byte[] rom;
		public MOS6507 cpu;
		public M6532 m6532;
		public TIA tia;

		bool resetSignal;

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
		public byte ReadMemory(ushort addr)
		{
			ushort maskedAddr = (ushort)(addr & 0x1FFF);
			ushort pageNum = (ushort)(maskedAddr >> 6);
			if (pageNum < 0x40)
			{
				// if Page 0x02, or a mirror
				if (pageNum % 4 > 1)
				{
					return m6532.ReadMemory(maskedAddr);
				}
				// if Page 0x01 or 0x02, or a mirror
				else
				{
					return tia.ReadMemory(maskedAddr);
				}
			}
			// ROM data
			else
			{
				//Console.WriteLine("ROM read");
				return rom[maskedAddr & 0x0FFF];
			}
		}

		public void WriteMemory(ushort addr, byte value)
		{
			ushort maskedAddr = (ushort)(addr & 0x1FFF);
			ushort pageNum = (ushort)(maskedAddr >> 6);
			if (pageNum < 0x40)
			{
				if (pageNum % 4 > 1)
				{
					m6532.WriteMemory(maskedAddr, value);
				}
				else
				{
					tia.WriteMemory(maskedAddr, value);
				}
			}
			// ROM data
			else
			{
				Console.WriteLine("ROM write(?):  " + addr.ToString("x"));
			}
		}

		public void HardReset()
		{
			cpu = new MOS6507();
			cpu.debug = true;
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;

			// Setup TIA
			tia = new TIA(this, frameBuffer);
			// Setup 6532
			m6532 = new M6532(cpu, ram, this);

			//setup the system state here. for instance..
			// Read from the reset vector for where to start
			cpu.PC = (ushort)(ReadMemory(0xFFFC) + (ReadMemory(0xFFFD) << 8)); //set the initial PC
			//cpu.PC = 0x0000; //set the initial PC
		}

		public void FrameAdvance(bool render)
		{
			Frame++;
			if (resetSignal)
			{
				//cpu.PC = cpu.ReadWord(MOS6507.ResetVector);
				m6532.resetOccured = true;
				//m6532.swchb &= 0xFE;
				//cpu.FlagI = true;
			}
			//cpu.Execute(228);
			//cpu.Execute(2000);

			tia.frameComplete = false;
			while (tia.frameComplete == false)
			{
				tia.execute(1);
				tia.execute(1);
				tia.execute(1);

				cpu.Execute(1);
				if (cpu.PendingCycles <= 0)
				{
					Console.WriteLine("Tia clocks: " + tia.scanlinePos + "    CPU pending: " + cpu.PendingCycles);
				}
				if (cpu.PendingCycles < 0)
				{
					Console.WriteLine("------------Something went wrong------------");
				}
				
			}
			resetSignal = Controller["Reset"];
			//clear the framebuffer (hack code)
			if (render == false) return;
			/*
			for (int i = 0; i < 160 * 192; i++)
			{
				if (i < 64*256)
					frameBuffer[i] = i % 256; //black
				if (i >= 64*256 && i < 128*256)
					frameBuffer[i] = (i % 256) << 8; //black
				if (i >= 128*256)
					frameBuffer[i] = (i % 256) << 16; //black
			}
				*/

			//run one frame's worth of cpu cyclees (i.e. do the emulation!)
			//this should generate the framebuffer as it goes.
		}

		public byte ReadControls1()
		{
			byte value = 0xFF;

			if (Controller["P1 Up"]) value &= 0xEF;
			if (Controller["P1 Down"]) value &= 0xDF;
			if (Controller["P1 Left"]) value &= 0xBF;
			if (Controller["P1 Right"]) value &= 0x7F;
			if (Controller["P1 Button"]) value &= 0xF7;
			return value;
		}
	}
}