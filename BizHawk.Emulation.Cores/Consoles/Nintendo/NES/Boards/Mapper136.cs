using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Mei Loi Siu Ji (Metal Fighter) (Sachen) [!]
	internal sealed class Mapper136 : NesBoardBase
	{
		private int _chrBankMask_8k;
		private int _chrRegister;

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER136":
					break;
				default:
					return false;
			}

			_chrBankMask_8k = Cart.ChrSize / 8 - 1;
			return true;
		}

		public override void WriteExp(int addr, byte value)
		{
			if ((addr & 0x103) == 0x102)
			{
				_chrRegister = value + 3;
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
				_chrRegister = value + 3;
			}
		}

		public override byte ReadExp(int addr)
		{
			if (addr == 0x100)
			{
				return (byte)((_chrRegister & 0x3F) | (NES.DB & 0xC0));
			}
			else
			{
				return base.ReadExp(addr);
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = _chrRegister & _chrBankMask_8k;
				return Vrom[(bank * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chrRegister", ref _chrRegister);
		}
	}
}
