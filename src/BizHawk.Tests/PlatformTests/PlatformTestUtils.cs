using BizHawk.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests
{
	public static class PlatformTestUtils
	{
		public static void OnlyRunOnRealUnix()
		{
			if (!OSTailoredCode.IsUnixHost || OSTailoredCode.IsWSL) Assert.Inconclusive();
		}

		public static void OnlyRunOnUnix()
		{
			if (!OSTailoredCode.IsUnixHost) Assert.Inconclusive();
		}

		public static void OnlyRunOnWindows()
		{
			if (OSTailoredCode.IsUnixHost) Assert.Inconclusive();
		}
	}
}
