using System;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	public class UxROM : NES.NESBoardBase
	{
		string type;
		public UxROM(string type)
		{
			this.type = type;
		}
		public override void Initialize(NES.RomInfo romInfo, NES nes)
		{
			base.Initialize(romInfo, nes);
			Debug.Assert(Util.IsPowerOfTwo(RomInfo.PRG_Size));

			if (type == "UNROM") pagemask = 7;
			else if (type == "UOROM") pagemask = 15;
			else throw new InvalidOperationException("Invalid UXROM type");

			//guess CRAM size (this is a very confident guess!)
			if (RomInfo.CRAM_Size == -1) RomInfo.CRAM_Size = 8;

			cram = new byte[RomInfo.CRAM_Size * 1024];
			cram_mask = cram.Length - 1;
		}
		public override byte ReadPRG(int addr)
		{
			int block = addr >> 14;
			int page = block == 1 ? pagemask : prg;
			page &= pagemask;
			int ofs = addr & 0x3FFF;
			return RomInfo.ROM[(page << 14) | ofs];
		}
		public override void WritePRG(int addr, byte value)
		{
			prg = value;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return cram[addr & cram_mask];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				cram[addr & cram_mask] = value;
			}
			else base.WritePPU(addr,value);
		}

		int pagemask;
		int prg;
		byte[] cram;
		int cram_mask;
	}
}