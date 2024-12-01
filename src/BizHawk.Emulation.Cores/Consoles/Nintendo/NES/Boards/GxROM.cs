using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//generally mapper66

	//Doraemon
	//Dragon Power
	//Gumshoe
	//Thunder & Lightning
	//Super Mario Bros. + Duck Hunt

	//TODO - bus conflicts

	[NesBoardImplPriority]
	internal sealed class GxROM : NesBoardBase
	{
		//configuraton
		private int prg_mask, chr_mask;

		//state
		private int prg, chr;

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER066":
					AssertPrg(32, 64, 128); AssertChr(8, 16, 32, 64); AssertVram(0); AssertWram(0,8);
					break;
				case "NES-GNROM": // Thunder & Lightning 128 prg
								  // THere are no known with 64 prg
								  // U-Force Power Games (U) (Prototype) uses 32 prg
					AssertPrg(32, 128); AssertChr(8, 16, 32); AssertVram(0); AssertWram(0);
					break;
				case "BANDAI-GNROM":
				case "HVC-GNROM":
				case "NES-MHROM": //Super Mario Bros. / Duck Hunt
					AssertPrg(Cart.BoardType == "NES-MHROM" ? 64 : 128); AssertChr(8, 16, 32); AssertVram(0); AssertWram(0);
					break;

				default:
					return false;
			}

			prg_mask = (Cart.PrgSize / 32) - 1;
			chr_mask = (Cart.ChrSize / 8) - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);

			if(origin == EDetectionOrigin.INES)
				Console.WriteLine("Caution! This board (inferred from iNES) might have wrong mirr.type");


			return true;
		}
		public override byte ReadPrg(int addr)
		{
			return Rom[addr + (prg<<15)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[addr + (chr << 13)];
			}
			else return base.ReadPpu(addr);
		}

		public override void WritePrg(int addr, byte value)
		{
			chr = ((value & 7) & chr_mask);
			prg = (((value>>4) & 3) & prg_mask);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(prg), ref prg);
		}
	}
}
