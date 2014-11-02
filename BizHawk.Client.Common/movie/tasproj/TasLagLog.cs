using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class TasLagLog : List<bool>
	{
		public void RemoveFrom(int frame)
		{
			if (frame + 1 < this.Count)
			{
				this.RemoveRange(frame + 1, this.Count - frame - 1);
			}
		}

		public bool? Lagged(int index)
		{
			if (index < this.Count)
			{
				return this[index];
			}

			return null;
		}
	}
}
