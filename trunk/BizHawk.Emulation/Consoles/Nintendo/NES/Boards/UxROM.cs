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
		int prg_mask;
		int cram_byte_mask;

		//state
		int prg;
		byte[] cram;

		public override bool Configure(NES.BootGodDB.Cart cart)
		{
			//configure
			switch (cart.board_type)
			{
				case "NES-UNROM":
				case "HVC-UNROM": 
				case "KONAMI-UNROM":
					BoardInfo.PRG_Size = 128; 
					break;

				case "NES-UOROM":
				case "HVC-UOROM":
					BoardInfo.PRG_Size = 256;
					break;

				default:
					return false;
			}
			//these boards always have 8KB of CRAM
			BoardInfo.CRAM_Size = 8;
			cram = new byte[BoardInfo.CRAM_Size * 1024];
			cram_byte_mask = cram.Length - 1;
			prg_mask = (BoardInfo.PRG_Size / 16) - 1;
			SetMirrorType(cart.pad_h, cart.pad_v);


			//validate
			Assert(cart.prg_size == BoardInfo.PRG_Size);

			return true;
		}

		public override byte ReadPRG(int addr)
		{
			int block = addr >> 14;
			int page = block == 1 ? prg_mask : prg;
			int ofs = addr & 0x3FFF;
			return ROM[(page << 14) | ofs];
		}
		public override void WritePRG(int addr, byte value)
		{
			prg = value & prg_mask;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return cram[addr & cram_byte_mask];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				cram[addr & cram_byte_mask] = value;
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