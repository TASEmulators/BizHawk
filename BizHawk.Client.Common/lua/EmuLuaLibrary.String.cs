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
					"hex",
					"binary",
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

		public static string string_trim(object str)
		{
			return Convert.ToString(str);
		}

		public static string string_replace(object str, object str2, object replace)
		{
			return Convert.ToString(str).Replace(Convert.ToString(str2), Convert.ToString(replace));
		}

		public static string string_toupper(object str)
		{
			return Convert.ToString(str).ToUpper();
		}

		public static string string_tolower(object str)
		{
			return Convert.ToString(str).ToLower();
		}

		public static string string_substring(object str, object position, object length)
		{
			return Convert.ToString(str).Substring((int) position,(int) length);
		}

		public static string string_remove(object str, object position, object count)
		{
			return Convert.ToString(str).Remove((int) position,(int) (count));
		}

		public static bool string_contains(object str, object str2)
		{
			return Convert.ToString(str).Contains(Convert.ToString(str2));
		}

		public static bool string_startswith(object str, object str2)
		{
			return Convert.ToString(str).StartsWith(Convert.ToString(str2));
		}

		public static bool string_endswith(object str, object str2)
		{
			return Convert.ToString(str).EndsWith(Convert.ToString(str2));
		}
	}
}

