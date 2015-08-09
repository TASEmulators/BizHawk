using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper235 : NES.NESBoardBase
	{
		private int _reg;

		private int _prg16BankMask;
		private int _prg32BankMask;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER235":
					break;
				default:
					return false;
			}

			_prg16BankMask = Cart.prg_size / 16 - 1;
			_prg32BankMask = Cart.prg_size / 32 - 1;

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);
		}

		public override byte ReadPRG(int addr)
		{
			if ((_reg & 0x800) > 0)
			{
				int bank;
				if (addr < 0x4000)
				{
					bank = ((_reg & 0x300) >> 3) | ((_reg & 0x1F) << 1) | ((_reg >> 12) & 1);
					
				}
				else
				{
					bank = ((_reg & 0x300) >> 3) | ((_reg & 0x1F) << 1) | ((_reg >> 12) & 1);
					
				}

				return ROM[((bank & _prg16BankMask) * 0x4000) + (addr & 0x3FFF)];
			}
			else
			{
				int bank = ((_reg & 0x300) >> 4) | (_reg & 0x1F);
				return ROM[((bank & _prg32BankMask) * 0x8000) + (addr & 0x7FFF)];
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			_reg = addr;
			SyncMirroring();
		}

		private void SyncMirroring()
		{
			if ((_reg & 0x400) > 0)
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				int type = ((_reg >> 13) & 1) ^ 1;

				if (type == 0)
				{
					SetMirrorType(EMirrorType.Horizontal);
				}
				else
				{
					SetMirrorType(EMirrorType.Vertical);
				}
			}
		}
	}
}
