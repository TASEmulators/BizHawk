using System;
using System.Collections.Generic;

using BizHawk.Common.BufferExtensions;

//some old junk

namespace BizHawk.Emulation.DiscSystem
{
	sealed public partial class Disc
	{

	

		// converts LBA to minute:second:frame format.
		//TODO - somewhat redundant with Timestamp, which is due for refactoring into something not cue-related
		public static void ConvertLBAtoMSF(int lba, out byte m, out byte s, out byte f)
		{
			lba += 150;
			m = (byte)(lba / 75 / 60);
			s = (byte)((lba - (m * 75 * 60)) / 75);
			f = (byte)(lba - (m * 75 * 60) - (s * 75));
		}

		// converts MSF to LBA offset
		public static int ConvertMSFtoLBA(byte m, byte s, byte f)
		{
			return f + (s * 75) + (m * 75 * 60) - 150;
		}

		
	}
}