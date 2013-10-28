using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		public bool snes_getlayer_bg_1()
		{
			return Global.Config.SNES_ShowBG1_1;
		}

		public bool snes_getlayer_bg_2()
		{
			return Global.Config.SNES_ShowBG2_1;
		}

		public bool snes_getlayer_bg_3()
		{
			return Global.Config.SNES_ShowBG3_1;
		}

		public bool snes_getlayer_bg_4()
		{
			return Global.Config.SNES_ShowBG4_1;
		}

		public bool snes_getlayer_obj_1()
		{
			return Global.Config.SNES_ShowOBJ1;
		}

		public bool snes_getlayer_obj_2()
		{
			return Global.Config.SNES_ShowOBJ2;
		}

		public bool snes_getlayer_obj_3()
		{
			return Global.Config.SNES_ShowOBJ3;
		}

		public bool snes_getlayer_obj_4()
		{
			return Global.Config.SNES_ShowOBJ4;
		}

		public void snes_setlayer_bg_1(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleBG1(value);
		}

		public void snes_setlayer_bg_2(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleBG2(value);
		}

		public void snes_setlayer_bg_3(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleBG3(value);
		}

		public void snes_setlayer_bg_4(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleBG4(value);
		}

		public void snes_setlayer_obj_1(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleOBJ1(value);
		}

		public void snes_setlayer_obj_2(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleOBJ2(value);
		}

		public void snes_setlayer_obj_3(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleOBJ3(value);
		}

		public void snes_setlayer_obj_4(bool value)
		{
			GlobalWinF.MainForm.SNES_ToggleOBJ4(value);
		}
	}
}
