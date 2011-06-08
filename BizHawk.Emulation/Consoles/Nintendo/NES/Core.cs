using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public partial class NES : IEmulator
	{
		//hardware/state
		protected MOS6502 cpu;
		int cpu_accumulate; //cpu timekeeper
		public PPU ppu;
		public APU apu;
		byte[] ram;
		MemoryDomain.FreezeData[] sysbus_freeze = new MemoryDomain.FreezeData[65536];
		NESWatch[] sysbus_watch = new NESWatch[65536];
		protected byte[] CIRAM; //AKA nametables
		string game_name; //friendly name exposed to user and used as filename base
		CartInfo cart; //the current cart prototype. should be moved into the board, perhaps
		INESBoard board; //the board hardware that is currently driving things

		bool _irq_apu, _irq_cart;
		public bool irq_apu { get { return _irq_apu; } set { _irq_apu = value; sync_irq(); } }
		public bool irq_cart { get { return _irq_cart; } set { _irq_cart = value; sync_irq(); } }
		void sync_irq()
		{
			cpu.IRQ = _irq_apu || _irq_cart;
		}

		//user configuration 
		int[,] palette = new int[64,3];
		int[] palette_compiled = new int[64*8];
		IPortDevice[] ports;

		public void HardReset()
		{
			cpu = new MOS6502();
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			ppu = new PPU(this);
			apu = new APU(this);
			ram = new byte[0x800];
			CIRAM = new byte[0x800];
			ports = new IPortDevice[2];
			ports[0] = new JoypadPortDevice(this);
			ports[1] = new NullPortDevice();

			//fceux uses this technique, which presumably tricks some games into thinking the memory is randomized
			for (int i = 0; i < 0x800; i++)
			{
				if ((i & 4) != 0) ram[i] = 0xFF; else ram[i] = 0x00;
			}

			//in this emulator, reset takes place instantaneously
			cpu.PC = (ushort)(ReadMemory(0xFFFC) | (ReadMemory(0xFFFD) << 8));
			cpu.P = 0x34;
			cpu.S = 0xFD;

			//cpu.debug = true;
		}

		bool resetSignal;
		public void FrameAdvance(bool render)
		{
            lagged = true;
            if (resetSignal)
			{
				cpu.PC = cpu.ReadWord(MOS6502.ResetVector);
				apu.WriteReg(0x4015, 0);
				cpu.FlagI = true;
			}

			Controller.UpdateControls(Frame++);
			//if (resetSignal)
				//Controller.UnpressButton("Reset");   TODO fix this
			resetSignal = Controller["Reset"];
			ppu.FrameAdvance();
            if (lagged)
            {
                _lagcount++;
                islag = true;
            }
            else
                islag = false;
		}

		protected void RunCpu(int ppu_cycles)
		{
			//not being used right now. maybe needed later.
			//Timestamp += ppu_cycles;

			int cycles = ppu_cycles;
			if (ppu.PAL)
			    cycles *= 15;
			else
				cycles <<= 4;


			//tricky logic to try to run one instruction at a time
			cpu_accumulate += cycles;
			int cpu_cycles = cpu_accumulate / 48;
			for (; ; )
			{
				if (cpu_cycles == 0) break;
				int need_cpu = -cpu.PendingCycles + 1;
				if (cpu_cycles < need_cpu) break;
				if (need_cpu == 0) need_cpu = 1;
				int todo = need_cpu;
				cpu_cycles -= todo;
				cpu_accumulate -= 48*todo;
				cpu.Execute(todo);
				apu.Run(todo);
				ppu.PostCpuInstruction(todo);
			}
		}

		public byte ReadPPUReg(int addr)
		{
			return ppu.ReadReg(addr);
		}

		public byte ReadReg(int addr)
		{
			switch (addr)
			{
				case 0x4000: case 0x4001: case 0x4002: case 0x4003:
				case 0x4004: case 0x4005: case 0x4006: case 0x4007:
				case 0x4008: case 0x4009: case 0x400A: case 0x400B:
				case 0x400C: case 0x400D: case 0x400E: case 0x400F:
				case 0x4010: case 0x4011: case 0x4012: case 0x4013:
					return apu.ReadReg(addr);
				case 0x4014: /*OAM DMA*/ break;
				case 0x4015: return apu.ReadReg(addr); break;
				case 0x4016:
					return read_joyport(addr);
				case 0x4017: return apu.ReadReg(addr); break;
				default:
					//Console.WriteLine("read register: {0:x4}", addr);
					break;

			}
			return 0xFF;
		}

		void WritePPUReg(int addr, byte val)
		{
			ppu.WriteReg(addr, val);
		}

		void WriteReg(int addr, byte val)
		{
			switch (addr)
			{
				case 0x4000: case 0x4001: case 0x4002: case 0x4003:
				case 0x4004: case 0x4005: case 0x4006: case 0x4007:
				case 0x4008: case 0x4009: case 0x400A: case 0x400B:
				case 0x400C: case 0x400D: case 0x400E: case 0x400F:
				case 0x4010: case 0x4011: case 0x4012: case 0x4013:
					apu.WriteReg(addr, val);
					break;
				case 0x4014: Exec_OAMDma(val); break;
				case 0x4015: apu.WriteReg(addr, val); break;
				case 0x4016:
					ports[0].Write(val & 1);
					ports[1].Write(val & 1);
					break;
				case 0x4017: apu.WriteReg(addr, val); break;
				default:
					//Console.WriteLine("wrote register: {0:x4} = {1:x2}", addr, val);
					break;
			}
		}

		byte read_joyport(int addr)
		{
			//read joystick port
			//many todos here
			if (addr == 0x4016)
			{
                lagged = false;
                byte ret = ports[0].Read();
				return ret;
			}
			else return 0;
		}

		void Exec_OAMDma(byte val)
		{
			ushort addr = (ushort)(val << 8);
			for (int i = 0; i < 256; i++)
			{
				byte db = ReadMemory((ushort)addr);
				WriteMemory(0x2004, db);
				addr++;
			}
			cpu.PendingCycles -= 512;
		}

		/// <summary>
		/// sets the provided palette as current
		/// </summary>
		void SetPalette(int[,] pal)
		{
			Array.Copy(pal,palette,64*3);
			for(int i=0;i<64*8;i++)
			{
				int d = i >> 6;
				int c = i & 63;
				int r = palette[c, 0];
				int g = palette[c, 1];
				int b = palette[c, 2];
				Palettes.ApplyDeemphasis(ref r, ref g, ref b, d);
				palette_compiled[i] = (int)unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b);
			}
		}

		/// <summary>
		/// looks up an internal NES pixel value to an rgb int (applying the core's current palette and assuming no deemph)
		/// </summary>
		public int LookupColor(int pixel)
		{
			return palette_compiled[pixel];
		}

		public byte ReadMemory(ushort addr)
		{
			byte ret;
			if (addr < 0x0800) ret = ram[addr];
			else if (addr < 0x1000) ret = ram[addr - 0x0800];
			else if (addr < 0x1800) ret = ram[addr - 0x1000];
			else if (addr < 0x2000) ret = ram[addr - 0x1800];
			else if (addr < 0x4000) ret = ReadPPUReg(addr & 7);
			else if (addr < 0x4020) ret = ReadReg(addr); //we're not rebasing the register just to keep register names canonical
			else if (addr < 0x6000) ret = board.ReadEXP(addr - 0x4000);
			else if (addr < 0x8000) ret = board.ReadWRAM(addr - 0x6000);
			else ret = board.ReadPRG(addr - 0x8000);
			
			//apply freeze
			if (sysbus_freeze[addr].IsFrozen) ret = sysbus_freeze[addr].value;

			//handle breakpoints and stuff.
			//the idea is that each core can implement its own watch class on an address which will track all the different kinds of monitors and breakpoints and etc.
			//but since freeze is a common case, it was implemented through its own mechanisms
			if (sysbus_watch[addr] != null)
			{
				sysbus_watch[addr].Sync();
				ret = sysbus_watch[addr].ApplyGameGenie(ret);
			}

			return ret;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x0800) ram[addr] = value;
			else if (addr < 0x1000) ram[addr - 0x0800] = value;
			else if (addr < 0x1800) ram[addr - 0x1000] = value;
			else if (addr < 0x2000) ram[addr - 0x1800] = value;
			else if (addr < 0x4000) WritePPUReg(addr & 7, value);
			else if (addr < 0x4020) WriteReg(addr, value);  //we're not rebasing the register just to keep register names canonical
			else if (addr < 0x6000) board.WriteEXP(addr - 0x4000, value); 
			else if (addr < 0x8000) board.WriteWRAM(addr - 0x6000, value);
			else board.WritePRG(addr - 0x8000, value);
		}

	}
}