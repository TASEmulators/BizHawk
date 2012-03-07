using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
	partial class Atari2600
	{
		public byte[] ram = new byte[128];
		public byte[] rom;
		public BizHawk.Emulation.CPUs.M6502.MOS6502 cpu;

		public byte ReadMemory(ushort addr)
		{
			return 0xFF;
		}

		public void WriteMemory(ushort addr, byte value)
		{
		}

		public void HardReset()
		{
			cpu = new Emulation.CPUs.M6502.MOS6502();
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;

			//setup the system state here. for instance..
			cpu.PC = 0x0123; //set the initial PC
		}

		public void FrameAdvance(bool render)
		{
			//clear the framebuffer (hack code)
			if (render == false) return;
			for (int i = 0; i < 256 * 192; i++)
				frameBuffer[i] = 0; //black

			//run one frame's worth of cpu cyclees (i.e. do the emulation!)
			//this should generate the framebuffer as it goes.
		}
	}
}