using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Challenge of the Dragon (Sachen) [!]
	// Chinese KungFu (Sachen-JAP) [!]
	public sealed class Mapper147 : NES.NESBoardBase
	{
		private int _chrBankMask_8k;
		private int _prgBankMask_32k;

		private int _chrRegister;

		public override bool Configure(NES.EDetectionOrigin origin)
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

		public override void WriteEXP(int addr, byte value)
		{
			if ((addr & 0x103) == 0x102)
			{
				_chrRegister = value;
			}
			else
			{
				base.WriteEXP(addr, value);
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			if ((addr & 0x103) == 0x102)
			{
				_chrRegister = value;
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = _chrRegister >> 3 & 0x0F;
				bank &= _chrBankMask_8k;
				return VROM[(bank * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int bank = ((_chrRegister & 0x80) >> 6) | ((_chrRegister >> 2) & 1);
			bank &= _prgBankMask_32k;
			return ROM[(bank * 0x8000) + (addr & 0x7FFF)];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chrRegister", ref _chrRegister);
		}
	}
}
