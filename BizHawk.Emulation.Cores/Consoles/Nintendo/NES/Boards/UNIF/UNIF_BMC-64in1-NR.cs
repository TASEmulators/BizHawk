using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	public class UNIF_BMC_64in1_NR : NesBoardBase
	{
		private byte[] regs = new byte[4];

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-64in1NoRepeat":
					break;
				default:
					return false;
			}

			regs[0] = 0x80;
			regs[1] = 0x43;
			regs[2] = 0;
			regs[3] = 0;

			return true;
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1000 && addr <= 0x1003)
			{
				regs[addr & 3] = value;
				SetMirrorType((regs[0] & 0x20) > 0 ? EMirrorType.Horizontal : EMirrorType.Vertical);
			}

			base.WriteExp(addr, value);
		}

		public override void WritePrg(int addr, byte value)
		{
			regs[3] = value;
		}

		public override byte ReadPrg(int addr)
		{
			if ((regs[0] & 0x80) > 0)
			{
				if ((regs[1] & 0x80) > 0)
				{
					return Rom[((regs[1] & 0x1F) * 0x8000) + (addr & 0x7FFF)];
				}
				else
				{
					int bank = ((regs[1] & 0x1f) << 1) | ((regs[1] >> 6) & 1);
					return Rom[(bank * 0x4000) + (addr & 0x3FFF)];
				}
			}
			else
			{
				if (addr < 0x4000)
				{
					return Rom[(addr & 0x3FFF)];
				}
				else
				{
					int bank = ((regs[1] & 0x1f) << 1) | ((regs[1] >> 6) & 1);
					return Rom[(bank * 0x4000) + (addr & 0x3FFF)];
				}
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = (regs[2] << 2) | ((regs[0] >> 1) & 3);
				return Vrom[(bank * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(regs), ref regs, false);
		}
	}
}
