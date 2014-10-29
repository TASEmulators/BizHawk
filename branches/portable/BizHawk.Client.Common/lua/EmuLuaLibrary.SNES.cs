using System;
using System.ComponentModel;
using BizHawk.Emulation.Common;
using LuaInterface;
using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.Common
{
	[Description("Functions specific to SNESHawk (functions may not run when an SNES game is not loaded)")]
	public sealed class SnesLuaLibrary : LuaLibraryBase
	{
		public SnesLuaLibrary(Lua lua)
			: base(lua) { }

		public SnesLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "snes"; } }

		private static LibsnesCore.SnesSettings GetSettings()
		{
			return ((LibsnesCore)Global.Emulator).GetSettings();
		}

        private static void PutSettings(LibsnesCore.SnesSettings settings) {
            ((LibsnesCore)Global.Emulator).PutSettings(settings);
        }

		[LuaMethodAttributes(
			"getlayer_bg_1",
			"Returns whether the bg 1 layer is displayed"
		)]
		public static bool GetLayerBg1()
		{
			return GetSettings().ShowBG1_1;
		}

		[LuaMethodAttributes(
			"getlayer_bg_2",
			"Returns whether the bg 2 layer is displayed"
		)]
		public static bool GetLayerBg2()
		{
			return GetSettings().ShowBG2_1;
		}

		[LuaMethodAttributes(
			"getlayer_bg_3",
			"Returns whether the bg 3 layer is displayed"
		)]
		public static bool GetLayerBg3()
		{
			return GetSettings().ShowBG3_1;
		}

		[LuaMethodAttributes(
			"getlayer_bg_4",
			"Returns whether the bg 4 layer is displayed"
		)]
		public static bool GetLayerBg4()
		{
			return GetSettings().ShowBG4_1;
		}

		[LuaMethodAttributes(
			"getlayer_obj_1",
			"Returns whether the obj 1 layer is displayed"
		)]
		public static bool GetLayerObj1()
		{
			return GetSettings().ShowOBJ_0;
		}

		[LuaMethodAttributes(
			"getlayer_obj_2",
			"Returns whether the obj 2 layer is displayed"
		)]
		public static bool GetLayerObj2()
		{
			return GetSettings().ShowOBJ_1;
		}

		[LuaMethodAttributes(
			"getlayer_obj_3",
			"Returns whether the obj 3 layer is displayed"
		)]
		public static bool GetLayerObj3()
		{
			return GetSettings().ShowOBJ_2;
		}

		[LuaMethodAttributes(
			"getlayer_obj_4",
			"Returns whether the obj 4 layer is displayed"
		)]
		public static bool GetLayerObj4()
		{
			return GetSettings().ShowOBJ_3;
		}

		[LuaMethodAttributes(
			"setlayer_bg_1",
			"Sets whether the bg 1 layer is displayed"
		)]
		public static void SetLayerBg1(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
                var s = GetSettings();
				s.ShowBG1_1 = s.ShowBG1_0 = value;
				PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_bg_2",
			"Sets whether the bg 2 layer is displayed"
		)]
		public static void SetLayerBg2(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
                var s = GetSettings();
				s.ShowBG2_1 = s.ShowBG2_0 = value;
				PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_bg_3",
			"Sets whether the bg 3 layer is displayed"
		)]
		public static void SetLayerBg3(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
                var s = GetSettings();
				s.ShowBG3_1 = s.ShowBG3_0 = value;
				PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_bg_4",
			"Sets whether the bg 4 layer is displayed"
		)]
		public static void SetLayerBg4(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
                var s = GetSettings();
				s.ShowBG4_1 = s.ShowBG4_0 = value;
				PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_obj_1",
			"Sets whether the obj 1 layer is displayed"
		)]
		public static void SetLayerObj1(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
                var s = GetSettings();
				s.ShowOBJ_0 = value;
				PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_obj_2",
			"Sets whether the obj 2 layer is displayed"
		)]
		public static void SetLayerObj2(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
                var s = GetSettings();
				s.ShowOBJ_1 = value;
				PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_obj_3",
			"Sets whether the obj 3 layer is displayed"
		)]
		public static void SetLayerObj3(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
                var s = GetSettings();
				s.ShowOBJ_2 = value;
				PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_obj_4",
			"Sets whether the obj 4 layer is displayed"
		)]
		public static void SetLayerObj4(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
                var s = GetSettings();
				s.ShowOBJ_3 = value;
				PutSettings(s);
			}
		}
	}
}
