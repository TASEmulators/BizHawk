using System.Text;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Includes helper functions to validate user input
	/// </summary>
	public static class InputValidate
	{
		public static bool IsValidUnsignedNumber(string str)
		{
			var input = str.ToCharArray();
			var asciiEncoding = new ASCIIEncoding();
			
			// Check each character in the new label to determine if it is a number.
			for (int i = 0; i < input.Length; i++)
			{
				// Encode the character from the character array to its ASCII code.
				var bc = asciiEncoding.GetBytes(input[i].ToString());

				// Determine if the ASCII code is within the valid range of numerical values.
				if (bc[0] < 47 || bc[0] > 58)
				{
					return false;
				}
			}

			return true;
		}

		public static bool IsValidUnsignedNumber(char c)
		{
			if (c < 47 || c > 58)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates all chars are 0-9 or a dash as the first value
		/// </summary>
		public static bool IsValidSignedNumber(string str)
		{
			var input = str.Trim().ToCharArray();
			var asciiEncoding = new ASCIIEncoding();

			// Check each character in the new label to determine if it is a number.
			for (int i = 0; i < input.Length; i++)
			{
				// Encode the character from the character array to its ASCII code.
				var bc = asciiEncoding.GetBytes(input[i].ToString());

				// Determine if the ASCII code is within the valid range of numerical values.
				if (bc[0] > 58)
				{
					return false;
				}

				if (bc[0] < 47)
				{
					if (bc[0] == 45 && i == 0)
					{
						continue;
					}
					else
					{
						return false;
					}
				}
			}

			return true;
		}

		public static bool IsValidSignedNumber(char c)
		{
			if (c == 45)
			{
				return true;
			}

			if (c < 47 || c > 58)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// validates is a Hex number 0-9, A-F (must be capital letters)
		/// </summary>
		public static bool IsValidHexNumber(string str)
		{
			var input = str.ToCharArray();
			var asciiEncoding = new ASCIIEncoding();

			// Check each character in the new label to determine if it is a number.
			for (int i = 0; i < input.Length; i++)
			{
				// Encode the character from the character array to its ASCII code.
				var bc = asciiEncoding.GetBytes(input[i].ToString());

				// Determine if the ASCII code is within the valid range of numerical values.
				if (bc[0] < 47) // 0
				{
					return false;
				}

				if (bc[0] > 58) // 9
				{
					if (bc[0] < 65) // A
					{
						return false;
					}

					if (bc[0] > 70) // F
					{
						if (bc[0] < 97 || bc[0] > 102) // a-f
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		public static bool IsValidHexNumber(char c)
		{
			if (c < 47)
			{
				return false; // 0
			}

			if (c > 58) // 9
			{
				if (c < 65) // A
				{
					return false;
				}

				if (c > 70) // F
				{
					if (c < 97 || c > 102) // a-f
					{
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Takes any string and removes any value that is not a valid hex value (0-9, a-f, A-F), returns the remaining characters in uppercase
		/// </summary>
		public static string DoHexString(string raw)
		{
			raw = raw.ToUpper();
			var output = new StringBuilder();
			foreach (var chr in raw)
			{
				if (chr >= 'A' && chr <= 'F')
				{
					output.Append(chr);
				}
				else if (chr >= '0' && chr <= '9')
				{
					output.Append(chr);
				}
			}

			return output.ToString();
		}

		public static bool IsValidBinaryNumber(string str)
		{
			var input = str.ToCharArray();
			var asciiEncoding = new ASCIIEncoding();

			// Check each character in the new label to determine if it is a number.
			for (int i = 0; i < input.Length; i++)
			{
				// Encode the character from the character array to its ASCII code.
				var bc = asciiEncoding.GetBytes(input[i].ToString());

				// Determine if the ASCII code is within the valid range of numerical values.
				if (bc[0] != 48 && bc[0] != 49) // 0 or 1
				{
					return false;
				}
			}

			return true;
		}

		public static bool IsValidBinaryNumber(char c)
		{
			return c == 48 || c == 49;
		}

		/// <summary>
		/// Validates all chars are 0-9 or decimal
		/// </summary>
		public static bool IsValidFixedPointNumber(string str)
		{
			if (StringHelpers.HowMany(str, '.') > 1)
			{
				return false;
			}

			var input = str.Trim().ToCharArray();
			var asciiEncoding = new ASCIIEncoding();
			
			// Check each character in the new label to determine if it is a number.
			for (int i = 0; i < input.Length; i++)
			{
				// Encode the character from the character array to its ASCII code.
				var bc = asciiEncoding.GetBytes(input[i].ToString());

				// Determine if the ASCII code is within the valid range of numerical values.
				if (bc[0] > 58)
				{
					return false;
				}

				if (bc[0] == 46)
				{
					continue;
				}

				if (bc[0] < 48)
				{
					if (bc[0] == 45 && i == 0)
					{
						continue;
					}
					else
					{
						return false;
					}
				}
			}

			return true;
		}

		public static bool IsValidFixedPointNumber(char c)
		{
			if (c == 46 || c == 45)
			{
				return true;
			}

			if (c < 48 || c > 58)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates all chars are 0-9 or decimal or dash as the first character
		/// </summary>
		public static bool IsValidDecimalNumber(string str)
		{
			if (StringHelpers.HowMany(str, '.') > 1)
			{
				return false;
			}

			var input = str.Trim().ToCharArray();
			var asciiEncoding = new ASCIIEncoding();

			// Check each character in the new label to determine if it is a number.
			for (int i = 0; i < input.Length; i++)
			{
				// Encode the character from the character array to its ASCII code.
				var bc = asciiEncoding.GetBytes(input[i].ToString());

				// Determine if the ASCII code is within the valid range of numerical values.
				if (bc[0] > 58)
				{
					return false;
				}

				if (bc[0] == 46)
				{
					continue;
				}

				if (bc[0] < 48)
				{
					if (bc[0] == 45 && i == 0)
					{
						continue;
					}
					else
					{
						return false;
					}
				}
			}

			return true;
		}

		public static bool IsValidDecimalNumber(char c)
		{
			if (c == 45 || c == 46) // 45 = dash, 46 = dot
			{
				return true;
			}
			else if (c < 48 || c > 58)
			{
				return false;
			}

			return true;
		}
	}
}
