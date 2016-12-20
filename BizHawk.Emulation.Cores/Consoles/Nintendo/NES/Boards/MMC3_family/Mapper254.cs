using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper254 : MMC3Board_Base
	{
		private ByteBuffer regs = new ByteBuffer(2);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER254":
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		public override void Dispose()
		{
			regs.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("regs", ref regs);
		}

		public override byte ReadWRAM(int addr)
		{
			if (regs[0] > 0)
			{
				return WRAM[addr];
			}
			else
			{
				return (byte)(WRAM[addr] ^ regs[1]);
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (addr)
			{
				case 0x0000:
					regs[0] = 0xff;
					break;
				case 0x2001:
					regs[1] = value;
					break;
			}

			base.WritePRG(addr, value);
		}
	}
}
