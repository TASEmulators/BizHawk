using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	internal sealed class Mapper123 : MMC3Board_Base
	{
		private byte[] EXPREGS = new byte[8];

		private readonly byte[] sec = { 0, 3, 1, 5, 6, 7, 2, 4 };

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER123": // Nestopia suggests this board is mapper 123, I do not have any ROMs with this ines header info to confirm
				case "UNIF_UNL-H2288":
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
			ser.Sync("expregs", ref EXPREGS, false);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1000)
			{
				if ((addr & 0x800) > 0)
				{
					if ((addr & 1) > 0)
					{
						EXPREGS[1] = value;
					}
					else
					{
						EXPREGS[0] = value;
					}
				}
			}
			else
			{
				base.WriteExp(addr, value);
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				switch ((addr + 0x8000) & 0x8001)
				{
					case 0x8000: base.WritePrg(0x8000, (byte)((value & 0xC0) | sec[value&7])); break;
					case 0x8001: base.WritePrg(0x8001, value); break;
				}
			}
			else
			{
				base.WritePrg(addr, value);
			}
		}

		public override byte ReadPrg(int addr)
		{
			if ((EXPREGS[0] & 0x40) > 0)
			{
				var bank = (EXPREGS[0] & 5) | ((EXPREGS[0] & 8) >> 2) | ((EXPREGS[0] & 0x20) >> 2);
				if ((EXPREGS[0] & 2) > 0)
				{
					return Rom[((bank >> 1) << 15) + (addr & 0x7FFF)];
				}

				return Rom[(bank << 14) + (addr & 0x3FFF)];
			}

			//return (byte)(base.ReadPRG(addr) & 0x3F);
			return base.ReadPrg(addr);
		}
	}
}
