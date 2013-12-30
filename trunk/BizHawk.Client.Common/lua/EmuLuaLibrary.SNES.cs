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

		public static bool snes_getlayer_bg_1()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowBG1_1;
		}

		public static bool snes_getlayer_bg_2()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowBG2_1;
		}

		public static bool snes_getlayer_bg_3()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowBG3_1;
		}

		public static bool snes_getlayer_bg_4()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowBG4_1;
		}

		public static bool snes_getlayer_obj_1()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowOBJ_0;
		}

		public static bool snes_getlayer_obj_2()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowOBJ_1;
		}

		public static bool snes_getlayer_obj_3()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowOBJ_2;
		}

		public static bool snes_getlayer_obj_4()
		{
			return ((LibsnesCore.SnesSettings)Global.Emulator.GetSettings()).ShowOBJ_3;
		}

		public static void snes_setlayer_bg_1(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowBG1_1 = s.ShowBG1_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void snes_setlayer_bg_2(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowBG2_1 = s.ShowBG2_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void snes_setlayer_bg_3(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowBG3_1 = s.ShowBG3_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void snes_setlayer_bg_4(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowBG4_1 = s.ShowBG4_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void snes_setlayer_obj_1(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowOBJ_0 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void snes_setlayer_obj_2(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowOBJ_1 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void snes_setlayer_obj_3(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
				s.ShowOBJ_2 = value;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void snes_setlayer_obj_4(bool value)
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
