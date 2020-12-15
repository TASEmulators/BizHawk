using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_154
	internal sealed class Mapper154 : Namcot108Board_Base
	{
		//configuration
		private int chr_bank_mask_1k;

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "NAMCOT-3453":
				case "MAPPER154":
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(EMirrorType.OneScreenA);

			chr_bank_mask_1k = Cart.ChrSize - 1;

			return true;
		}

		private int RewireCHR(int addr)
		{
			int bank_1k = mapper.Get_CHRBank_1K(addr);
			bank_1k &= 0x3F;
			if (addr >= 0x1000)
				bank_1k |= 0x40;
			bank_1k &= chr_bank_mask_1k;
			int ofs = addr & ((1 << 10) - 1);
			addr = (bank_1k << 10) + ofs;
			return addr;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000) return Vrom[RewireCHR(addr)];
			else return base.ReadPpu(addr);
		}
		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000) { }
			else base.WritePpu(addr, value);
		}

		public override void WritePrg(int addr, byte value)
		{
			if (value.Bit(6))
			{
				SetMirrorType(EMirrorType.OneScreenB);
			}
			else
			{
				SetMirrorType(EMirrorType.OneScreenA);
			}

			base.WritePrg(addr, value);
		}
	}
}
