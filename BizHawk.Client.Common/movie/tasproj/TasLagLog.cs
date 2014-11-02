using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class TasLagLog : List<bool>
	{
		public void RemoveFrom(int frame)
		{
			if (frame > 0 && frame < this.Count)
			{
				this.RemoveRange(frame - 1, this.Count - frame);
			}
		}

		public bool? Lagged(int index)
		{
			// Hacky but effective, we haven't record the lag information for the current frame yet
			if (index == Global.Emulator.Frame - 1)
			{
				return Global.Emulator.IsLagFrame;
			}
			
			if (index < this.Count)
			{
				return this[index];
			}

			return null;
		}
	}
}
