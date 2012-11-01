using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class  C64 : IEmulator
	{
		public byte[] rom;
		public MOS6502X cpu;

		private void HardReset()
		{
			cpu = new MOS6502X();
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			cpu.DummyReadMemory = ReadMemory;
		}

		public byte ReadMemory(ushort addr)
		{
			return 0;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			//TODO
		}
	}
}
