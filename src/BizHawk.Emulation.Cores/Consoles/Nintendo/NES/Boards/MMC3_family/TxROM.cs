﻿//http://wiki.nesdev.com/w/index.php/TxROM
//read some background info on namco 108 and DxROM boards here

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[NesBoardImplPriority]
	internal sealed class TxROM : MMC3Board_Base
	{
		public override void WritePrg(int addr, byte value)
		{
			base.WritePrg(addr, value);
			SetMirrorType(mmc3.MirrorType);  //often redundant, but gets the job done
		}

		public override byte[] SaveRam
		{
			get
			{
				if (!Cart.WramBattery) return null;
				return Wram;
				//some boards have a wram that is backed-up or not backed-up. need to handle that somehow
				//(nestopia splits it into NVWRAM and WRAM but i didnt like that at first.. but it may player better with this architecture)
			}
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER116_HACKY":
					break;

				case "TXROM-HOMEBREW": // should this even exist?
					break;
				case "MAPPER004":
				case "MAPPER0004-00":
					if (Cart.InesMirroring == 2) // send these to TVROM
						return false;
					break;
				case "NES-TBROM": //tecmo world cup soccer (DE) [untested]
				case "HVC-TBROM":
					AssertPrg(64); AssertChr(64); AssertVram(0); AssertWram(0);
					AssertBattery(false);
					break;
				case "NES-TEROM": //Adventures of lolo 2
				case "HVC-TEROM": //Adventures of Lolo (J)
					AssertPrg(32); AssertChr(32); AssertVram(0); AssertWram(0);
					AssertBattery(false);
					break;
				case "NES-TFROM": //legacy of the wizard
				case "HVC-TFROM":
					AssertPrg(128); AssertChr(32, 64); AssertVram(0); AssertWram(0);
					AssertBattery(false);
					break;
				case "NES-TGROM": //mega man 4 & 6
				case "HVC-TGROM": //Aa Yakyuu Jinsei Icchokusen (J)
					AssertPrg(128, 256, 512); AssertChr(0); AssertVram(8); AssertWram(0);
					AssertBattery(false);
					break;
				case "NES-TKROM": //kirby's adventure
				case "HVC-TKROM": //Akuma No Shoutaijou (J)
					AssertPrg(128, 256, 512); AssertChr(128, 256); AssertVram(0); AssertWram(8);
					break;
				case "NES-TKEPROM": // kirby's adventure (proto)
					AssertPrg(128, 256, 512); AssertChr(128, 256); AssertVram(0); AssertWram(0, 8);
					break;
				case "NES-TLROM": //mega man 3
				case "KONAMI-TLROM": //Super C
				case "HVC-TLROM": //8 eyes (J)
				case "ACCLAIM-TLROM":

					AssertPrg(128, 256, 512); AssertChr(64, 128, 256); AssertVram(0); AssertWram(0);
					AssertBattery(false);
					break;
				case "UNIF_NES-TLROM": // Gaiapolis (obviously a clone board, but which one?)
					//zero edited this. does it still work?
					break;
				case "HVC-TL1ROM": // untested
				case "NES-TL1ROM": //Double dragon 2
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					AssertBattery(false);
					break;
				case "NES-TL2ROM": //batman (U) ?
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					AssertBattery(false);
					break;
				case "HVC-TNROM": //Final Fantasy 3 (J)
					AssertPrg(128, 256, 512); AssertChr(0, 8); AssertVram(0, 8); AssertWram(8);
					break;
				case "NES-TSROM": //super mario bros. 3 (U)
					AssertPrg(128, 256, 512); AssertChr(128, 256); AssertVram(0); AssertWram(8);
					AssertBattery(false);
					break;
				case "HVC-TSROM": //SMB3 (J)
					AssertPrg(128, 256, 512); AssertChr(128, 256); AssertVram(0); AssertWram(8);
					AssertBattery(false);
					break;
				case "UNIF_TSROM":
					Cart.WramSize = 8;
					Cart.WramBattery = false;
					break;
				case "ACCLAIM-MC-ACC": //alien 3 (U), bart simpson vs the world (U)
					AssertPrg(128, 256); AssertChr(128, 256); AssertVram(0); AssertWram(0);
					AssertBattery(false);
					break;
				case "NES-B4": //batman (U)
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					AssertBattery(false);
					break;


				default:
					return false;
			}

			BaseSetup();

			return true;
		}
	}
}
