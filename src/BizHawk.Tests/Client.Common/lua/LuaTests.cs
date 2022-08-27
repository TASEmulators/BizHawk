using System;
using System.Drawing;
using System.Text;

using BizHawk.Client.Common;
using BizHawk.Common.StringExtensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Client.Common.Lua
{
	[TestClass]
	public class LuaTests
	{
		private static readonly NLua.Lua LuaInstance = new();
		private static readonly NLuaTableHelper _th = new(LuaInstance, Console.WriteLine);

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

		private static object? ExpectedValue { get; set; }

		[LuaMethod("pass_object", "")]
		public static void PassObject(object? o)
			=> Assert.IsTrue(o == ExpectedValue);

		// seems nil passed over here gets turned into false
		[LuaMethod("pass_bool", "")]
		public static void PassBool(bool? o)
			=> Assert.IsTrue(o == ((bool?)ExpectedValue ?? false));

		[LuaMethod("pass_s8", "")]
		public static void PassS8(sbyte? o)
			=> Assert.IsTrue(o == (sbyte?)ExpectedValue);

		[LuaMethod("pass_u8", "")]
		public static void PassU8(byte? o)
			=> Assert.IsTrue(o == (byte?)ExpectedValue);

		[LuaMethod("pass_s16", "")]
		public static void PassS16(short? o)
			=> Assert.IsTrue(o == (short?)ExpectedValue);

		[LuaMethod("pass_u16", "")]
		public static void PassU16(ushort? o)
			=> Assert.IsTrue(o == (ushort?)ExpectedValue);

		[LuaMethod("pass_s32", "")]
		public static void PassS32(int? o)
			=> Assert.IsTrue(o == (int?)ExpectedValue);

		[LuaMethod("pass_u32", "")]
		public static void PassU32(uint? o)
			=> Assert.IsTrue(o == (uint?)ExpectedValue);

		[LuaMethod("pass_s64", "")]
		public static void PassS64(long? o)
			=> Assert.IsTrue(o == (long?)ExpectedValue);

		[LuaMethod("pass_u64", "")]
		public static void PassU64(ulong? o)
			=> Assert.IsTrue(o == (ulong?)ExpectedValue);

		[LuaMethod("pass_f32", "")]
		public static void PassF32(float? o)
			=> Assert.IsTrue(o == (float?)ExpectedValue);

		[LuaMethod("pass_f64", "")]
		public static void PassF64(double? o)
			=> Assert.IsTrue(o == (double?)ExpectedValue);

		[LuaMethod("pass_f128", "")]
		public static void PassF128(decimal? o)
			=> Assert.IsTrue(o == (decimal?)ExpectedValue);

		[LuaMethod("pass_intptr", "")]
		public static void PassIntPtr(IntPtr? o)
			=> Assert.IsTrue(o == (IntPtr?)ExpectedValue);

		[LuaMethod("pass_uintptr", "")]
		public static void PassUIntPtr(UIntPtr? o)
			=> Assert.IsTrue(o == (UIntPtr?)ExpectedValue);

		[LuaMethod("pass_char", "")]
		public static void PassChar(char? o)
			=> Assert.IsTrue(o == (char?)ExpectedValue);

		[LuaMethod("pass_string", "")]
		public static void PassString(string? o)
		{
			Assert.IsTrue(FixString(o) == (string?)ExpectedValue);
			Assert.IsTrue(o == UnFixString((string?)ExpectedValue));
		}

		[LuaMethod("pass_color", "")]
		public static void PassColor(object? c)
			=> Assert.IsTrue(_th.SafeParseColor(c) == (Color?)ExpectedValue);

		static LuaTests()
		{
			foreach (var mi in typeof(LuaTests).GetMethods())
			{
				var lma = (LuaMethodAttribute?)Attribute.GetCustomAttribute(mi, typeof(LuaMethodAttribute));
				if (lma is not null)
				{
					LuaInstance.RegisterFunction(lma.Name, mi);
				}
			}
		}

		[TestMethod]
		public void Lua_Return_Nil()
		{
			Assert.IsTrue(LuaInstance.DoString("return nil")[0] is null);
		}

		[TestMethod]
		public void Lua_MultiReturn_Nil()
		{
			var ret = LuaInstance.DoString("return nil, nil");
			Assert.IsTrue(ret.Length == 2);
			Assert.IsTrue(ret[0] is null);
			Assert.IsTrue(ret[1] is null);
		}

		[TestMethod]
		public void Lua_Return_Boolean()
		{
			Assert.IsTrue((bool)LuaInstance.DoString("return true")[0]);
			Assert.IsFalse((bool)LuaInstance.DoString("return false")[0]);
		}

		[TestMethod]
		public void Lua_MultiReturn_Boolean()
		{
			var ret = LuaInstance.DoString("return true, false");
			Assert.IsTrue(ret.Length == 2);
			Assert.IsTrue((bool)ret[0]);
			Assert.IsFalse((bool)ret[1]);
		}

		[TestMethod]
		public void Lua_Return_Number()
		{
			Assert.IsTrue((double)LuaInstance.DoString("return 0.0")[0] == 0.0);
		}

		[TestMethod]
		public void Lua_MultiReturn_Number()
		{
			var ret = LuaInstance.DoString("return 0.0, 0.1");
			Assert.IsTrue(ret.Length == 2);
			Assert.IsTrue((double)ret[0] == 0.0);
			Assert.IsTrue((double)ret[1] == 0.1);
		}

		[TestMethod]
		public void Lua_Return_String()
		{
			Assert.IsTrue((string)LuaInstance.DoString("return \"foo\"")[0] == "foo");
		}

		[TestMethod]
		public void Lua_MultiReturn_String()
		{
			var ret = LuaInstance.DoString("return \"foo\", \"bar\"");
			Assert.IsTrue(ret.Length == 2);
			Assert.IsTrue((string)ret[0] == "foo");
			Assert.IsTrue((string)ret[1] == "bar");
		}

		[TestMethod]
		public void Lua_Return_String_Utf8()
		{
			var ret = (string)LuaInstance.DoString("return \"こんにちは\"")[0];
			Assert.IsTrue(FixString(ret) == "こんにちは");
			Assert.IsTrue(ret == UnFixString("こんにちは"));
		}

		[TestMethod]
		public void Lua_Return_Function()
		{
			var ret = (NLua.LuaFunction)LuaInstance.DoString("return function() return 0.123 end")[0];
			Assert.IsTrue((double)ret.Call()[0] == 0.123);
		}

		[TestMethod]
		public void Lua_MultiReturn_Function()
		{
			var ret = LuaInstance.DoString("return function() return 0.123 end, function() return 0.321 end");
			Assert.IsTrue((double)((NLua.LuaFunction)ret[0]).Call()[0] == 0.123);
			Assert.IsTrue((double)((NLua.LuaFunction)ret[1]).Call()[0] == 0.321);
		}

		[TestMethod]
		public void Lua_Return_Table_Array_Style()
		{
			var ret = (NLua.LuaTable)LuaInstance.DoString("return {0.0,1.0,2.0}")[0];
			Assert.IsTrue((double)ret[1.0] == 0.0);
			Assert.IsTrue((double)ret[2.0] == 1.0);
			Assert.IsTrue((double)ret[3.0] == 2.0);
		}

		[TestMethod]
		public void Lua_MultiReturn_Table_Array_Style()
		{
			var ret = LuaInstance.DoString("return {0.0,1.0,2.0}, {2.0,1.0,0.0}");
			var table = (NLua.LuaTable)ret[0];
			Assert.IsTrue((double)table[1.0] == 0.0);
			Assert.IsTrue((double)table[2.0] == 1.0);
			Assert.IsTrue((double)table[3.0] == 2.0);
			table = (NLua.LuaTable)ret[1];
			Assert.IsTrue((double)table[1.0] == 2.0);
			Assert.IsTrue((double)table[2.0] == 1.0);
			Assert.IsTrue((double)table[3.0] == 0.0);
		}

		[TestMethod]
		public void Lua_Return_Table_Dict_Style()
		{
			var ret = (NLua.LuaTable)LuaInstance.DoString("return {[\"foo\"]=0.0,[\"bar\"]=1.0}")[0];
			Assert.IsTrue((double)ret["foo"] == 0.0);
			Assert.IsTrue((double)ret["bar"] == 1.0);
		}

		[TestMethod]
		public void Lua_MultiReturn_Table_Dict_Style()
		{
			var ret = LuaInstance.DoString("return {[\"foo\"]=0.0,[\"bar\"]=1.0}, {[\"bar\"]=0.0,[\"foo\"]=1.0}");
			var table = (NLua.LuaTable)ret[0];
			Assert.IsTrue((double)table["foo"] == 0.0);
			Assert.IsTrue((double)table["bar"] == 1.0);
			table = (NLua.LuaTable)ret[1];
			Assert.IsTrue((double)table["bar"] == 0.0);
			Assert.IsTrue((double)table["foo"] == 1.0);
		}

		[TestMethod]
		public void Net_Argument_Nullable()
		{
			ExpectedValue = null;
			LuaInstance.DoString("pass_object(nil)");
			LuaInstance.DoString("pass_bool(nil)");
			LuaInstance.DoString("pass_s8(nil)");
			LuaInstance.DoString("pass_u8(nil)");
			LuaInstance.DoString("pass_s16(nil)");
			LuaInstance.DoString("pass_u16(nil)");
			LuaInstance.DoString("pass_s32(nil)");
			LuaInstance.DoString("pass_u32(nil)");
			LuaInstance.DoString("pass_s64(nil)");
			LuaInstance.DoString("pass_u64(nil)");
			LuaInstance.DoString("pass_f32(nil)");
			LuaInstance.DoString("pass_f64(nil)");
			LuaInstance.DoString("pass_f128(nil)");
			LuaInstance.DoString("pass_intptr(nil)");
			LuaInstance.DoString("pass_uintptr(nil)");
			LuaInstance.DoString("pass_char(nil)");
			LuaInstance.DoString("pass_string(nil)");
			LuaInstance.DoString("pass_color(nil)");
		}
	}
}
