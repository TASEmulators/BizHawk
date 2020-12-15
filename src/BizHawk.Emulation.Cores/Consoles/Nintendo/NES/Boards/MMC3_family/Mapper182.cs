namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper182 : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER182":
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr & 0xE001)
			{
				case 0x8000: break; //?
				case 0x8001: base.WritePrg(0xA000,value); break;
				case 0xA000:
					value = (byte)scramble_A000(value);
					base.WritePrg(0x8000,value);
					break;
				case 0xA001: break; //?
				case 0xC000: base.WritePrg(0x8001, value); break;
				case 0xC001:
					base.WritePrg(0xC000, value);
					base.WritePrg(0xC001, value);
					break;
				default:
					base.WritePrg(addr, value);
					break;
			}
		}

		static readonly byte[] scramble_table = { 0, 3, 1, 5, 6, 7, 2, 4 };

		private static int scramble_A000(byte val)
		{
			return (val & ~0x7) | scramble_table[val & 0x7];
		}
	}
}