using BizHawk.Emulation.CPUs.M6502;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class PRGFile : IMedia
	{
		private MOS6502X cpu;
		private byte[] data;
		private bool loaded;
		private Memory mem;

		public PRGFile(byte[] file, Memory checkMem, MOS6502X targetCpu)
		{
			cpu = targetCpu;
			mem = checkMem;
			data = file;
		}

		public void Apply(Memory mem)
		{
			int address = data[1];
			address <<= 8;
			address |= data[0];

			int count = data.Length;

			for (int i = 2; i < count; i++)
			{
				mem.Write((ushort)(address & 0xFFFF), data[i]);
				address++;
			}

			//// "RUN"
			//mem[0x04F0] = 0x12;
			//mem[0x04F1] = 0x15;
			//mem[0x04F2] = 0x0E;
			
			//// set cursor to be right after run (3, 6)
			//mem[0x00C9] = 0x06;
			//mem[0x00CA] = 0x03;
			//mem[0x00D3] = 0x03;
			//mem[0x00D6] = 0x06;

			//// set keyboard buffer
			//mem[0x00C5] = 0x0D;
			//mem[0x00C6] = 0x02;
			//mem[0x00CB] = 0x0D;
			//mem[0x0277] = 0x0D;
			//mem[0x0278] = 0x0D;

			cpu.PC = 2064;

			loaded = true;
		}

		public bool Loaded()
		{
			return loaded;
		}

		public bool Ready()
		{
			// wait for READY. to be on display
			return (
				mem[0x04C8] == 0x12 &&
				mem[0x04C9] == 0x05 &&
				mem[0x04CA] == 0x01 &&
				mem[0x04CB] == 0x04 &&
				mem[0x04CC] == 0x19 &&
				mem[0x04CD] == 0x2E &&
				mem[0x04CE] == 0x20
				);
		}
	}
}
