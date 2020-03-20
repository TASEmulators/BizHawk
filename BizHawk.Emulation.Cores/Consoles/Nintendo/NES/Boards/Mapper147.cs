using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Challenge of the Dragon (Sachen) [!]
	// Chinese KungFu (Sachen-JAP) [!]
	internal sealed class Mapper147 : NesBoardBase
	{
		private int _chrBankMask_8k;
		private int _prgBankMask_32k;

		private int _chrRegister;

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER147":
				case "UNIF_UNL-TC-U01-1.5M":
					break;
				default:
					return false;
			}

			_chrBankMask_8k = Cart.chr_size / 8 - 1;
			_prgBankMask_32k = Cart.prg_size / 32 - 1;

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override void WriteExp(int addr, byte value)
		{
			if ((addr & 0x103) == 0x102)
			{
				_chrRegister = value;
			}
			else
			{
				base.WriteExp(addr, value);
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			if ((addr & 0x103) == 0x102)
			{
				_chrRegister = value;
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = _chrRegister >> 3 & 0x0F;
				bank &= _chrBankMask_8k;
				return Vrom[(bank * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			int bank = ((_chrRegister & 0x80) >> 6) | ((_chrRegister >> 2) & 1);
			bank &= _prgBankMask_32k;
			return Rom[(bank * 0x8000) + (addr & 0x7FFF)];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chrRegister", ref _chrRegister);
		}
	}
}
