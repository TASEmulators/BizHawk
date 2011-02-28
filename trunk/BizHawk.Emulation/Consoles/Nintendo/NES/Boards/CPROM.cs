using System;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	//generally mapper3

	public class CPROM : NES.NESBoardBase
	{
		byte[] cram;
		int chr;

		public CPROM()
		{
		}
		public override void Initialize(NES.RomInfo romInfo, NES nes)
		{
			base.Initialize(romInfo, nes);
			cram = new byte[16*1024];
		}
		
		public override void WritePRG(int addr, byte value)
		{
			value = HandleNormalPRGConflict(addr,value);
			chr = value&3;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x1000)
				return cram[addr];
			else if(addr<0x2000)
				return cram[addr-0x1000 + (chr<<12)];
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x1000)
				cram[addr] = value;
			else if (addr < 0x2000)
				cram[addr - 0x1000 + (chr << 12)] = value;
			else base.WritePPU(addr,value);
		}
	}
}