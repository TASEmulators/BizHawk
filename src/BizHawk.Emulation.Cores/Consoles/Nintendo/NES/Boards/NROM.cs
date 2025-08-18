namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[NesBoardImplPriority]
	internal sealed class NROM : NesBoardBase
	{
		//configuration
		private int prg_byte_mask;

		//state
		//(none)

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure.
			//contrary to expectations, some NROM games may have WRAM if theyve been identified through iNES. lame.
			switch (Cart.BoardType)
			{
				case "MAPPER000":
					break;
				case "MAPPER000_VS":
					// I thibnk this is only for bad/pirated dumps of VS games.
					// No VS game used mapper zero but some were changed to use noraml CHR mapping I think
					NES._isVS = true;
					break;
				case "MAPPER0000-00":
					AssertPrg(8, 16, 32);
					AssertChr(8); AssertVram(0); AssertWram(0, 2, 4, 8);
					break;
				case "BANDAI-NROM-128":
				case "BANDAI-NROM-256":
				case "HVC-HROM": //Donkey Kong Jr. (J)
				case "HVC-NROM-128":
				case "HVC-NROM-256": //super mario bros.
				case "HVC-RROM": //balloon fight
				case "HVC-RTROM":
				case "HVC-SROM":
				case "HVC-STROM":
				case "IREM-NROM-128":
				case "IREM-NROM-256":
				case "JALECO-JF-01": //Exerion (J)
				case "JALECO-JF-02":
				case "JALECO-JF-03":
				case "JALECO-JF-04":
				case "KONAMI-NROM-128":
				case "NAMCOT-3301":
				case "NAMCOT-3302":
				case "NAMCOT-3303":
				case "NAMCOT-3304":
				case "NAMCOT-3305":
				case "NAMCOT-3311":
				case "NAMCOT-3312":
				case "NAMCOT-NROM-128":
				case "NES-NROM-128":
				case "NES-NROM-256": //10 yard fight
				case "NES-RROM-128":
				case "SACHEN-NROM":
				case "SETA-NROM-128":
				case "SUNSOFT-NROM-128":
				case "SUNSOFT-NROM-256":
				case "TAITO-NROM-128":
				case "TAITO-NROM-256":
				case "TENGEN-800003": // ms pac man, others
				case "UNIF_NES-NROM-128": // various
				case "UNIF_NES-NROM-256": // Locksmith
					AssertPrg(8, 16, 32);
					AssertChr(8); AssertVram(0); AssertWram(0, 8);
					break;
				case "AVE-NINA-03":
					// at least one game on this board has none of the mapper chips present,
					// and emulates as simple NROM
					if (Cart.Chips.Count != 0)
						return false;
					AssertPrg(8, 16, 32);
					AssertChr(8); AssertVram(0); AssertWram(0);
					break;

				case "HVC-FAMILYBASIC":
					// we don't emulate the controller, so this won't work
					AssertPrg(32); AssertChr(8); AssertWram(2, 4);
					break;

				default:
					return false;
			}

			prg_byte_mask = (Cart.PrgSize*1024) - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);

			return true;
		}

		public override byte ReadPrg(int addr)
		{
			addr &= prg_byte_mask;
			return Rom[addr];
		}
	}
}
