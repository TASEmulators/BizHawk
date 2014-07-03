using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Common.StringExtensions
{
	public static class StringExtensions
	{
		public static bool IsBinary(this string str)
		{
			return str.All(c => c == '0' || c == '1');
		}

		public static string GetPrecedingString(this string str, string value)
		{
			var index = str.IndexOf(value);

			if (index < 0)
			{
				return null;
			}
			
			if (index == 0)
			{
				return string.Empty;
			}

			return str.Substring(0, index);
		}

		public static bool IsValidRomExtentsion(this string str, params string[] romExtensions)
		{
			var strUpper = str.ToUpper();
			return romExtensions.Any(ext => strUpper.EndsWith(ext.ToUpper()));
		}

		public static bool In(this string str, params string[] options)
		{
			return options.Any(opt => opt.Equals(str, StringComparison.CurrentCultureIgnoreCase));
		}

		public static bool In(this string str, IEnumerable<string> options)
		{
			return options.Any(opt => opt.Equals(str, StringComparison.CurrentCultureIgnoreCase));
		}

		public static bool In<T>(this string str, IEnumerable<T> options, Func<T, string, bool> eval)
		{
			return options.Any(opt => eval(opt, str));
		}

		public static bool NotIn(this string str, params string[] options)
		{
			return options.All(opt => opt.ToLower() != str.ToLower());
		}

		public static bool NotIn(this string str, IEnumerable<string> options)
		{
			return options.All(opt => opt.ToLower() != str.ToLower());
		}

		public static int HowMany(this string str, char c)
		{
			return !string.IsNullOrEmpty(str) ? str.Count(t => t == c) : 0;
		}

		public static int HowMany(this string str, string s)
		{
			var count = 0;
			for (var i = 0; i < (str.Length - s.Length); i++)
			{
				if (str.Substring(i, s.Length) == s)
				{
					count++;
				}
			}

			return count;
		}
	}
}
