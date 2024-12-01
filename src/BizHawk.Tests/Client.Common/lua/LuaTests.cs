using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.Lua
{
#if !SKIP_PLATFORM_TESTS
	[DoNotParallelize]
	[TestClass]
#endif
	public class LuaTests
	{
		private static readonly NLua.Lua LuaInstance = new();
		private static readonly NLuaTableHelper _th = new(LuaInstance, Console.WriteLine);

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

		[LuaMethod("pass_decimal", "")]
		public static void PassDecimal(decimal? o)
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
			=> Assert.IsTrue(o == (string?)ExpectedValue);

		[LuaMethod("pass_color", "")]
		public static void PassColor(object? o)
			=> Assert.IsTrue(_th.SafeParseColor(o)?.ToArgb() == ((Color?)ExpectedValue)?.ToArgb());

		[LuaMethod("pass_table", "")]
		public static void PassTable(NLua.LuaTable? o)
		{
			if (ExpectedValue is null)
			{
				Assert.IsNull(o);
			}
			else
			{
				var t = _th.EnumerateEntries<object, object>(o!);
				var expected = _th.EnumerateEntries<object, object>((NLua.LuaTable)ExpectedValue);
				Assert.IsTrue(!t.Except(expected).Any() && !expected.Except(t).Any());
			}
		}

		private static object? CallackArg { get; set; }

		[LuaMethod("pass_callback", "")]
		public static void PassCallback(NLua.LuaFunction? o)
		{
			if (ExpectedValue is null)
			{
				Assert.IsNull(o);
			}
			else
			{
				switch (CallackArg)
				{
					case null:
						o!.Call();
						break;
					case bool b:
						o!.Call(b);
						break;
					case double d:
						o!.Call(d);
						break;
					case string s:
						o!.Call(s);
						break;
					case NLua.LuaTable t:
						o!.Call(t);
						break;
					case NLua.LuaFunction f:
						o!.Call(f);
						break;
					default:
						Assert.Fail();
						break;
				}
			}
		}

		private static object? ReturnValue { get; set; }

		[LuaMethod("return_object", "")]
		public static object? ReturnObject()
			=> ReturnValue;

		[LuaMethod("return_bool", "")]
		public static bool? ReturnBool()
			=> (bool?)ReturnValue;

		[LuaMethod("return_s8", "")]
		public static sbyte? ReturnS8()
			=> (sbyte?)ReturnValue;

		[LuaMethod("return_u8", "")]
		public static byte? ReturnU8()
			=> (byte?)ReturnValue;

		[LuaMethod("return_s16", "")]
		public static short? ReturnS16()
			=> (short?)ReturnValue;

		[LuaMethod("return_u16", "")]
		public static ushort? ReturnU16()
			=> (ushort?)ReturnValue;

		[LuaMethod("return_s32", "")]
		public static int? ReturnS32()
			=> (int?)ReturnValue;

		[LuaMethod("return_u32", "")]
		public static uint? ReturnU32()
			=> (uint?)ReturnValue;

		[LuaMethod("return_s64", "")]
		public static long? ReturnS64()
			=> (long?)ReturnValue;

		[LuaMethod("return_u64", "")]
		public static ulong? ReturnU64()
			=> (ulong?)ReturnValue;

		[LuaMethod("return_f32", "")]
		public static float? ReturnF32()
			=> (float?)ReturnValue;

		[LuaMethod("return_f64", "")]
		public static double? ReturnF64()
			=> (double?)ReturnValue;

		[LuaMethod("return_decimal", "")]
		public static decimal? ReturnDecimal()
			=> (decimal?)ReturnValue;

		[LuaMethod("return_intptr", "")]
		public static IntPtr? ReturnIntPtr()
			=> (IntPtr?)ReturnValue;

		[LuaMethod("return_uintptr", "")]
		public static UIntPtr? ReturnUIntPtr()
			=> (UIntPtr?)ReturnValue;

		[LuaMethod("return_char", "")]
		public static char? ReturnChar()
			=> (char?)ReturnValue;

		[LuaMethod("return_string", "")]
		public static string? ReturnString()
			=> (string?)ReturnValue;

		[LuaMethod("return_table", "")]
		public static NLua.LuaTable? ReturnTable()
			=> (NLua.LuaTable?)ReturnValue;

		[LuaMethod("return_color", "")]
		public static Color? ReturnColor()
			=> (Color?)ExpectedValue;

		[LuaMethod("return_callback", "")]
		public static NLua.LuaFunction? ReturnCallback()
			=> (NLua.LuaFunction?)ReturnValue;

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
			Assert.IsTrue(ret == "こんにちは");
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
		public void Lua_Argument_Nil()
		{
			ExpectedValue = true;
			CallackArg = null;
			LuaInstance.DoString("pass_callback(function(foo) pass_bool(foo == nil) end)");
		}

		[TestMethod]
		public void Lua_Argument_Boolean()
		{
			ExpectedValue = true;
			CallackArg = true;
			LuaInstance.DoString("pass_callback(function(foo) pass_bool(foo == true) end)");
			CallackArg = false;
			LuaInstance.DoString("pass_callback(function(foo) pass_bool(foo == false) end)");
		}

		[TestMethod]
		public void Lua_Argument_Number()
		{
			ExpectedValue = true;
			CallackArg = 123.0;
			LuaInstance.DoString("pass_callback(function(foo) pass_bool(foo == 123.0) end)");
		}

		[TestMethod]
		public void Lua_Argument_String()
		{
			ExpectedValue = true;
			CallackArg = "foobar";
			LuaInstance.DoString("pass_callback(function(foo) pass_bool(foo == \"foobar\") end)");
		}

		[TestMethod]
		public void Lua_Argument_String_Utf8()
		{
			ExpectedValue = true;
			CallackArg = "こんにちは";
			LuaInstance.DoString("pass_callback(function(foo) pass_bool(foo == \"こんにちは\") end)");
		}

		[TestMethod]
		public void Lua_Argument_Function()
		{
			ExpectedValue = true;

			//this doesn't work
			//seems that this gets interpreted as userdata for some reason?
			//Action<object> cb = o => Assert.IsTrue((double)o == 0.123);
			//CallackArg = LuaInstance.RegisterFunction("__INTERNAL_CALLBACK__", cb.GetMethodInfo());

			CallackArg = LuaInstance.DoString("return function(foo) pass_bool(foo == 0.123) end")[0];
			LuaInstance.DoString("pass_callback(function(foo) foo(0.123) end)");
		}

		[TestMethod]
		public void Lua_Argument_Table_FromList()
		{
			ExpectedValue = true;
			CallackArg = _th.ListToTable(new List<double>
			{
				0.123,
				0.321
			});
			LuaInstance.DoString("pass_callback(function(foo) pass_bool(foo[1] == 0.123) pass_bool(foo[2] == 0.321) end)");
		}

		[TestMethod]
		public void Lua_Argument_Table_FromDict()
		{
			ExpectedValue = true;
			CallackArg = _th.DictToTable(new Dictionary<string, double>
			{
				["foo"] = 0.123,
				["bar"] = 0.321
			});
			LuaInstance.DoString("pass_callback(function(foo) pass_bool(foo[\"foo\"] == 0.123) pass_bool(foo[\"bar\"] == 0.321) end)");
		}

		[TestMethod]
		public void Net_Return_Nullable()
		{
			ReturnValue = null;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_object() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_bool() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s8() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_u8() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s16() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_u16() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s32() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_u32() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s64() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_u64() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_f32() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_f64() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_decimal() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_intptr() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_uintptr() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_char() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_string() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_table() == nil")[0]);
			Assert.IsTrue((bool)LuaInstance.DoString("return return_callback() == nil")[0]);
		}

		[TestMethod]
		public void Net_Return_Bool()
		{
			ReturnValue = false;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_bool() == false")[0]);
			ReturnValue = true;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_bool() == true")[0]);
		}

		[TestMethod]
		public void Net_Return_S8()
		{
			ReturnValue = (sbyte)123;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s8() == 123")[0]);
			ReturnValue = (sbyte)-123;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s8() == -123")[0]);
		}

		[TestMethod]
		public void Net_Return_U8()
		{
			ReturnValue = (byte)123;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_u8() == 123")[0]);
		}

		[TestMethod]
		public void Net_Return_S16()
		{
			ReturnValue = (short)123;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s16() == 123")[0]);
			ReturnValue = (short)-123;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s16() == -123")[0]);
		}

		[TestMethod]
		public void Net_Return_U16()
		{
			ReturnValue = (ushort)123;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_u16() == 123")[0]);
		}

		[TestMethod]
		public void Net_Return_S32()
		{
			ReturnValue = 123;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s32() == 123")[0]);
			ReturnValue = -123;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s32() == -123")[0]);
		}

		[TestMethod]
		public void Net_Return_U32()
		{
			ReturnValue = 123U;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_u32() == 123")[0]);
		}

		[TestMethod]
		public void Net_Return_S64()
		{
			ReturnValue = 123L;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s64() == 123")[0]);
			ReturnValue = -123L;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_s64() == -123")[0]);
		}

		[TestMethod]
		public void Net_Return_U64()
		{
			ReturnValue = 123UL;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_u64() == 123")[0]);
		}

		[TestMethod]
		public void Net_Return_F32()
		{
			ReturnValue = 123.0F;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_f32() == 123.0")[0]);
			ReturnValue = -123.0F;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_f32() == -123.0")[0]);
		}

		[TestMethod]
		public void Net_Return_F64()
		{
			ReturnValue = 123.0;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_f64() == 123.0")[0]);
			ReturnValue = -123.0;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_f64() == -123.0")[0]);
		}

		[TestMethod]
		public void Net_Return_Decimal()
		{
			ReturnValue = 123.0M;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_decimal() == 123.0")[0]);
			ReturnValue = -123.0M;
			Assert.IsTrue((bool)LuaInstance.DoString("return return_decimal() == -123.0")[0]);
		}

		[TestMethod]
		public void Net_Return_IntPtr()
		{
			ReturnValue = ExpectedValue = (IntPtr)123;
			LuaInstance.DoString("pass_intptr(return_intptr())");
			ReturnValue = ExpectedValue = (IntPtr)(-123);
			LuaInstance.DoString("pass_intptr(return_intptr())");
		}

		[TestMethod]
		public void Net_Return_UIntPtr()
		{
			ReturnValue = ExpectedValue = (UIntPtr)123;
			LuaInstance.DoString("pass_uintptr(return_uintptr())");
		}

		[TestMethod]
		public void Net_Return_Char()
		{
			ReturnValue = 'a';
			Assert.IsTrue((bool)LuaInstance.DoString($"return return_char() == {(ushort)'a'}")[0]);
			ReturnValue = 'こ';
			Assert.IsTrue((bool)LuaInstance.DoString($"return return_char() == {(ushort)'こ'}")[0]);
		}

		[TestMethod]
		public void Net_Return_String()
		{
			ReturnValue = "foobar";
			Assert.IsTrue((bool)LuaInstance.DoString("return return_string() == \"foobar\"")[0]);
		}

		[TestMethod]
		public void Net_Return_String_Utf8()
		{
			ReturnValue = "こんにちは";
			Assert.IsTrue((bool)LuaInstance.DoString("return return_string() == \"こんにちは\"")[0]);
		}

		[TestMethod]
		public void Net_Return_Color()
		{
			ReturnValue = ExpectedValue = Color.Aqua;
			LuaInstance.DoString("pass_color(return_color())");
		}

		[TestMethod]
		public void Net_Return_Table_FromList()
		{
			ReturnValue = _th.ListToTable(new List<double>
			{
				0.123,
				0.321
			});
			Assert.IsTrue((bool)LuaInstance.DoString("local t = return_table() return (t[1] == 0.123) and (t[2] == 0.321)")[0]);
		}

		[TestMethod]
		public void Net_Return_Table_FromDict()
		{
			ReturnValue = _th.DictToTable(new Dictionary<string, double>
			{
				["foo"] = 0.123,
				["bar"] = 0.321
			});
			Assert.IsTrue((bool)LuaInstance.DoString("local t = return_table() return (t[\"foo\"] == 0.123) and (t[\"bar\"] == 0.321)")[0]);
		}

		[TestMethod]
		public void Net_Return_LuaFunction()
		{
			ReturnValue = LuaInstance.DoString("return function() return 0.123 end")[0];
			Assert.IsTrue((bool)LuaInstance.DoString("print(return_callback()) return return_callback()() == 0.123")[0]);
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
			LuaInstance.DoString("pass_decimal(nil)");
			LuaInstance.DoString("pass_intptr(nil)");
			LuaInstance.DoString("pass_uintptr(nil)");
			LuaInstance.DoString("pass_char(nil)");
			LuaInstance.DoString("pass_string(nil)");
			LuaInstance.DoString("pass_color(nil)");
			LuaInstance.DoString("pass_table(nil)");
			LuaInstance.DoString("pass_callback(nil)");
		}

		[TestMethod]
		public void Net_Argument_Bool()
		{
			ExpectedValue = false;
			LuaInstance.DoString("pass_bool(false)");
			ExpectedValue = true;
			LuaInstance.DoString("pass_bool(true)");
		}

		[TestMethod]
		public void Net_Argument_S8()
		{
			ExpectedValue = (sbyte)123;
			LuaInstance.DoString("pass_s8(123)");
			ExpectedValue = (sbyte)-123;
			LuaInstance.DoString("pass_s8(-123)");
		}

		[TestMethod]
		public void Net_Argument_U8()
		{
			ExpectedValue = (byte)123;
			LuaInstance.DoString("pass_u8(123)");
		}

		[TestMethod]
		public void Net_Argument_S16()
		{
			ExpectedValue = (short)123;
			LuaInstance.DoString("pass_s16(123)");
			ExpectedValue = (short)-123;
			LuaInstance.DoString("pass_s16(-123)");
		}

		[TestMethod]
		public void Net_Argument_U16()
		{
			ExpectedValue = (ushort)123;
			LuaInstance.DoString("pass_u16(123)");
		}

		[TestMethod]
		public void Net_Argument_S32()
		{
			ExpectedValue = 123;
			LuaInstance.DoString("pass_s32(123)");
			ExpectedValue = -123;
			LuaInstance.DoString("pass_s32(-123)");
		}

		[TestMethod]
		public void Net_Argument_U32()
		{
			ExpectedValue = 123U;
			LuaInstance.DoString("pass_u32(123)");
		}

		[TestMethod]
		public void Net_Argument_S64()
		{
			ExpectedValue = 123L;
			LuaInstance.DoString("pass_s64(123)");
			ExpectedValue = -123L;
			LuaInstance.DoString("pass_s64(-123)");
		}

		[TestMethod]
		public void Net_Argument_U64()
		{
			ExpectedValue = 123UL;
			LuaInstance.DoString("pass_u64(123)");
		}

		[TestMethod]
		public void Net_Argument_F32()
		{
			ExpectedValue = 123.0F;
			LuaInstance.DoString("pass_f32(123.0)");
			ExpectedValue = -123.0F;
			LuaInstance.DoString("pass_f32(-123.0)");
		}

		[TestMethod]
		public void Net_Argument_F64()
		{
			ExpectedValue = 123.0;
			LuaInstance.DoString("pass_f64(123.0)");
			ExpectedValue = -123.0;
			LuaInstance.DoString("pass_f64(-123.0)");
		}

		[TestMethod]
		public void Net_Argument_Decimal()
		{
			ExpectedValue = 123.0M;
			LuaInstance.DoString("pass_decimal(123.0)");
			ExpectedValue = -123.0M;
			LuaInstance.DoString("pass_decimal(-123.0)");
		}

		// these don't work, although there is reasoning behind this
		// IntPtr/UIntPtr are meant as handles to "userdata"
		// so raw integers result in "Invalid arguments to method call"

		/*[TestMethod]
		public void Net_Argument_IntPtr()
		{
			ExpectedValue = (IntPtr)123;
			LuaInstance.DoString("pass_intptr(123)");
			ExpectedValue = (IntPtr)(-123);
			LuaInstance.DoString("pass_intptr(-123)");
		}*/

		/*[TestMethod]
		public void Net_Argument_UIntPtr()
		{
			ExpectedValue = (UIntPtr)123;
			LuaInstance.DoString("pass_uintptr(123)");
		}*/

		[TestMethod]
		public void Net_Argument_Char()
		{
			ExpectedValue = 'a';
			LuaInstance.DoString($"pass_char({(ushort)'a'})");
			ExpectedValue = 'こ';
			LuaInstance.DoString($"pass_char({(ushort)'こ'})");
		}

		[TestMethod]
		public void Net_Argument_String()
		{
			ExpectedValue = "foobar";
			LuaInstance.DoString($"pass_string(\"foobar\")");
		}

		[TestMethod]
		public void Net_Argument_String_Utf8()
		{
			ExpectedValue = "こんにちは";
			LuaInstance.DoString($"pass_string(\"こんにちは\")");
		}

		[TestMethod]
		public void Net_Argument_String_Implicit_Number_Conversion()
		{
			ExpectedValue = "123";
			LuaInstance.DoString("pass_string(123)");
			ExpectedValue = "-123";
			LuaInstance.DoString("pass_string(-123)");
			ExpectedValue = "0.321";
			LuaInstance.DoString("pass_string(0.321)");
			ExpectedValue = "-0.321";
			LuaInstance.DoString("pass_string(-0.321)");
		}

		[TestMethod]
		public void Net_Argument_Color()
		{
			ExpectedValue = Color.Aqua;
			LuaInstance.DoString("pass_color(\"Aqua\")");
			LuaInstance.DoString("pass_color(\"#FF00FFFF\")");
			LuaInstance.DoString("pass_color(0xFF00FFFF)");
			LuaInstance.DoString("pass_color(4278255615.0)");
			// implicit 0xFF for Alpha when not provided
			LuaInstance.DoString("pass_color(\"#00FFFF\")");
		}

		[TestMethod]
		public void Net_Argument_Table_FromList()
		{
			ExpectedValue = _th.ListToTable(new List<double>
			{
				0.123,
				0.321
			});
			LuaInstance.DoString("pass_table({0.123,0.321})");
		}

		[TestMethod]
		public void Net_Argument_Table_FromDict()
		{
			ExpectedValue = _th.DictToTable(new Dictionary<string, double>
			{
				["foo"] = 0.123,
				["bar"] = 0.321
			});
			LuaInstance.DoString("pass_table({[\"foo\"]=0.123,[\"bar\"]=0.321})");
		}

		[TestMethod]
		public void Net_Argument_LuaFunction()
		{
			ExpectedValue = 123.0;
			LuaInstance.DoString("pass_callback(function() pass_f64(123.0) end)");
		}
	}
}
