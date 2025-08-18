﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper235 : NesBoardBase
	{
		private int _reg;

		private int _prg16BankMask;
		private int _prg32BankMask;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER235":
					break;
				default:
					return false;
			}

			_prg16BankMask = Cart.PrgSize / 16 - 1;
			_prg32BankMask = Cart.PrgSize / 32 - 1;

			SetMirrorType(Cart.PadH, Cart.PadV);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);
		}

		public override byte ReadPrg(int addr)
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

				return Rom[((bank & _prg16BankMask) * 0x4000) + (addr & 0x3FFF)];
			}
			else
			{
				int bank = ((_reg & 0x300) >> 4) | (_reg & 0x1F);
				return Rom[((bank & _prg32BankMask) * 0x8000) + (addr & 0x7FFF)];
			}
		}

		public override void WritePrg(int addr, byte value)
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
