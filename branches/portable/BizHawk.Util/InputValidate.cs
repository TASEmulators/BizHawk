using System.Text;

namespace BizHawk
{
	/// <summary>
	/// Includes helper functions to validate user input
	/// </summary>
	public static class InputValidate
	{
		public static bool IsValidUnsignedNumber(string Str)
		{
			char[] input = (Str.ToCharArray());
			ASCIIEncoding AE = new ASCIIEncoding();
			// Check each character in the new label to determine if it is a number.
			for (int x = 0; x < input.Length; x++)
			{
				// Encode the character from the character array to its ASCII code.
				byte[] bc = AE.GetBytes(input[x].ToString());

				// Determine if the ASCII code is within the valid range of numerical values.
				if (bc[0] < 47 || bc[0] > 58)
					return false;
			}
			return true;
		}

		public static bool IsValidUnsignedNumber(char c)
		{
			if (c < 47 || c > 58)
				return false;

			return true;
		}

		/// <summary>
		/// Validates all chars are 0-9 or a dash as the first value
		/// </summary>
		/// <param name="Str"></param>
		/// <returns></returns>
		public static bool IsValidSignedNumber(string Str)
		{
			char[] input = (Str.ToCharArray());
			ASCIIEncoding AE = new ASCIIEncoding();
			// Check each character in the new label to determine if it is a number.
			for (int x = 0; x < input.Length; x++)
			{
				// Encode the character from the character array to its ASCII code.
				byte[] bc = AE.GetBytes(input[x].ToString());

				// Determine if the ASCII code is within the valid range of numerical values.
				if (bc[0] > 58)
					return false;

				if (bc[0] < 47)
				{
					if (bc[0] == 45 && x == 0)
						continue;
					else
						return false;
				}

			}
			return true;
		}

		public static bool IsValidSignedNumber(char c)
		{
			if (c == 45) return true;

			if (c < 47 || c > 58)
				return false;

			return true;
		}


		/// <summary>
		/// validates is a Hex number 0-9, A-F (must be capital letters)
		/// </summary>
		/// <returns></returns>
		public static bool IsValidHexNumber(string Str)
		{
			char[] input = (Str.ToCharArray());
			ASCIIEncoding AE = new ASCIIEncoding();
			// Check each character in the new label to determine if it is a number.
			for (int x = 0; x < input.Length; x++)
			{
				// Encode the character from the character array to its ASCII code.
				byte[] bc = AE.GetBytes(input[x].ToString());

				// Determine if the ASCII code is within the valid range of numerical values.
				if (bc[0] < 47) //0
					return false;
				if (bc[0] > 58) //9
				{
					if (bc[0] < 65) //A
						return false;

					if (bc[0] > 70) //F
					{
						if (bc[0] < 97 || bc[0] > 102) //a-f
							return false;
					}
				}
			}
			return true;
		}

		public static bool IsValidHexNumber(char c)
		{
			if (c < 47) return false; //0

			if (c > 58) //9
			{
				if (c < 65) //A
					return false;

				if (c > 70) //F
				{
					if (c < 97 || c > 102) //a-f
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Takes any string and removes any value that is not a valid hex value (0-9, a-f, A-F), returns the remaining characters in uppercase
		/// </summary>
		/// <param name="raw"></param>
		/// <returns></returns>
		public static string DoHexString(string raw)
		{
			raw = raw.ToUpper();
			StringBuilder output = new StringBuilder();
			foreach (char x in raw)
			{
				if (x >= 'A' && x <= 'F')
				{
					output.Append(x);
				}
				else if (x >= '0' && x <= '9')
				{
					output.Append(x);
				}
			}
			return output.ToString();
		}
	}
}
