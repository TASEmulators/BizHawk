using System;
using System.ComponentModel;
using System.Linq;

using NLua;

namespace BizHawk.Client.Common
{
	[Description("A library exposing standard .NET string methods")]
	public sealed class StringLuaLibrary : LuaLibraryBase
	{
		public override string Name => "bizstring";

		public StringLuaLibrary(Lua lua)
			: base(lua) { }

		public StringLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		[LuaMethodExample("local stbizhex = bizstring.hex( -12345 );")]
		[LuaMethod("hex", "Converts the number to a string representation of the hexadecimal value of the given number")]
		public static string Hex(long num)
		{
			var hex = $"{num:X}";
			if (hex.Length == 1)
			{
				hex = $"0{hex}";
			}

			return hex;
		}

		[LuaMethodExample("local stbizbin = bizstring.binary( -12345 );")]
		[LuaMethod("binary", "Converts the number to a string representation of the binary value of the given number")]
		public static string Binary(long num)
		{
			var binary = Convert.ToString(num, 2);
			binary = binary.TrimStart('0');
			return binary;
		}

		[LuaMethodExample("local stbizoct = bizstring.octal( -12345 );")]
		[LuaMethod("octal", "Converts the number to a string representation of the octal value of the given number")]
		public static string Octal(long num)
		{
			var octal = Convert.ToString(num, 8);
			if (octal.Length == 1)
			{
				octal = $"0{octal}";
			}

			return octal;
		}

		[LuaMethodExample("local stbiztri = bizstring.trim( \"Some trim string\t \" );")]
		[LuaMethod("trim", "returns a string that trims whitespace on the left and right ends of the string")]
		public static string Trim(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}

			return str.Trim();
		}

		[LuaMethodExample("local stbizrep = bizstring.replace( \"Some string\", \"Some\", \"Replaced\" );")]
		[LuaMethod("replace", "Returns a string that replaces all occurances of str2 in str1 with the value of replace")]
		public static string Replace(string str, string str2, string replace)
		{
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}

			return str.Replace(str2, replace);
		}

		[LuaMethodExample("local stbiztou = bizstring.toupper( \"Some string\" );")]
		[LuaMethod("toupper", "Returns an uppercase version of the given string")]
		public static string ToUpper(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}

			return str.ToUpper();
		}

		[LuaMethodExample("local stbiztol = bizstring.tolower( \"Some string\" );")]
		[LuaMethod("tolower", "Returns an lowercase version of the given string")]
		public static string ToLower(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}

			return str.ToLower();
		}

		[LuaMethodExample("local stbizsub = bizstring.substring( \"Some string\", 6, 3 );")]
		[LuaMethod("substring", "Returns a string that represents a substring of str starting at position for the specified length")]
		public static string SubString(string str, int position, int length)
		{
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}

			return str.Substring(position, length);
		}

		[LuaMethodExample("local stbizrem = bizstring.remove( \"Some string\", 4, 5 );")]
		[LuaMethod("remove", "Returns a string that represents str with the given position and count removed")]
		public static string Remove(string str, int position, int count)
		{
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}

			return str.Remove(position, count);
		}

		[LuaMethodExample("if ( bizstring.contains( \"Some string\", \"Some\") ) then\r\n\tconsole.log( \"Returns whether or not str contains str2\" );\r\nend;")]
		[LuaMethod("contains", "Returns whether or not str contains str2")]
		public static bool Contains(string str, string str2)
		{
			if (string.IsNullOrEmpty(str))
			{
				return false;
			}

			return str.Contains(str2);
		}

		[LuaMethodExample("if ( bizstring.startswith( \"Some string\", \"Some\") ) then\r\n\tconsole.log( \"Returns whether str starts with str2\" );\r\nend;")]
		[LuaMethod("startswith", "Returns whether str starts with str2")]
		public static bool StartsWith(string str, string str2)
		{
			if (string.IsNullOrEmpty(str))
			{
				return false;
			}

			return str.StartsWith(str2);
		}

		[LuaMethodExample("if ( bizstring.endswith( \"Some string\", \"string\") ) then\r\n\tconsole.log( \"Returns whether str ends wth str2\" );\r\nend;")]
		[LuaMethod("endswith", "Returns whether str ends wth str2")]
		public static bool EndsWith(string str, string str2)
		{
			if (string.IsNullOrEmpty(str))
			{
				return false;
			}

			return str.EndsWith(str2);
		}

		[LuaMethodExample("local nlbizspl = bizstring.split( \"Some, string\", \", \" );")]
		[LuaMethod("split", "Splits str based on separator into a LuaTable. Separator must be one character!. Same functionality as .NET string.Split() using the RemoveEmptyEntries option")]
		public LuaTable Split(string str, string separator)
		{
			var table = Lua.NewTable();
			if (!string.IsNullOrEmpty(str))
			{
				var splitStr = str.Split(
					new[] { separator.FirstOrDefault() },
					StringSplitOptions.RemoveEmptyEntries);

				for (int i = 0; i < splitStr.Length; i++)
				{
					table[i + 1] = splitStr[i];
				}
			}

			return table;
		}
	}
}
