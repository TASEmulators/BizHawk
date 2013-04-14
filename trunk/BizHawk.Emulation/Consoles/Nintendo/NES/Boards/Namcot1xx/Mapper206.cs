using System;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//various japanese Namcot108 boards plus DEROM
	public class Mapper206 : Namcot108Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NES-DEROM": //R.B.I. baseball (U)
					AssertPrg(64); AssertChr(32,64); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3401": //babel no tou (J)
					AssertPrg(32); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3405": //side pocket (J)
					AssertPrg(128); AssertChr(32); AssertVram(0); AssertWram(0);
					throw new Exception("TODO - test please");
					//break;
				case "NAMCOT-3406": //karnov (J)
					AssertPrg(128); AssertChr(64); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3407": //family jockey (J)
					AssertPrg(32); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3413": //pro yakyuu family stadium (J)
					AssertPrg(64); AssertChr(32); AssertVram(0); AssertWram(0);
					throw new Exception("TODO - test please");
					//break;
				case "NAMCOT-3414": //family boxing (J)
					AssertPrg(64); AssertChr(64); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3415": //mappy-land (J)
					AssertPrg(128); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3416": //dragon slayer IV (J) (aka legacy of the wizard)
					AssertPrg(128); AssertChr(64); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3417": //spy kid (J)
					//apparently this shows up as namcot 108 as well but perhaps there is no difference
					//(is this game older than the other namcot 109 games?)
					AssertPrg(32); AssertChr(32); AssertVram(0); AssertWram(0);
					throw new Exception("TODO - test please");
					//break;
				case "TENGEN-800030": // Pac-Mania (U), etc
					AssertPrg(64, 128); AssertChr(32, 64); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

	}

}