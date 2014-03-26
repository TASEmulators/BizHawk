using System;
namespace BizHawk.Client.Common
{
	public class StringLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "bizstring"; } }

		[LuaMethodAttributes(
			"hex",
			"Converts the number to a string representation of the hexadecimal value of the given number"
		)]
		public static string Hex(long num)
		{
			var hex = string.Format("{0:X}", num);
			if (hex.Length == 1)
			{
				hex = "0" + hex;
			}

			return hex;
		}

		[LuaMethodAttributes(
			"binary",
			"Converts the number to a string representation of the binary value of the given number"
		)]
		public static string Binary(long num)
		{
			var binary = Convert.ToString(num, 2);
			binary = binary.TrimStart('0');
			return binary;
		}

		[LuaMethodAttributes(
			"octal",
			"Converts the number to a string representation of the octal value of the given number"
		)]
		public static string Octal(long num)
		{
			var octal = Convert.ToString(num, 8);
			if (octal.Length == 1)
			{
				octal = "0" + octal;
			}

			return octal;
		}

		[LuaMethodAttributes(
			"trim",
			"returns a string that trims whitespace on the left and right ends of the string"
		)]
		public static string Trim(string str)
		{
			return str.Trim();
		}

		[LuaMethodAttributes(
			"replace",
			"Returns a string that replaces all occurances of str2 in str1 with the value of replace"
		)]
		public static string Replace(string str, string str2, string replace)
		{
			return str.Replace(str2, replace);
		}

		[LuaMethodAttributes(
			"toupper",
			"Returns an uppercase version of the given string"
		)]
		public static string ToUpper(string str)
		{
			return str.ToUpper();
		}

		[LuaMethodAttributes(
			"tolower",
			"Returns an lowercase version of the given string"
		)]
		public static string ToLower(string str)
		{
			return str.ToLower();
		}

		[LuaMethodAttributes(
			"substring",
			"Returns a string that represents a substring of str starting at position for the specified length"
		)]
		public static string SubString(string str, int position, int length)
		{
			return str.Substring(position, length);
		}

		[LuaMethodAttributes(
			"remove",
			"Returns a string that represents str with the given position and count removed"
		)]
		public static string Remove(string str, int position, int count)
		{
			return str.Remove(position, count);
		}

		[LuaMethodAttributes(
			"contains",
			"Returns whether or not str contains str2"
		)]
		public static bool Contains(string str, string str2)
		{
			return str.Contains(str2);
		}

		[LuaMethodAttributes(
			"startswith",
			"Returns whether str starts with str2"
		)]
		public static bool StartsWith(string str, string str2)
		{
			return str.StartsWith(str2);
		}

		[LuaMethodAttributes(
			"endswith",
			"Returns whether str ends wth str2"
		)]
		public static bool EndsWith(string str, string str2)
		{
			return str.EndsWith(str2);
		}
	}
}
