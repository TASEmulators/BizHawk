﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// 1997-in-1
	// 999999-in-1
	// 1000000-in-1
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_212
	internal sealed class Mapper212 : NesBoardBase
	{
		private int _reg;
		private int prg_bank_mask_32k, prg_bank_mask_16k, chr_bank_mask_8k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER212":
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.PadH, Cart.PadV);

			chr_bank_mask_8k = Cart.ChrSize / 8 - 1;
			prg_bank_mask_16k = Cart.PrgSize / 16 - 1;
			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;

			_reg = 65535;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(_reg), ref _reg);
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;

			_reg = addr;
			SetMirrorType(addr.Bit(3) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPrg(int addr)
		{
			addr += 0x8000;
			byte ret;

			if ((_reg & 0x4000) > 0)
			{
				int bank = (_reg >> 1) & 3;
				bank &= prg_bank_mask_32k;
				ret = Rom[(bank * 0x8000) + (addr & 0x7FFF)];
			}
			else
			{
				int bank = _reg & 7;
				bank &= prg_bank_mask_16k;
				ret = Rom[(bank * 0x4000) + (addr & 0x3FFF)];
			}

			if ((addr & 0xE010) == 0x6000)
			{
				ret |= 0x80;
			}

			return ret;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = _reg & 7;
				bank &= chr_bank_mask_8k;
				return Vrom[(bank * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}
	}
}
