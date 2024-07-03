using BizHawk.Common;

namespace BizHawk.Tests
{
	public static class PlatformTestUtils
	{
		public static void OnlyRunOnRealUnix()
		{
#if SKIP_PLATFORM_TESTS
			Assert.Inconclusive();
#else
			if (!OSTailoredCode.IsUnixHost || OSTailoredCode.IsWSL) Assert.Inconclusive();
#endif
		}

		public static void OnlyRunOnUnix()
		{
#if SKIP_PLATFORM_TESTS
			Assert.Inconclusive();
#else
			if (!OSTailoredCode.IsUnixHost) Assert.Inconclusive();
#endif
		}

		public static void OnlyRunOnWindows()
		{
#if SKIP_PLATFORM_TESTS
			Assert.Inconclusive();
#else
			if (OSTailoredCode.IsUnixHost) Assert.Inconclusive();
#endif
		}

		public static void RunEverywhere()
		{
#if SKIP_PLATFORM_TESTS
			Assert.Inconclusive();
#endif
		}
	}
}
