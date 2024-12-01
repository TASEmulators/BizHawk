using System.ComponentModel;
using System.Linq;
using BizHawk.Common.StringExtensions;
using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("A library exposing standard .NET string methods")]
	public sealed class StringLuaLibrary : LuaLibraryBase
	{
		public override string Name => "bizstring";

		public StringLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

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

		[LuaMethodExample("local s = bizstring.pad_end(\"hm\", 5, 'm'); -- \"hmmmm\"")]
		[LuaMethod("pad_end", "Appends zero or more of pad_char to the end (right) of str until it's at least length chars long. If pad_char is not a string exactly one char long, its first char will be used, or ' ' if it's empty.")]
		public static string PadEnd(
			string str,
			int length,
			string pad_char)
				=> str.PadRight(length, pad_char.Length is 0 ? ' ' : pad_char[0]);

		[LuaMethodExample("local s = bizstring.pad_start(tostring(0x1A3792D4), 11, ' '); -- \"  439849684\"")]
		[LuaMethod("pad_start", "Prepends zero or more of pad_char to the start (left) of str until it's at least length chars long. If pad_char is not a string exactly one char long, its first char will be used, or ' ' if it's empty.")]
		public static string PadStart(
			string str,
			int length,
			string pad_char)
				=> str.PadLeft(length, pad_char.Length is 0 ? ' ' : pad_char[0]);

		[LuaMethodExample("local stbiztri = bizstring.trim( \"Some trim string\t \" );")]
		[LuaMethod("trim", "returns a string that trims whitespace on the left and right ends of the string")]
		public static string Trim(string str)
			=> string.IsNullOrEmpty(str) ? null : str.Trim();

		[LuaMethodExample("local stbizrep = bizstring.replace( \"Some string\", \"Some\", \"Replaced\" );")]
		[LuaMethod("replace", "Returns a string that replaces all occurrences of str2 in str1 with the value of replace")]
		public static string Replace(
			string str,
			string str2,
			string replace)
		{
			return string.IsNullOrEmpty(str)
				? null
				: str.Replace(str2, replace);
		}

		[LuaMethodExample("local stbiztou = bizstring.toupper( \"Some string\" );")]
		[LuaMethod("toupper", "Returns an uppercase version of the given string")]
		public static string ToUpper(string str)
		{
			return string.IsNullOrEmpty(str)
				? null
				: str.ToUpperInvariant();
		}

		[LuaMethodExample("local stbiztol = bizstring.tolower( \"Some string\" );")]
		[LuaMethod("tolower", "Returns an lowercase version of the given string")]
		public static string ToLower(string str)
		{
			return string.IsNullOrEmpty(str)
				? null
				: str.ToLowerInvariant();
		}

		[LuaMethodExample("local stbizsub = bizstring.substring( \"Some string\", 6, 3 );")]
		[LuaMethod("substring", "Returns a string that represents a substring of str starting at position for the specified length")]
		public static string SubString(string str, int position, int length)
		{
			return string.IsNullOrEmpty(str)
				? null
				: str.Substring(position, length);
		}

		[LuaMethodExample("local stbizrem = bizstring.remove( \"Some string\", 4, 5 );")]
		[LuaMethod("remove", "Returns a string that represents str with the given position and count removed")]
		public static string Remove(string str, int position, int count)
		{
			return string.IsNullOrEmpty(str)
				? null
				: str.Remove(position, count);
		}

		[LuaMethodExample("if ( bizstring.contains( \"Some string\", \"Some\") ) then\r\n\tconsole.log( \"Returns whether or not str contains str2\" );\r\nend;")]
		[LuaMethod("contains", "Returns whether or not str contains str2")]
		public static bool Contains(string str, string str2)
			=> !string.IsNullOrEmpty(str) && str.Contains(str2); // don't bother fixing encoding, will match (or not match) regardless

		[LuaMethodExample("if ( bizstring.startswith( \"Some string\", \"Some\") ) then\r\n\tconsole.log( \"Returns whether str starts with str2\" );\r\nend;")]
		[LuaMethod("startswith", "Returns whether str starts with str2")]
		public static bool StartsWith(string str, string str2)
			=> !string.IsNullOrEmpty(str) && str.StartsWithOrdinal(str2); // don't bother fixing encoding, will match (or not match) regardless

		[LuaMethodExample("if ( bizstring.endswith( \"Some string\", \"string\") ) then\r\n\tconsole.log( \"Returns whether str ends wth str2\" );\r\nend;")]
		[LuaMethod("endswith", "Returns whether str ends wth str2")]
		public static bool EndsWith(string str, string str2)
			=> !string.IsNullOrEmpty(str) && str.EndsWithOrdinal(str2); // don't bother fixing encoding, will match (or not match) regardless

		[LuaMethodExample("local nlbizspl = bizstring.split( \"Some, string\", \", \" );")]
		[LuaMethod("split", "Splits str into a Lua-style array using the given separator (consecutive separators in str will NOT create empty entries in the array). If the separator is not a string exactly one char long, ',' will be used.")]
		public LuaTable Split(string str, string separator)
		{
			static char SingleOrElse(string s, char defaultValue)
				=> s?.Length == 1 ? s[0] : defaultValue;
			return string.IsNullOrEmpty(str)
				? _th.CreateTable()
				: _th.ListToTable(str.Split(new[] { SingleOrElse(separator, ',') }, StringSplitOptions.RemoveEmptyEntries).ToList());
		}
	}
}
