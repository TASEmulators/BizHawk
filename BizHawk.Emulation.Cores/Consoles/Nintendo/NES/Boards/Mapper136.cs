using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Mei Loi Siu Ji (Metal Fighter) (Sachen) [!]
	public sealed class Mapper136 : NES.NESBoardBase
	{
		private int _chrBankMask_8k;
		private int _chrRegister;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER136":
					break;
				default:
					return false;
			}

			_chrBankMask_8k = Cart.chr_size / 8 - 1;
			return true;
		}

		public override void WriteEXP(int addr, byte value)
		{
			if ((addr & 0x103) == 0x102)
			{
				_chrRegister = value + 3;
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
				_chrRegister = value + 3;
			}
		}

		public override byte ReadEXP(int addr)
		{
			if (addr == 0x100)
			{
				return (byte)((_chrRegister & 0x3F) | (NES.DB & 0xC0));
			}
			else
			{
				return base.ReadEXP(addr);
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = _chrRegister & _chrBankMask_8k;
				return VROM[(bank * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chrRegister", ref _chrRegister);
		}
	}
}
