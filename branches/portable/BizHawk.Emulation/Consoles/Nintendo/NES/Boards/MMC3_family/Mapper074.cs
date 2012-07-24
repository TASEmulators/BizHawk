using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper074 : MMC3Board_Base
	{
		/*
			Here are Disch's original notes:
		========================
		=  Mapper 074          =
		========================


		aka:
		--------------------------
		Pirate MMC3 variant


		Example Games:
		--------------------------
		Di 4 Ci - Ji Qi Ren Dai Zhan
		Ji Jia Zhan Shi


		Notes:
		--------------------------
		This mapper is a modified MMC3 (or is based on MMC3?).

		In addition to any CHR-ROM present, there is also an additional 2k of CHR-RAM which is selectable.  CHR pages
		$08 and $09 select CHR-RAM, other pages select CHR-ROM

		Apart from that, this mapper behaves exactly like your typical MMC3.  See mapper 004 for details.
		
		TODO: implement CHR-RAM behavior
		*/

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER074":
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}
	}
}
