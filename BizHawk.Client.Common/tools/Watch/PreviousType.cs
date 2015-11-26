using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public abstract partial class Watch
	{
		public enum PreviousType
		{
			Original = 0,
			LastSearch = 1,
			LastFrame = 2,
			LastChange = 3
		}
	}
}
