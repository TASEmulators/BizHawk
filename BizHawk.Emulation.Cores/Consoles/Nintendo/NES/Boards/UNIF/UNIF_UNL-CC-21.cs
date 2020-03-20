using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class UNIF_UNL_CC_21 : NesBoardBase
	{
		int _reg;
		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-CC-21":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr == 0) // FCEUX says: another one many-in-1 mapper, there is a lot of similar carts with little different wirings
			{
				_reg = value;
			}
			else
			{
				_reg = addr;
			}

			SetMirrorType(addr.Bit(0) ? EMirrorType.OneScreenB : EMirrorType.OneScreenA);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (Cart.chr_size == 8192)
				{
					return Vrom[((_reg & 1) * 0xFFF) + (addr & 0xFFF)];
				}
				else // Some bad, overdumped roms made by cah4e3
				{
					return Vrom[((_reg & 1) * 0x2000) + (addr & 0x1FFF)];
				}
			}

			return base.ReadPpu(addr);
		}
	}
}
