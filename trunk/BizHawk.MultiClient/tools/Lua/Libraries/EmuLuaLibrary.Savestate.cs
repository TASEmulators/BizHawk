using System.IO;
using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		public void savestate_load(object lua_input)
		{
			if (lua_input is string)
			{
				GlobalWinF.MainForm.LoadStateFile(lua_input.ToString(), Path.GetFileName(lua_input.ToString()), true);
			}
		}

		public void savestate_loadslot(object lua_input)
		{
			int x;

			try //adelikat:  This crap might not be necessary, need to test for a more elegant solution
			{
				x = int.Parse(lua_input.ToString());
			}
			catch
			{
				return;
			}

			if (x < 0 || x > 9)
				return;

			GlobalWinF.MainForm.LoadState("QuickSave" + x.ToString(), true);
		}

		public string savestate_registerload(LuaFunction luaf, object name)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnSavestateLoad", name != null ? name.ToString() : null);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public string savestate_registersave(LuaFunction luaf, object name)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnSavestateSave", name != null ? name.ToString() : null);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public void savestate_save(object lua_input)
		{
			if (lua_input is string)
			{
				string path = lua_input.ToString();
				GlobalWinF.MainForm.SaveStateFile(path, path, true);
			}
		}

		public void savestate_saveslot(object lua_input)
		{
			int x;

			try //adelikat:  This crap might not be necessary, need to test for a more elegant solution
			{
				x = int.Parse(lua_input.ToString());
			}
			catch
			{
				return;
			}

			if (x < 0 || x > 9)
				return;

			GlobalWinF.MainForm.SaveState("QuickSave" + x.ToString());
		}
	}
}
