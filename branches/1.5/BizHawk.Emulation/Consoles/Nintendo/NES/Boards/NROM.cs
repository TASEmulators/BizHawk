namespace BizHawk.Emulation.Consoles.Nintendo
{
	[NES.INESBoardImplPriority]
	public class NROM : NES.NESBoardBase
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
					if (Cart.prg_size <= 32)
						break;
					else
						return false; // NROM-368
				case "MAPPER219": //adelikat: a version of 3D-Block tries to use this ROM, but plays fine as NROM and 219 is undocumented by Disch
					break;

				case "HVC-NROM-256": //super mario bros.
				case "NES-NROM-256": //10 yard fight
				case "HVC-RROM": //balloon fight
				case "HVC-NROM-128":
				case "IREM-NROM-128":
				case "KONAMI-NROM-128":
				case "NES-NROM-128":
				case "NAMCOT-3301":
				case "HVC-HROM": //Donkey Kong Jr. (J)
				case "JALECO-JF-01": //Exerion (J)
				case "UNIF_NES-NROM-256": // Locksmith
				case "UNIF_NES-NROM-128": // various
				case "TENGEN-800003": // ms pac man, others
					AssertPrg(8, 16, 32); AssertChr(8); AssertVram(0); AssertWram(0, 8);
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