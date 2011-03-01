using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	//generally mapper2

	//Mega Man
	//Castlevania
	//Contra
	//Duck Tales
	//Metal Gear

	//TODO - simplify logic and handle fewer (known) cases (e.g. no IsPowerOfTwo, but rather hardcoded cases)

	public class UxROM : NES.NESBoardBase
	{
		//configuration
		string type;
		int pagemask;
		int cram_mask;

		//state
		int prg;
		byte[] cram;

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
			else throw new InvalidOperationException("Invalid UxROM type");

			//guess CRAM size (this is a very confident guess!)
			//(should these guesses be here?) (is this a guess? maybe all these boards have cram)
			if (RomInfo.CRAM_Size == -1) RomInfo.CRAM_Size = 8;

			cram = new byte[RomInfo.CRAM_Size * 1024];
			cram_mask = cram.Length - 1;
		}
		public override byte ReadPRG(int addr)
		{
			int block = addr >> 14;
			int page = block == 1 ? pagemask : prg;
			int ofs = addr & 0x3FFF;
			return RomInfo.ROM[(page << 14) | ofs];
		}
		public override void WritePRG(int addr, byte value)
		{
			prg = value & pagemask;
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

		public override void SaveStateBinary(BinaryWriter bw)
		{
			base.SaveStateBinary(bw);
			bw.Write(prg);
			Util.WriteByteBuffer(bw, cram);
		}

		public override void LoadStateBinary(BinaryReader br)
		{
			base.LoadStateBinary(br);
			prg = br.ReadInt32();
			cram = Util.ReadByteBuffer(br, false);
		}
	}
}