using System;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	//generally mapper3

	public class CxROM : NES.NESBoardBase
	{
		string type;
		public CxROM(string type)
		{
			this.type = type;
		}
		public override void Initialize(NES.RomInfo romInfo, NES nes)
		{
			base.Initialize(romInfo, nes);
			Debug.Assert(Util.IsPowerOfTwo(RomInfo.CHR_Size));
			chr_mask = RomInfo.CHR_Size - 1;
			bus_conflict = true;
		}
		
		public override void WritePRG(int addr, byte value)
		{
			if (bus_conflict)
			{
				byte old_value = value;
				value &= ReadPRG(addr);
				Debug.Assert(old_value == value,"Found a test case of CxROM bus conflict. please report.");
			}
			chr = value&chr_mask;
			Console.WriteLine("at {0}, set chr={1}", NES.ppu.ppur.status.sl, chr);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return RomInfo.VROM[addr + (chr<<13)];
			}
			else return base.ReadPPU(addr);
		}

		int chr;
		int chr_mask;
		bool bus_conflict;

	}
}