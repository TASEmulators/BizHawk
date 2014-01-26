using System;
namespace BizHawk.Client.Common
{
	public class StringLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "string"; } }

		[LuaMethodAttributes(
			"hex",
			"TODO"
		)]
		public static string Hex(object num)
		{
			var hex = String.Format("{0:X}", LuaLong(num));
			if (hex.Length == 1)
			{
				hex = "0" + hex;
			}

			return hex;
		}

		[LuaMethodAttributes(
			"binary",
			"TODO"
		)]
		public static string Binary(object num)
		{
			var binary = Convert.ToString(LuaLong(num), 2);
			binary = binary.TrimStart('0');
			return binary;
		}

		[LuaMethodAttributes(
			"octal",
			"TODO"
		)]
		public static string Octal(object num)
		{
			var octal = Convert.ToString(LuaLong(num), 8);
			if (octal.Length == 1)
			{
				octal = "0" + octal;
			}

			return octal;
		}

		[LuaMethodAttributes(
			"trim",
			"TODO"
		)]
		public static string Trim(string str)
		{
			return str.Trim();
		}

		[LuaMethodAttributes(
			"replace",
			"TODO"
		)]
		public static string Replace(string str, string str2, string replace)
		{
			return str.Replace(str2, replace);
		}

		[LuaMethodAttributes(
			"toupper",
			"TODO"
		)]
		public static string ToUpper(string str)
		{
			return str.ToUpper();
		}

		[LuaMethodAttributes(
			"tolower",
			"TODO"
		)]
		public static string ToLower(string str)
		{
			return str.ToLower();
		}

		[LuaMethodAttributes(
			"substring",
			"TODO"
		)]
		public static string SubString(string str, object position, object length)
		{
			return str.Substring((int)position, (int)length);
		}

		[LuaMethodAttributes(
			"remove",
			"TODO"
		)]
		public static string Remove(string str, object position, object count)
		{
			return str.Remove((int)position, (int)count);
		}

		[LuaMethodAttributes(
			"contains",
			"TODO"
		)]
		public static bool Contains(string str, string str2)
		{
			return str.Contains(str2);
		}

		[LuaMethodAttributes(
			"startswith",
			"TODO"
		)]
		public static bool StartsWith(string str, string str2)
		{
			return str.StartsWith(str2);
		}

		[LuaMethodAttributes(
			"endswith",
			"TODO"
		)]
		public static bool EndsWith(string str, string str2)
		{
			return str.EndsWith(str2);
		}
	}
}
