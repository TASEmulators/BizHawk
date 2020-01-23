namespace BizHawk.Common
{
	using System;
	using System.Diagnostics;

	internal static class SuperGloballyUniqueID
	{
		private static readonly string StaticPart = $"bizhawk-{Process.GetCurrentProcess().Id}-{Guid.NewGuid()}";

		private static int ctr;

		public static string Next()
		{
			int myctr;
			lock (typeof(SuperGloballyUniqueID)) myctr = ctr++;
			return $"{StaticPart}-{myctr}";
		}
	}
}
