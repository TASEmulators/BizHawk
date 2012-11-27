using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Sid : ISoundProvider
	{
		public void GetSamples(short[] samples)
		{
			// produce no samples for now
		}

		public int MaxVolume
		{
			get
			{
				return 255;
			}
			set
			{
				// no change in volume
			}
		}
	}
}
