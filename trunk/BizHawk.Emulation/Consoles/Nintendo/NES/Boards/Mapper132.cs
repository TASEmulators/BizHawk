using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// Creatom
	// specs pulled from Nintendulator sources
	public class Mapper132 : NES.NESBoardBase
	{
		//configuraton
		int prg_mask, chr_mask;
		//state
		int prg, chr;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER132":
				case "UNIF_UNL-22211":
					break;
				default:
					return false;
			}

			prg_mask = Cart.prg_size / 32 - 1;
			chr_mask = Cart.chr_size / 8 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		byte reg
		{
			set
			{
				prg = (value & 0x4) >> 2;
				prg &= prg_mask;
				chr = (value & 0x3);
				chr &= chr_mask;
			}
			get
			{
				return (byte)(prg << 2 | chr);
			}
		}

		public override void WriteEXP(int addr, byte value)
		{
			if ((addr & 0x103) == 0x102)
				reg = (byte)(value & 0x0f);
		}

		public override byte ReadEXP(int addr)
		{
			if ((addr & 0x100) != 0)
				return (byte)((NES.DB & 0xf0) | reg);
			else if ((addr & 0x1000) == 0)
				return NES.DB;
			else
				return 0xff;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr + (prg << 15)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[addr + (chr << 13)];
			}
			else return base.ReadPPU(addr);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
		}

	}
}
