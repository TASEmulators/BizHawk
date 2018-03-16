using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper063 : NES.NESBoardBase
	{
		int prg0, prg1, prg2, prg3;
		bool open_bus;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER063":
					break;
				default:
					return false;
			}

			Cart.wram_size = 0;
			// not sure on initial mirroring
			SetMirrorType(EMirrorType.Vertical);

			WritePRG(0, 0);
			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			open_bus = ((addr & 0x0300) == 0x0300);

			prg0 = (addr >> 1 & 0x1FC) | ((addr & 0x2) > 0 ? 0x0 : (addr >> 1 & 0x2) | 0x0);
			prg1 = (addr >> 1 & 0x1FC) | ((addr & 0x2) > 0 ? 0x1 : (addr >> 1 & 0x2) | 0x1);
			prg2 = (addr >> 1 & 0x1FC) | ((addr & 0x2) > 0 ? 0x2 : (addr >> 1 & 0x2) | 0x0);
			prg3 = (addr & 0x800) > 0 ? ((addr & 0x07C) | ((addr & 0x06) > 0 ? 0x03 : 0x01)) : ((addr >> 1 & 0x01FC) | ((addr & 0x02) > 0 ? 0x03 : ((addr >> 1 & 0x02) | 0x01)));

			SetMirrorType((addr & 0x01) > 0 ? EMirrorType.Horizontal : EMirrorType.Vertical);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x2000)
			{
				if (open_bus)
				{
					return this.NES.DB;
				}
				else
				{
					return ROM[addr + prg0 * 0x2000];
				}			
			}
			else if (addr < 0x4000)
			{
				if (open_bus)
				{
					return this.NES.DB;
				}
				else
				{
					return ROM[(addr - 0x2000) + prg1 * 0x2000];
				}
			}
			else if (addr < 0x6000)
			{
				return ROM[(addr - 0x4000) + prg2 * 0x2000];
			}
			else
			{
				return ROM[(addr - 0x6000) + prg3 * 0x2000];
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg0", ref prg0);
			ser.Sync("prg1", ref prg1);
			ser.Sync("prg2", ref prg2);
			ser.Sync("prg3", ref prg3);
			ser.Sync("open_bus", ref open_bus);
		}
	}
}
