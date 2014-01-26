using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.Common
{
	public class SNESLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "snes"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"getlayer_bg_1",
					"getlayer_bg_2",
					"getlayer_bg_3",
					"getlayer_bg_4",
					"getlayer_obj_1",
					"getlayer_obj_2",
					"getlayer_obj_3",
					"getlayer_obj_4",
					"setlayer_bg_1",
					"setlayer_bg_2",
					"setlayer_bg_3",
					"setlayer_bg_4",
					"setlayer_obj_1",
					"setlayer_obj_2",
					"setlayer_obj_3",
					"setlayer_obj_4"
				};
			}
		}

		[LuaMethodAttributes(
			"getlayer_bg_1",
			"TODO"
		)]
		public static bool GetLayerBg1()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowBG1_1;
		}

		[LuaMethodAttributes(
			"getlayer_bg_2",
			"TODO"
		)]
		public static bool GetLayerBg2()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowBG2_1;
		}

		[LuaMethodAttributes(
			"getlayer_bg_3",
			"TODO"
		)]
		public static bool GetLayerBg3()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowBG3_1;
		}

		[LuaMethodAttributes(
			"getlayer_bg_4",
			"TODO"
		)]
		public static bool GetLayerBg4()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowBG4_1;
		}

		[LuaMethodAttributes(
			"getlayer_obj_1",
			"TODO"
		)]
		public static bool GetLayerObj1()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowOBJ_0;
		}

		[LuaMethodAttributes(
			"getlayer_obj_2",
			"TODO"
		)]
		public static bool GetLayerObj2()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowOBJ_1;
		}

		[LuaMethodAttributes(
			"getlayer_obj_3",
			"TODO"
		)]
		public static bool GetLayerObj3()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowOBJ_2;
		}

		[LuaMethodAttributes(
			"getlayer_obj_4",
			"TODO"
		)]
		public static bool GetLayerObj4()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowOBJ_3;
		}

		[LuaMethodAttributes(
			"setlayer_bg_1",
			"TODO"
		)]
		public static void SetLayerBg1(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowBG1_1 = s.ShowBG1_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_bg_2",
			"TODO"
		)]
		public static void SetLayerBg2(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowBG2_1 = s.ShowBG2_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_bg_3",
			"TODO"
		)]
		public static void SetLayerBg3(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowBG3_1 = s.ShowBG3_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_bg_4",
			"TODO"
		)]
		public static void SetLayerBg4(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowBG4_1 = s.ShowBG4_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_obj_1",
			"TODO"
		)]
		public static void SetLayerObj1(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowOBJ_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_obj_2",
			"TODO"
		)]
		public static void SetLayerObj2(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowOBJ_1 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_obj_3",
			"TODO"
		)]
		public static void SetLayerObj3(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowOBJ_2 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setlayer_obj_4",
			"TODO"
		)]
		public static void SetLayerObj4(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowOBJ_3 = value;
				Global.Emulator.PutSettings(s);
			}
		}
	}
}
