using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper196 : MMC3Board_Base
	{
		// pirate crap
		// behavior from fceumm
		// standard MMC3, plus scrambled address lines to access the MMC3, and a bit of extra hardware
		// adding a 32K prg banking mode

		// config
		private int prg_bank_mask_32k;

		// state
		private bool prgmode;
		private int prgreg;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER196":
					break;
				default:
					return false;
			}

			Cart.WramSize = 0;
			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;
			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.BeginSection(nameof(Mapper196));
			ser.Sync(nameof(prgmode), ref prgmode);
			ser.Sync(nameof(prgreg), ref prgreg);
			ser.EndSection();
		}

		public override void WriteWram(int addr, byte value)
		{
			if (addr < 0x1000)
			{
				if (!prgmode)
					Console.WriteLine("M196: 32K prg activated");
				prgmode = true; // no way to turn this off once activated?
				prgreg = value & 15 | value >> 4;
				prgreg &= prg_bank_mask_32k;
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			// addresses are scrambled
			if (addr >= 0x4000)
			{
				addr = (addr & 0xFFFE) | ((addr >> 2) & 1) | ((addr >> 3) & 1);
			}
			else
			{
				addr = (addr & 0xFFFE) | ((addr >> 2) & 1) | ((addr >> 3) & 1) | ((addr >> 1) & 1);
			}
			base.WritePrg(addr, value);
		}

		public override byte ReadPrg(int addr)
		{
			if (prgmode)
			{
				return Rom[addr | prgreg << 15];
			}
			else
			{
				return base.ReadPrg(addr);
			}
		}

	}
}
