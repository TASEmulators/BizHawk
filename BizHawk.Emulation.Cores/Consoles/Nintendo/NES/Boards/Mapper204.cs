using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// 64-in-1
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_204
	public class Mapper204 : NesBoardBase
	{
		private int _reg1, _reg2;

		private int prg_mask_16k, chr_mask_8k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER204":
					break;
				default:
					return false;
			}

			prg_mask_16k = Cart.prg_size / 16 - 1;
			chr_mask_8k = Cart.chr_size / 8 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg1", ref _reg1);
			ser.Sync("reg2", ref _reg2);
		}

		public override void WritePrg(int addr, byte value)
		{
			_reg1 = addr & 0x6;
			_reg2 = _reg1 + ((_reg1 == 0x6) ? 0 : (addr & 1));
			_reg1 = _reg1 + ((_reg1 == 0x6) ? 1 : (addr & 1));

			SetMirrorType(addr.Bit(0) ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x4000)
			{
				return Rom[((_reg2 & prg_mask_16k) * 0x4000) + (addr & 0x3FFF)];
			}

			return Rom[((_reg1 & prg_mask_16k) * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[((_reg2 & chr_mask_8k) * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}
	}
}
