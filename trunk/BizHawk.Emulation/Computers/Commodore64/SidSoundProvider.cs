using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class Sid : ISoundProvider
	{
		public void GetSamples(short[] samples)
		{
		}

		public void DiscardSamples()
		{
		}

		public int MaxVolume
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}
	}
}
