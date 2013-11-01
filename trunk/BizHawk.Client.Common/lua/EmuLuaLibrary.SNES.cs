using BizHawk.Emulation.Consoles.Nintendo.SNES;

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
					"setlayer_obj_4",
				};
			}
		}

		public static bool snes_getlayer_bg_1()
		{
			return Global.Config.SNES_ShowBG1_1;
		}

		public static bool snes_getlayer_bg_2()
		{
			return Global.Config.SNES_ShowBG2_1;
		}

		public static bool snes_getlayer_bg_3()
		{
			return Global.Config.SNES_ShowBG3_1;
		}

		public static bool snes_getlayer_bg_4()
		{
			return Global.Config.SNES_ShowBG4_1;
		}

		public static bool snes_getlayer_obj_1()
		{
			return Global.Config.SNES_ShowOBJ1;
		}

		public static bool snes_getlayer_obj_2()
		{
			return Global.Config.SNES_ShowOBJ2;
		}

		public static bool snes_getlayer_obj_3()
		{
			return Global.Config.SNES_ShowOBJ3;
		}

		public static bool snes_getlayer_obj_4()
		{
			return Global.Config.SNES_ShowOBJ4;
		}

		public static void snes_setlayer_bg_1(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				Global.Config.SNES_ShowBG1_1 = Global.Config.SNES_ShowBG1_0 = value;
				CoreFileProvider.SyncCoreCommInputSignals();
			}
		}

		public static void snes_setlayer_bg_2(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				Global.Config.SNES_ShowBG2_1 = Global.Config.SNES_ShowBG2_0 = value;
				CoreFileProvider.SyncCoreCommInputSignals();
			}
		}

		public static void snes_setlayer_bg_3(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				Global.Config.SNES_ShowBG3_1 = Global.Config.SNES_ShowBG3_0 = value;
				CoreFileProvider.SyncCoreCommInputSignals();
			}
		}

		public static void snes_setlayer_bg_4(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				Global.Config.SNES_ShowBG4_1 = Global.Config.SNES_ShowBG4_0 = value;
				CoreFileProvider.SyncCoreCommInputSignals();
			}
		}

		public static void snes_setlayer_obj_1(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				Global.Config.SNES_ShowOBJ1 = value;
				CoreFileProvider.SyncCoreCommInputSignals();
			}
		}

		public static void snes_setlayer_obj_2(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				Global.Config.SNES_ShowOBJ2 = value;
				CoreFileProvider.SyncCoreCommInputSignals();
			}
		}

		public static void snes_setlayer_obj_3(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				Global.Config.SNES_ShowOBJ3 = value;
				CoreFileProvider.SyncCoreCommInputSignals();
			}
		}

		public static void snes_setlayer_obj_4(bool value)
		{
			if (Global.Emulator is LibsnesCore)
			{
				Global.Config.SNES_ShowOBJ4 = value;
				CoreFileProvider.SyncCoreCommInputSignals();
			}
		}
	}
}
