using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// 1997-in-1
	// 999999-in-1
	// 1000000-in-1
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_212
	public class Mapper212 : NES.NESBoardBase
	{
		private int _reg;
		private int prg_bank_mask_32k, prg_bank_mask_16k, chr_bank_mask_8k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER212":
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			chr_bank_mask_8k = Cart.chr_size / 8 - 1;
			prg_bank_mask_16k = Cart.prg_size / 16 - 1;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;

			_reg = 65535;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("_reg", ref _reg);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;

			_reg = addr;
			SetMirrorType(addr.Bit(3) ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPRG(int addr)
		{
			addr += 0x8000;
			byte ret;

			if ((_reg & 0x4000) > 0)
			{
				int bank = (_reg >> 1) & 3;
				bank &= prg_bank_mask_32k;
				ret = ROM[(bank * 0x8000) + (addr & 0x7FFF)];
			}
			else
			{
				int bank = _reg & 7;
				bank &= prg_bank_mask_16k;
				ret = ROM[(bank * 0x4000) + (addr & 0x3FFF)];
			}
			
			if ((addr & 0xE010) == 0x6000)
			{
				ret |= 0x80;
			}

			return ret;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = _reg & 7;
				bank &= chr_bank_mask_8k;
				return VROM[(bank * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
