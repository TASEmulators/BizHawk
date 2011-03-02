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

	//TODO - look for a mirror=H UNROM--maybe there are none? this may be fixed to the board type.

	public class UxROM : NES.NESBoardBase
	{
		//configuration
		string type;
		int prg_mask;
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
			Debug.Assert(RomInfo.PRG_Size == 8 || RomInfo.PRG_Size == 16);
			Debug.Assert(RomInfo.CRAM_Size == -1, "don't specify in gamedb, it is redundant");

			if (type == "UNROM") prg_mask = 7;
			else if (type == "UOROM") prg_mask = 15;
			else throw new InvalidOperationException("Invalid UxROM type");

			//regardless of what the board is equipped to handle, reduce the mask to how much ROM is actually present
			int rom_prg_mask = (RomInfo.PRG_Size - 1);
			if (rom_prg_mask < prg_mask) prg_mask = rom_prg_mask;

			//these boards always have 8KB of CRAM
			RomInfo.CRAM_Size = 8;
			cram = new byte[RomInfo.CRAM_Size * 1024];
			cram_mask = cram.Length - 1;
		}
		public override byte ReadPRG(int addr)
		{
			int block = addr >> 14;
			int page = block == 1 ? prg_mask : prg;
			int ofs = addr & 0x3FFF;
			return RomInfo.ROM[(page << 14) | ofs];
		}
		public override void WritePRG(int addr, byte value)
		{
			prg = value & prg_mask;
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