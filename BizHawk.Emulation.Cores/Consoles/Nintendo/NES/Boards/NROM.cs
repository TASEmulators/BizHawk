using System;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[NES.INESBoardImplPriority]
	public sealed class NROM : NES.NESBoardBase
	{
		//configuration
		int prg_byte_mask;

		//state
		//(none)

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure.
			//contrary to expectations, some NROM games may have WRAM if theyve been identified through iNES. lame.
			switch (Cart.board_type)
			{
				case "MAPPER000":
					break;
				case "MAPPER000_VS":
					// I thibnk this is only for bad/pirated dumps of VS games.
					// No VS game used mapper zero but some were changed to use noraml CHR mapping I think
					NES._isVS = true;
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
					if (Cart.chips.Count != 0)
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

			prg_byte_mask = (Cart.prg_size*1024) - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			addr &= prg_byte_mask;
			return ROM[addr];
		}
	}
}