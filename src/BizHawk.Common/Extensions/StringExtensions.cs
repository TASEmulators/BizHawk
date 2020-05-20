using System;
using System.Linq;

namespace BizHawk.Common.StringExtensions
{
	public static class StringExtensions
	{
		/// <returns><see langword="true"/> if <paramref name="str"/> appears in <paramref name="options"/> (case-insensitive)</returns>
		public static bool In(this string str, params string[] options) => options.Any(opt => string.Equals(opt, str, StringComparison.InvariantCultureIgnoreCase));
	}
}
