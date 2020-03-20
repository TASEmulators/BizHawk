using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	internal sealed class Mapper238 : MMC3Board_Base
	{
		private readonly int[] lut = { 0x00, 0x02, 0x02, 0x03 };
		private byte reg;

		public override bool Configure(EDetectionOrigin origin)
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
			ser.Sync(nameof(reg), ref reg);
		}

		public override byte ReadExp(int addr)
		{
			if (addr < 0x20)
			{
				return base.ReadExp(addr);
			}

			return reg;
		}

		public override byte ReadWram(int addr)
		{
			return reg;
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr < 0x20)
			{
				base.WriteExp(addr, value);
			}

			reg = (byte)lut[value & 3];
		}

		public override void WriteWram(int addr, byte value)
		{
			reg = (byte)lut[value & 3];
		}
	}
}
