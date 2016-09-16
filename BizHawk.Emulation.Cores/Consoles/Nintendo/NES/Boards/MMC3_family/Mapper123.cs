using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	public sealed class Mapper123 : MMC3Board_Base
	{
		private ByteBuffer EXPREGS = new ByteBuffer(8);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
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

		public override void WriteEXP(int addr, byte value)
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
				base.WriteEXP(addr, value);
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				switch ((addr + 0x8000) & 0x8001)
				{
					case 0x8000: break;
					case 0x8001: break;
				}
			}
			else
			{
				base.WritePRG(addr, value);
			}
		}

		public override byte ReadPRG(int addr)
		{
			if ((EXPREGS[0] & 0x40) > 0)
			{
				var bank = (EXPREGS[0] & 5) | ((EXPREGS[0] & 8) >> 2) | ((EXPREGS[0] & 0x20) >> 2);
				if ((EXPREGS[0] & 2) > 0)
				{
					return ROM[((bank >> 1) << 15) + (addr & 0x7FFF)];
				}
				else
				{
					return ROM[(bank << 14) + (addr & 0x3FFF)];
				}
			}
			else
			{
				//return (byte)(base.ReadPRG(addr) & 0x3F);
				return base.ReadPRG(addr);
			}
		}
	}
}
