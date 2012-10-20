using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// caltron 6 in 1
	public class Mapper041 : NES.NESBoardBase
	{
		int prg;
		int chr;
		bool regenable;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER041":
				case "MLT-CALTRON6IN1":
					break;
				default:
					return false;
			}
			AssertPrg(256);
			AssertChr(128);
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr < 0x800)
			{
				prg = addr & 7;
				SetMirrorType((addr & 32) != 0 ? EMirrorType.Horizontal : EMirrorType.Vertical);
				regenable = (addr & 4) != 0;
				chr &= 3;
				chr |= (addr >> 1) & 0xc;
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			if (regenable)
			{
				chr &= 0xc;
				chr |= addr & 3;
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[addr | chr << 13];
			else
				return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr | prg << 15];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
			ser.Sync("chr", ref chr);
			ser.Sync("regenable", ref regenable);
		}
	}
}
