using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper063 : NesBoardBase
	{
		private int prg0, prg1, prg2, prg3;
		private bool open_bus;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER063":
					break;
				default:
					return false;
			}

			Cart.WramSize = 0;
			// not sure on initial mirroring
			SetMirrorType(EMirrorType.Vertical);

			WritePrg(0, 0);
			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			open_bus = ((addr & 0x0300) == 0x0300);

			prg0 = (addr >> 1 & 0x1FC) | ((addr & 0x2) > 0 ? 0x0 : (addr >> 1 & 0x2) | 0x0);
			prg1 = (addr >> 1 & 0x1FC) | ((addr & 0x2) > 0 ? 0x1 : (addr >> 1 & 0x2) | 0x1);
			prg2 = (addr >> 1 & 0x1FC) | ((addr & 0x2) > 0 ? 0x2 : (addr >> 1 & 0x2) | 0x0);
			prg3 = (addr & 0x800) > 0 ? ((addr & 0x07C) | ((addr & 0x06) > 0 ? 0x03 : 0x01)) : ((addr >> 1 & 0x01FC) | ((addr & 0x02) > 0 ? 0x03 : ((addr >> 1 & 0x02) | 0x01)));

			SetMirrorType((addr & 0x01) > 0 ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x2000)
			{
				if (open_bus)
				{
					return NES.DB;
				}

				return Rom[addr + prg0 * 0x2000];
			}

			if (addr < 0x4000)
			{
				if (open_bus)
				{
					return NES.DB;
				}

				return Rom[(addr - 0x2000) + prg1 * 0x2000];
			}

			if (addr < 0x6000)
			{
				return Rom[(addr - 0x4000) + prg2 * 0x2000];
			}

			return Rom[(addr - 0x6000) + prg3 * 0x2000];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg0), ref prg0);
			ser.Sync(nameof(prg1), ref prg1);
			ser.Sync(nameof(prg2), ref prg2);
			ser.Sync(nameof(prg3), ref prg3);
			ser.Sync(nameof(open_bus), ref open_bus);
		}
	}
}
