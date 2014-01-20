using System;
namespace BizHawk.Client.Common
{
	public class StringLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "string"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"binary",
					"hex",
					"octal",
					"trim",
					"replace",
					"toupper",
					"tolower",
					"startswith",
					"substring",
					"contains",
					"endswith",
				};
			}
		}
		public static string string_hex(object num)
		{
			string hex = String.Format("{0:X}", LuaLong(num));
			if (hex.Length == 1) hex = "0" + hex;
			return hex;
		}

		public static string string_binary(object num)
		{
			string binary = Convert.ToString( LuaLong(num), 2);
			binary = binary.TrimStart('0');
			return binary;
		}

		public static string string_octal(object num)
		{
			string octal = Convert.ToString(LuaLong(num),8);
			if (octal.Length == 1) octal = "0" + octal;
			return octal;
		}

		public static string string_trim(string str)
		{
			return str.Trim();
		}

		public static string string_replace(string str, string str2, string replace)
		{
			return str.Replace(str2,replace);
		}

		public static string string_toupper(string str)
		{
			return str.ToUpper();
		}

		public static string string_tolower(string str)
		{
			return str.ToLower();
		}

		public static string string_substring(string str, object position, object length)
		{
			return str.Substring((int) position,(int) length);
		}

		public static string string_remove(string str, object position, object count)
		{
			return str.Remove((int) position,(int) (count));
		}

		public static bool string_contains(string str, string str2)
		{
			return str.Contains(str2);
		}

		public static bool string_startswith(string str, string str2)
		{
			return str.StartsWith(str2);
		}

		public static bool string_endswith(string str, string str2)
		{
			return str.EndsWith(str2);
		}
	}
}

