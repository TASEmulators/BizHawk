using System;
using System.Text;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common.StringExtensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Client.Common.Lua
{
	[TestClass]
	public class LuaTests
	{
		private static readonly NLua.Lua LuaInstance = new();

		// NOTE: NET6 changed Default encoding behavior.
		// It no longer represents the default ANSI encoding
		// Rather it now represents the "default encoding for this NET implementation"
		// on Win10 this seems to just be an alias to UTF8, so it doesn't work here

		public static string? FixString(string? s)
			=> s is null
				? null
				: Encoding.UTF8.GetString(s.ToCharCodepointArray());

		public static string? UnFixString(string? s)
			=> s is null
				? null
				: StringExtensions.CharCodepointsToString(Encoding.UTF8.GetBytes(s));

		[TestMethod]
		public void Return_Nil_Literal()
		{
			Assert.IsTrue(LuaInstance.DoString("return nil")[0] is null);
		}

		[TestMethod]
		public void Return_Nil_Literal_MultiReturn()
		{
			var ret = LuaInstance.DoString("return nil, nil");
			Assert.IsTrue(ret.Length == 2);
			Assert.IsTrue(ret[0] is null);
			Assert.IsTrue(ret[1] is null);
		}

		[TestMethod]
		public void Return_Boolean_Literal()
		{
			Assert.IsTrue((bool)LuaInstance.DoString("return true")[0]);
			Assert.IsFalse((bool)LuaInstance.DoString("return false")[0]);
		}

		[TestMethod]
		public void Return_Boolean_Literal_MultiReturn()
		{
			var ret = LuaInstance.DoString("return true, false");
			Assert.IsTrue(ret.Length == 2);
			Assert.IsTrue((bool)ret[0]);
			Assert.IsFalse((bool)ret[1]);
		}

		[TestMethod]
		public void Return_Number_Literal()
		{
			Assert.IsTrue((double)LuaInstance.DoString("return 0.0")[0] == 0.0);
		}

		[TestMethod]
		public void Return_Number_Literal_MultiReturn()
		{
			var ret = LuaInstance.DoString("return 0.0, 0.1");
			Assert.IsTrue(ret.Length == 2);
			Assert.IsTrue((double)ret[0] == 0.0);
			Assert.IsTrue((double)ret[1] == 0.1);
		}

		[TestMethod]
		public void Return_String_Literal()
		{
			Assert.IsTrue((string)LuaInstance.DoString("return \"foo\"")[0] == "foo");
		}

		[TestMethod]
		public void Return_String_Literal_MultiReturn()
		{
			var ret = LuaInstance.DoString("return \"foo\", \"bar\"");
			Assert.IsTrue(ret.Length == 2);
			Assert.IsTrue((string)ret[0] == "foo");
			Assert.IsTrue((string)ret[1] == "bar");
		}

		// this is an unfair test, seems that the string returned is marshalled back as ANSI
		// thus the string is left in very bad shape
		// it can be fixed however, as shown

		[TestMethod]
		public void Return_String_Utf8()
		{
			var ret = (string)LuaInstance.DoString("return \"こんにちは\"")[0];
			Assert.IsTrue(FixString(ret) == "こんにちは");
			Assert.IsTrue(ret == UnFixString("こんにちは"));
		}

		[TestMethod]
		public void Return_Function_Literal()
		{
			var ret = (NLua.LuaFunction)LuaInstance.DoString("return function() return 0.123 end")[0];
			Assert.IsTrue((double)ret.Call()[0] == 0.123);
		}

		[TestMethod]
		public void Return_Function_Literal_MultiReturn()
		{
			var ret = LuaInstance.DoString("return function() return 0.123 end, function() return 0.321 end");
			Assert.IsTrue((double)((NLua.LuaFunction)ret[0]).Call()[0] == 0.123);
			Assert.IsTrue((double)((NLua.LuaFunction)ret[1]).Call()[0] == 0.321);
		}
	}
}
