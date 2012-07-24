using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper192 : MMC3Board_Base
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 192          =
		========================
 
 
		aka:
		--------------------------
		Pirate MMC3 variant
 
 
		Example Game:
		--------------------------
		Ying Lie Qun Xia Zhuan
 
 
		Notes:
		--------------------------
		This mapper is a modified MMC3 (or is based on MMC3?).
 
		In addition to any CHR-ROM present, there is also an additional 4k of CHR-RAM which is selectable.
 
		CHR Pages $08-$0B are CHR-RAM, other pages are CHR-ROM.
 
		Apart from that, this mapper behaves exactly like your typical MMC3.  See mapper 004 for details.
		*/

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER192": //adelikat:  I couldn't find any ROMs that weren't labeled as Mapper 04.  All of these ran fine as far as I could tell, but just in case, I added this.  I'm considering the mapper fully supported until proven otherwise.
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}
	}
}
