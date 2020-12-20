using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper254 : MMC3Board_Base
	{
		private byte[] regs = new byte[2];

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER254":
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(regs), ref regs, false);
		}

		public override byte ReadWram(int addr)
		{
			if (regs[0] > 0)
			{
				return Wram[addr];
			}

			return (byte)(Wram[addr] ^ regs[1]);
		}

		public override void WritePrg(int addr, byte value)
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

			base.WritePrg(addr, value);
		}
	}
}
