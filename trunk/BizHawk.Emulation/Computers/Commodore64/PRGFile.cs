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

		public void Apply()
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

			if (data[0x06] == 0x9E)
			{
				// sys command
				bool isNumber = false;
				int sysAddress = 0;
				int sysIndex = 0x07;
				while (data[sysIndex] != 0)
				{
					if (!isNumber)
					{
						if (data[sysIndex] >= 0x30 && data[sysIndex] <= 0x39)
						{
							isNumber = true;
						}
						else
						{
							sysIndex++;
						}
					}
					if (isNumber)
					{
						sysAddress *= 10;
						sysAddress += (data[sysIndex] & 0xF);
						sysIndex++;
						if (data[sysIndex] < 0x30 || data[sysIndex] > 0x39)
							break;
					}
				}
				if (sysAddress > 0 && sysAddress < 0x10000)
				{
					cpu.PC = (ushort)sysAddress;
				}
			}

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
