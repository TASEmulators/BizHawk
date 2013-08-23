namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class Mapper182 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER182":
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr & 0xE001)
			{
				case 0x8000: break; //?
				case 0x8001: base.WritePRG(0xA000,value); break;
				case 0xA000:
					value = (byte)scramble_A000(value);
					base.WritePRG(0x8000,value);
					break;
				case 0xA001: break; //?
				case 0xC000: base.WritePRG(0x8001, value); break;
				case 0xC001:
					base.WritePRG(0xC000, value);
					base.WritePRG(0xC001, value);
					break;
				default:
					base.WritePRG(addr, value);
					break;
			}
		}

		static byte[] scramble_table = new byte[] { 0, 3, 1, 5, 6, 7, 2, 4 };
		static int scramble_A000(byte val)
		{
			return (val & ~0x7) | scramble_table[val & 0x7];
		}

	}
}