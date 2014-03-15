using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Farid_UNROM_8_in_1 : NES.NESBoardBase
	{
		// http://forums.nesdev.com/viewtopic.php?f=9&t=11099

		// state
		int c; // clock bit for the second 74'161
		int e; // /load for second 74'161. guaranteed to be 0 on powerup
		int prginner;
		int prgouter; // guaranteed to be 0 on powerup

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_FARID_UNROM_8-IN-1":
					AssertPrg(1024);
					AssertChr(0);
					break;
				default:
					return false;
			}

			Cart.vram_size = 8;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			prginner = value & 7;
			int newc = value >> 7;
			int newe = value >> 3 & 1;

			if (newc > c && e == 0) // latch e and outer
			{
				e = newe;
				prgouter = value >> 4 & 7;
			}
			c = newc;
		}

		public override byte ReadPRG(int addr)
		{
			int bnk = addr >= 0x4000 ? 7 : prginner;
			bnk |= prgouter << 3;
			return ROM[bnk << 14 | addr & 0x3fff];
		}

		public override void SyncState(BizHawk.Common.Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("c", ref c);
			ser.Sync("e", ref e);
			ser.Sync("prginner", ref prginner);
			ser.Sync("prgouter", ref prgouter);
		}

		public override void NESSoftReset()
		{
			e = 0;
			prgouter = 0;
		}
	}
}
