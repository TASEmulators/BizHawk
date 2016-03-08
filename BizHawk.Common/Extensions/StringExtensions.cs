using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Common.StringExtensions
{
	public static class StringExtensions
	{
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
			if (str == null)
			{
				return 0;
			}

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

		#region String and Char validation extensions

		/// <summary>
		/// Validates all chars are 0-9
		/// </summary>
		public static bool IsUnsigned(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return false;
			}

			return str.All(IsUnsigned);
		}

		/// <summary>
		/// Validates the char is 0-9
		/// </summary>
		public static bool IsUnsigned(this char c)
		{
			return char.IsDigit(c);
		}

		/// <summary>
		/// Validates all chars are 0-9, or a dash as the first value
		/// </summary>
		public static bool IsSigned(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return false;
			}

			return str[0].IsSigned() && str.Substring(1).All(IsUnsigned);
		}

		/// <summary>
		/// Validates the char is 0-9 or a dash
		/// </summary>
		public static bool IsSigned(this char c)
		{
			return char.IsDigit(c) || c == '-';
		}

		/// <summary>
		/// Validates all chars are 0-9, A-F or a-f
		/// </summary>
		public static bool IsHex(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return false;
			}

			return str.All(IsHex);
		}

		/// <summary>
		/// Validates the char is 0-9, A-F or a-f
		/// </summary>
		public static bool IsHex(this char c)
		{
			if (char.IsDigit(c))
			{
				return true;
			}

			return char.ToUpperInvariant(c) >= 'A' && char.ToUpperInvariant(c) <= 'F';
		}

		/// <summary>
		/// Validates all chars are 0 or 1
		/// </summary>
		public static bool IsBinary(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return false;
			}

			return str.All(IsBinary);
		}

		/// <summary>
		/// Validates the char is 0 or 1
		/// </summary>
		public static bool IsBinary(this char c)
		{
			return c == '0' || c == '1';
		}

		/// <summary>
		/// Validates all chars are 0-9, a decimal point, and that there is no more than 1 decimal point, can not be signed
		/// </summary>
		public static bool IsFixedPoint(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return false;
			}

			return str.HowMany('.') <= 1
				&& str.All(IsFixedPoint);
		}

		/// <summary>
		/// Validates the char is 0-9, a dash, or a decimal
		/// </summary>
		public static bool IsFixedPoint(this char c)
		{
			return c.IsUnsigned() || c == '.';
		}

		/// <summary>
		/// Validates all chars are 0-9 or decimal, and that there is no more than 1 decimal point, a dash can be the first character
		/// </summary>
		public static bool IsFloat(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return false;
			}

			return str.HowMany('.') <= 1
				&& str[0].IsFloat()
				&& str.Substring(1).All(IsFixedPoint);
		}

		/// <summary>
		/// Validates that the char is 0-9, a dash, or a decimal point
		/// </summary>
		public static bool IsFloat(this char c)
		{
			return c.IsFixedPoint() || c == '-';
		}

		/// <summary>
		/// Takes any string and removes any value that is not a valid binary value (0 or 1)
		/// </summary>
		public static string OnlyBinary(this string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return string.Empty;
			}

			var output = new StringBuilder();

			foreach (var chr in raw)
			{
				if (IsBinary(chr))
				{
					output.Append(chr);
				}
			}

			return output.ToString();
		}

		/// <summary>
		/// Takes any string and removes any value that is not a valid unsigned integer value (0-9)
		/// </summary>
		public static string OnlyUnsigned(this string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return string.Empty;
			}

			var output = new StringBuilder();

			foreach (var chr in raw)
			{
				if (IsUnsigned(chr))
				{
					output.Append(chr);
				}
			}

			return output.ToString();
		}

		/// <summary>
		/// Takes any string and removes any value that is not a valid unsigned integer value (0-9 or -)
		/// Note: a "-" will only be kept if it is the first digit
		/// </summary>
		public static string OnlySigned(this string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return string.Empty;
			}

			var output = new StringBuilder();

			int count = 0;
			foreach (var chr in raw)
			{
				if (count == 0 && chr == '-')
				{
					output.Append(chr);
				}
				else if (IsUnsigned(chr))
				{
					
					output.Append(chr);
				}

				count++;
			}

			return output.ToString();
		}

		/// <summary>
		/// Takes any string and removes any value that is not a valid hex value (0-9, a-f, A-F), returns the remaining characters in uppercase
		/// </summary>
		public static string OnlyHex(this string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return string.Empty;
			}

			var output = new StringBuilder(raw.Length);

			foreach (var chr in raw)
			{
				if (IsHex(chr))
				{
					output.Append(char.ToUpper(chr));
				}
			}

			return output.ToString();
		}

		/// <summary>
		/// Takes any string and removes any value that is not a fixed point value (0-9 or .)
		/// Note: only the first occurance of a . will be kept
		/// </summary>
		public static string OnlyFixedPoint(this string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return string.Empty;
			}

			var output = new StringBuilder();

			var usedDot = false;
			foreach (var chr in raw)
			{
				if (chr == '.')
				{
					if (usedDot)
					{
						continue;
					}

					usedDot = true;
				}

				if (IsFixedPoint(chr))
				{
					output.Append(chr);
				}
			}

			return output.ToString();
		}

		/// <summary>
		/// Takes any string and removes any value that is not a float point value (0-9, -, or .)
		/// Note: - is only valid as the first character, and only the first occurance of a . will be kept
		/// </summary>
		public static string OnlyFloat(this string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return string.Empty;
			}

			var output = new StringBuilder();

			var usedDot = false;
			var count = 0;
			foreach (var chr in raw)
			{
				if (count == 0 && chr == '-')
				{
					output.Append(chr);
				}
				else
				{
					if (chr == '.')
					{
						if (usedDot)
						{
							continue;
						}

						usedDot = true;
					}

					if (IsFixedPoint(chr))
					{
						output.Append(chr);
					}
				}

				count++;
			}

			return output.ToString();
		}

		#endregion
	}
}
