using System.Text;

namespace BizHawk.Client.Common
{
	using System.Linq;

	/// <summary>
	/// Includes helper functions to validate user input
	/// </summary>
	public static class InputValidate
	{
		/// <summary>
		/// Validates all chars are 0-9
		/// </summary>
		public static bool IsUnsigned(string str)
		{
			return str.All(IsUnsigned);
		}

		/// <summary>
		/// Validates the char is 0-9
		/// </summary>
		public static bool IsUnsigned(char c)
		{
			return char.IsDigit(c);
		}

		/// <summary>
		/// Validates all chars are 0-9, or a dash as the first value
		/// </summary>
		public static bool IsSigned(string str)
		{
			return IsSigned(str[0]) && str.Substring(1).All(IsUnsigned);
		}

		/// <summary>
		/// Validates the char is 0-9 or a dash
		/// </summary>
		public static bool IsSigned(char c)
		{
			return char.IsDigit(c) || c == '-';
		}

		/// <summary>
		/// Validates all chars are 0-9, A-F or a-f
		/// </summary>
		public static bool IsHex(string str)
		{
			return str.All(IsHex);
		}

		/// <summary>
		/// Validates the char is 0-9, A-F or a-f
		/// </summary>
		public static bool IsHex(char c)
		{
			if (char.IsDigit(c))
			{
				return true;
			}

			return char.ToUpper(c) >= 'A' && char.ToUpper(c) <= 'F';
		}

		/// <summary>
		/// Takes any string and removes any value that is not a valid hex value (0-9, a-f, A-F), returns the remaining characters in uppercase
		/// </summary>
		public static string DoHexString(string raw)
		{
			var output = new StringBuilder();

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
		/// Validates all chars are 0 or 1
		/// </summary>
		public static bool IsBinary(string str)
		{
			return str.All(IsBinary);
		}

		/// <summary>
		/// Validates the char is 0 or 1
		/// </summary>
		public static bool IsBinary(char c)
		{
			return c == '0' || c == '1';
		}

		/// <summary>
		/// Validates all chars are 0-9, a decimal point, and that there is no more than 1 decimal point, can not be signed
		/// </summary>
		public static bool IsFixedPoint(string str)
		{
			return StringHelpers.HowMany(str, '.') <= 1 
				&& str.All(IsFixedPoint);
		}

		/// <summary>
		/// Validates the char is 0-9, a dash, or a decimal
		/// </summary>
		public static bool IsFixedPoint(char c)
		{
			return IsUnsigned(c) || c == '.';
		}

		/// <summary>
		/// Validates all chars are 0-9 or decimal, and that there is no more than 1 decimal point, a dash can be the first character
		/// </summary>
		public static bool IsFloat(string str)
		{
			return StringHelpers.HowMany(str, '.') <= 1
				&& IsFloat(str[0])
				&& str.Substring(1).All(IsFixedPoint);
		}

		/// <summary>
		/// Validates that the char is 0-9, a dash, or a decimal point
		/// </summary>
		public static bool IsFloat(char c)
		{
			return IsFixedPoint(c) || c == '-';
		}
	}
}
