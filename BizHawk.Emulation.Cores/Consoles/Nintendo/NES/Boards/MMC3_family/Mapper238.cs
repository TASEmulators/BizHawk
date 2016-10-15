using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	public sealed class Mapper238 : MMC3Board_Base
	{
		private readonly int[] lut = { 0x00, 0x02, 0x02, 0x03 };
		private byte reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER238":
				case "UNIF_UNL-603-5052":
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
			ser.Sync("reg", ref reg);
		}

		public override byte ReadEXP(int addr)
		{
			if (addr < 0x20)
			{
				return base.ReadEXP(addr);
			}

			return reg;
		}

		public override byte ReadWRAM(int addr)
		{
			return reg;
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr < 0x20)
			{
				base.WriteEXP(addr, value);
			}

			reg = (byte)lut[value & 3];
		}

		public override void WriteWRAM(int addr, byte value)
		{
			reg = (byte)lut[value & 3];
		}
	}
}
