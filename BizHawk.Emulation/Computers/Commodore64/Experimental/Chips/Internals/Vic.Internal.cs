using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable 649 //adelikat: Disable dumb warnings until this file is complete
#pragma warning disable 169 //adelikat: Disable dumb warnings until this file is complete

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Experimental
{
	sealed public partial class Vic
	{
		int address;
		bool aec;
		bool ba;
		int data;
		int phi1Data;
		int rasterX;

		public Vic(VicSettings settings)
		{
		}

		public void Clock()
		{
			Render();
		}

		public void Reset()
		{
		}
	}
}
