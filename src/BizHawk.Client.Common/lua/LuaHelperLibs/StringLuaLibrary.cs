using System;
using System.ComponentModel;
using System.Linq;

using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("A library exposing standard .NET string methods")]
	public sealed class StringLuaLibrary : LuaLibraryBase
	{
		public override string Name => "bizstring";

		public StringLuaLibrary(IPlatformLuaLibEnv luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		[LuaMethodExample("local stbizhex = bizstring.hex( -12345 );")]
		[LuaMethod("hex", "Converts the number to a string representation of the hexadecimal value of the given number")]
		[return: LuaASCIIStringParam]
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
		[return: LuaASCIIStringParam]
		public static string Binary(long num)
		{
			var binary = Convert.ToString(num, 2);
			binary = binary.TrimStart('0');
			return binary;
		}

		[LuaMethodExample("local stbizoct = bizstring.octal( -12345 );")]
		[LuaMethod("octal", "Converts the number to a string representation of the octal value of the given number")]
		[return: LuaASCIIStringParam]
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
		[return: LuaArbitraryStringParam]
		public static string Trim([LuaArbitraryStringParam] string str)
			=> string.IsNullOrEmpty(str) ? null : UnFixString(FixString(str).Trim());

		[LuaMethodExample("local stbizrep = bizstring.replace( \"Some string\", \"Some\", \"Replaced\" );")]
		[LuaMethod("replace", "Returns a string that replaces all occurrences of str2 in str1 with the value of replace")]
		[return: LuaArbitraryStringParam]
		public static string Replace(
			[LuaArbitraryStringParam] string str,
			[LuaArbitraryStringParam] string str2,
			[LuaArbitraryStringParam] string replace)
		{
			return string.IsNullOrEmpty(str)
				? null
				: UnFixString(FixString(str).Replace(FixString(str2), FixString(replace)));
		}

		[LuaMethodExample("local stbiztou = bizstring.toupper( \"Some string\" );")]
		[LuaMethod("toupper", "Returns an uppercase version of the given string")]
		[return: LuaArbitraryStringParam]
		public static string ToUpper([LuaArbitraryStringParam] string str)
		{
			return string.IsNullOrEmpty(str)
				? null
				: UnFixString(FixString(str).ToUpperInvariant());
		}

		[LuaMethodExample("local stbiztol = bizstring.tolower( \"Some string\" );")]
		[LuaMethod("tolower", "Returns an lowercase version of the given string")]
		[return: LuaArbitraryStringParam]
		public static string ToLower([LuaArbitraryStringParam] string str)
		{
			return string.IsNullOrEmpty(str)
				? null
				: UnFixString(FixString(str).ToLowerInvariant());
		}

		[LuaMethodExample("local stbizsub = bizstring.substring( \"Some string\", 6, 3 );")]
		[LuaMethod("substring", "Returns a string that represents a substring of str starting at position for the specified length")]
		[return: LuaArbitraryStringParam]
		public static string SubString([LuaArbitraryStringParam] string str, int position, int length)
		{
			return string.IsNullOrEmpty(str)
				? null
				: UnFixString(FixString(str).Substring(position, length));
		}

		[LuaMethodExample("local stbizrem = bizstring.remove( \"Some string\", 4, 5 );")]
		[LuaMethod("remove", "Returns a string that represents str with the given position and count removed")]
		[return: LuaArbitraryStringParam]
		public static string Remove([LuaArbitraryStringParam] string str, int position, int count)
		{
			return string.IsNullOrEmpty(str)
				? null
				: UnFixString(FixString(str).Remove(position, count));
		}

		[LuaMethodExample("if ( bizstring.contains( \"Some string\", \"Some\") ) then\r\n\tconsole.log( \"Returns whether or not str contains str2\" );\r\nend;")]
		[LuaMethod("contains", "Returns whether or not str contains str2")]
		public static bool Contains([LuaArbitraryStringParam] string str, [LuaArbitraryStringParam] string str2)
			=> !string.IsNullOrEmpty(str) && str.Contains(str2); // don't bother fixing encoding, will match (or not match) regardless

		[LuaMethodExample("if ( bizstring.startswith( \"Some string\", \"Some\") ) then\r\n\tconsole.log( \"Returns whether str starts with str2\" );\r\nend;")]
		[LuaMethod("startswith", "Returns whether str starts with str2")]
		public static bool StartsWith([LuaArbitraryStringParam] string str, [LuaArbitraryStringParam] string str2)
			=> !string.IsNullOrEmpty(str) && str.StartsWith(str2); // don't bother fixing encoding, will match (or not match) regardless

		[LuaMethodExample("if ( bizstring.endswith( \"Some string\", \"string\") ) then\r\n\tconsole.log( \"Returns whether str ends wth str2\" );\r\nend;")]
		[LuaMethod("endswith", "Returns whether str ends wth str2")]
		public static bool EndsWith([LuaArbitraryStringParam] string str, [LuaArbitraryStringParam] string str2)
			=> !string.IsNullOrEmpty(str) && str.EndsWith(str2); // don't bother fixing encoding, will match (or not match) regardless

		[LuaMethodExample("local nlbizspl = bizstring.split( \"Some, string\", \", \" );")]
		[LuaMethod("split", "Splits str into a Lua-style array using the given separator (consecutive separators in str will NOT create empty entries in the array). If the separator is not a string exactly one char long, ',' will be used.")]
		[return: LuaArbitraryStringParam]
		public LuaTable Split([LuaArbitraryStringParam] string str, [LuaArbitraryStringParam] string separator)
		{
			static char SingleOrElse(string s, char defaultValue)
				=> s?.Length == 1 ? s[0] : defaultValue;
			return string.IsNullOrEmpty(str)
				? _th.CreateTable()
				: _th.ListToTable(FixString(str).Split(new[] { SingleOrElse(FixString(separator), ',') }, StringSplitOptions.RemoveEmptyEntries)
					.Select(static s => UnFixString(s)).ToList());
		}
	}
}
