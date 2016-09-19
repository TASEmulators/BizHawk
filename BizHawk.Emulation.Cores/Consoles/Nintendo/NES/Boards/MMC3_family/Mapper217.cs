using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// TODO
	public sealed class Mapper217 : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(4);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER217":
					break;
				default:
					return false;
			}

			BaseSetup();

			exRegs[0] = 0x00;
			exRegs[1] = 0xFF;
			exRegs[2] = 0x03;
			exRegs[3] = 0x00;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exRegs", ref exRegs);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr == 0x1000)
			{
				exRegs[0] = value;
				// TODO: if value & 0x80, prg 16k mode
			}

			else if (addr == 0x1001)
			{
				exRegs[1] = value;
			}

			else if (addr == 0x1007)
			{
				exRegs[2] = value;
			}

			base.WriteEXP(addr, value);
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			if (exRegs[1].Bit(3))
			{
				return base.Get_PRGBank_8K(addr) & 0x1F;
			}
			else
			{
				return base.Get_PRGBank_8K(addr) & 0x1F | (exRegs[1] & 0x10);
			}
		}
	}
}
