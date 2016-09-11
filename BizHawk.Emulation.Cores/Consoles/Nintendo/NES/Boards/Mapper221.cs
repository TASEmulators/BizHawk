using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper221 : NES.NESBoardBase
	{
		IntBuffer regs = new IntBuffer(2);

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
			ser.Sync("regs", ref regs);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
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

		public override byte ReadPRG(int addr)
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

			return ROM[(bank << 14) + (addr & 0x3FFF)];
		}
	}
}
