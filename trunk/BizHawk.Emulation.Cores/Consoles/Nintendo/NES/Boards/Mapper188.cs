using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper188 : NES.NESBoardBase
	{
		// config
		int prg_16k_mask;
		// state
		int prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER188":
					AssertVram(8);
					AssertChr(0);
					AssertPrg(128, 256);
					Cart.wram_size = 0;
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			prg_16k_mask = Cart.prg_size / 16 - 1;
			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			prg = value;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg", ref prg);
			base.SyncState(ser);
		}

		public override byte ReadPRG(int addr)
		{
			int bank = prg;
			if (addr >= 0x4000)
				bank = 15;
			bank ^= 8; // bad dumps?
			bank &= prg_16k_mask;
			return ROM[addr & 0x3fff | bank << 14];
		}

		public override byte ReadWRAM(int addr)
		{
			return 3;
		}
	}
}
