using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	public static class RandomAdapterConfig
	{
		public static bool FuckupCamera { get; set; } = true;

		public static bool FuckupControls { get; set; } = true;

		public static bool IsEnabled { get; set; } = false;

		public static int Interval { get; set; } = 2500;
	}
}
