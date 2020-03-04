using System;

namespace BizHawk.Client.Common.RamSearchEngine
{
	internal static class Extensions
	{
		public static float ToFloat(this long val)
		{
			var bytes = BitConverter.GetBytes((int)val);
			return BitConverter.ToSingle(bytes, 0);
		}
	}
}
