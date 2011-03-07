using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{

	public class CPROM : NES.NESBoardBase
	{
		//generally mapper 13

		//Videomation

		//state
		byte[] cram;
		int chr;

		public override bool Configure(NES.BootGodDB.Cart cart)
		{
			//configure
			switch (cart.board_type)
			{
				case "NES-CPROM":
					BoardInfo.PRG_Size = 32;
					break;
				default:
					return false;
			}

			cram = new byte[16*1024];
			SetMirrorType(NES.EMirrorType.Vertical);

			//validate
			Assert(cart.prg_size == BoardInfo.PRG_Size);

			return true;
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

		public override void SaveStateBinary(BinaryWriter bw)
		{
			base.SaveStateBinary(bw);
			bw.Write(chr);
			Util.WriteByteBuffer(bw, cram);
		}

		public override void LoadStateBinary(BinaryReader br)
		{
			base.LoadStateBinary(br);
			chr = br.ReadInt32();
			cram = Util.ReadByteBuffer(br, false);
		}
	}
}