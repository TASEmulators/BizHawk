using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	internal sealed class Mapper187 : MMC3Board_Base
	{
		private byte[] exRegs = new byte[2];

		private readonly byte[] prot_data = { 0x83, 0x83, 0x42, 0x00 };

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER187":
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
			ser.Sync("expregs", ref exRegs, false);
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr == 0)
			{
				exRegs[1] = 1;
				base.WritePrg(addr, value);
			}
			else if ((addr == 0x0001) && (exRegs[1] > 0))
			{
				base.WritePrg(addr, value);
			}
			else 
				base.WritePrg(addr, value);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr == 0x1000) // 0x5000
			{
				exRegs[0] = value;
			}

			base.WriteExp(addr, value);
		}

		public override void WriteWram(int addr, byte value)
		{
			if (addr == 0x0000)
			{
				exRegs[0] = value;
			}

			base.WriteWram(addr, value);
		}

		public override byte ReadExp(int addr)
		{
			if (addr >= 0x1000)
			{
				return prot_data[exRegs[1] & 3];
			}

			return base.ReadExp(addr);
		}

		private byte MMc3_cmd => (byte)(mmc3.chr_mode ? 0x80 : 0);

		protected override int Get_CHRBank_1K(int addr)
		{
			if ((addr & 0x1000) == ((MMc3_cmd & 0x80) << 5))
			{
				return base.Get_CHRBank_1K(addr) | 0x100;
			}

			return base.Get_CHRBank_1K(addr);
		}

		public override byte ReadPrg(int addr)
		{
			if ((exRegs[0] & 0x80) > 0)
			{
				var bank = exRegs[0] & 0x1F;
				if ((exRegs[0] & 0x20) > 0)
				{
					if ((exRegs[0] & 0x40) > 0)
					{
						return Rom[((bank >> 2) << 15) + addr];
					}

					return Rom[((bank >> 1) << 15) + addr]; // hacky! two mappers in one! need real hw carts to test
				}

				return Rom[(bank << 14) + (addr & 0x3FFF)];
			}

			return base.ReadPrg(addr);
		}
	}
}
