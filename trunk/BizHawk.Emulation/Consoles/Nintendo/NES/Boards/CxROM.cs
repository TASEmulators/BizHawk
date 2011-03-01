using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	//generally mapper3

	//Solomon's Key
	//Arkanoid
	//Arkista's Ring
	//Bump 'n' Jump
	//Cybernoid

	public class CxROM : NES.NESBoardBase
	{
		//configuration
		string type;
		int chr_mask;
		bool bus_conflict;

		//state
		int chr;

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
			if (bus_conflict) value = HandleNormalPRGConflict(addr,value);
			chr = value&chr_mask;
			//Console.WriteLine("at {0}, set chr={1}", NES.ppu.ppur.status.sl, chr);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return RomInfo.VROM[addr + (chr<<13)];
			}
			else return base.ReadPPU(addr);
		}

		public override void SaveStateBinary(BinaryWriter bw)
		{
			base.SaveStateBinary(bw);
			bw.Write(chr);
		}

		public override void LoadStateBinary(BinaryReader br)
		{
			base.LoadStateBinary(br);
			chr = br.ReadInt32();
		}
	}
}