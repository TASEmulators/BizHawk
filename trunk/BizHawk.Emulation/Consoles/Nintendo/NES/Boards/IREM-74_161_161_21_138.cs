using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//Mapper 77
	//Napoleon Senki

	class IREM_74_161_161_21_138 : NES.NESBoardBase
	{
		int chr, prg;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "IREM-74*161/161/21/138":
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
		}

		public override void WritePRG(int addr, byte value)
		{
			chr = (value >> 4) & 0x0F;
			prg = value & 0x0F;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x0800)
			{
				return VROM[addr + (chr * 0x0800)];
			}
			else if (addr < 0x2000)
				return VRAM[addr];
			else return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x8000)
				return ROM[addr + (prg * 0x8000)];
			else
				return base.ReadPRG(addr); 
		}
	}
}
