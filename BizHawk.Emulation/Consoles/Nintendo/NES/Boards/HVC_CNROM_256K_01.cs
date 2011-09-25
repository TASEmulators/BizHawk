using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class HVC_CNROM_256K_01 : NES.NESBoardBase 
	{
		//Mapper 185
		//Spy Vs. Spy (J)
		//Mighty Bomb Jack (J)

		int chr;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "HVC-CNROM-256K-01": 
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
		}

		public override void WritePRG(int addr, byte value)
		{
			chr = value;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[addr + (chr * 0x2000)];
			return base.ReadPPU(addr);
		}
	}
}
