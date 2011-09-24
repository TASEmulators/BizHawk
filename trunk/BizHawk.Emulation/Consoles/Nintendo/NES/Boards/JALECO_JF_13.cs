using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//Mapper 86
	
	//Example Games:
	//--------------------------
	//Moero!! Pro Yakyuu (Black)
	//Moero!! Pro Yakyuu (Red)

	class JALECO_JF_13 : NES.NESBoardBase
	{
		int chr;
		int prg;
		int soundon;
		int soundid;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "JALECO-JF-13":
					break;
				default:
					return false;
			}
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x8000)
				return ROM[addr + (prg * 0x8000)];
			else
				return base.ReadPRG(addr);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[(addr & 0x1FFF) + (chr * 0x2000)];
			else
				return base.ReadPPU(addr);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			prg = (value >> 4) & 3;
			chr = (value & 3) + ((value >> 6) & 1);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
			ser.Sync("soundon", ref soundon);
			ser.Sync("soundid", ref soundid);
		}
	}
}
