using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper221 : NesBoardBase
	{
		int[] regs = new int[2];

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER221":
				case "UNIF_BMC-N625092":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(regs), ref regs, false);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr < 0x4000)
			{
				SetMirrorType(addr.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
				regs[0] = addr >> 1 & 0xFF;
			}
			else
			{
				regs[1] = addr & 7;
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank;
			if (addr < 0x4000)
			{
				bank = (regs[0] >> 1 & 0x38) | ((regs[0].Bit(0)) ? ((regs[0] & 0x80) > 0) ? regs[1] : (regs[1] & 0x6) | 0x0 : regs[1]);
			}
			else
			{
				bank = (regs[0] >> 1 & 0x38) | ((regs[0].Bit(0) ? ((regs[0] & 0x80) > 0) ? 0x7 : (regs[1] & 0x6) | 0x1 : regs[1]));
			}

			return Rom[(bank << 14) + (addr & 0x3FFF)];
		}
	}
}
