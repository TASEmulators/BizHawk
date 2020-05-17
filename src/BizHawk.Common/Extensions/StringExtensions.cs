using System;
using System.Linq;

namespace BizHawk.Common.StringExtensions
{
	public static class StringExtensions
	{
		/// <returns>how many times <paramref name="c"/> appears in <paramref name="str"/>, or <c>0</c> if <paramref name="str"/> is null</returns>
		public static int HowMany(this string? str, char c) => string.IsNullOrEmpty(str) ? 0 : str.Count(t => t == c);

		/// <returns><see langword="true"/> iff <paramref name="str"/> appears in <paramref name="options"/> (case-insensitive)</returns>
		public static bool In(this string str, params string[] options) => options.Any(opt => string.Equals(opt, str, StringComparison.InvariantCultureIgnoreCase));
	}
}
