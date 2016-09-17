using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from Nestopia src
	public sealed class Mapper121 : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(3);

		private readonly byte[] lut = { 0x83, 0x83, 0x42, 0x00 };

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER121":
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
			ser.Sync("exRegs", ref exRegs);
		}

		public override byte ReadEXP(int addr)
		{
			if (addr >= 0x1000)
			{
				return exRegs[2];
			}
			else
			{
				return base.ReadEXP(addr);
			}
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			if (addr == 0x4000 && exRegs[0] > 0)
			{
				return exRegs[0] & prg_mask;
			}

			else if (addr == 0x6000 && exRegs[1] > 0)
			{
				return exRegs[1] & prg_mask;
			}

			else
			{
				return base.Get_PRGBank_8K(addr);
			}
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr >= 0x1000) // 0x5000-0x5FFF
			{
				exRegs[2] = lut[value & 0x3];
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if ((addr & 3) == 3)
				{
					switch (value)
					{
						case 0x28: exRegs[0] = 0x0C; break;
						case 0x26: exRegs[1] = 0x08; break;
						case 0xAB: exRegs[1] = 0x07; break;
						case 0xEC: exRegs[1] = 0x0D; break;
						case 0xEF: exRegs[1] = 0x0D; break;
						case 0xFF: exRegs[1] = 0x09; break;

						case 0x20: exRegs[1] = 0x13; break;
						case 0x29: exRegs[1] = 0x1B; break;

						default: exRegs[0] = 0x0; exRegs[1] = 0x0; break;
					}
				}
				//else
				//{
				//	base.WritePRG(addr, value);
				//}
			}
			//else
			//{
			//	base.WritePRG(addr, value);
			//}
		}
	}
}
