using System.IO;
using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class SavestateLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "savestate"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"load",
					"loadslot",
					"save",
					"saveslot",
				};
			}
		}

		public void savestate_load(object lua_input)
		{
			if (lua_input is string)
			{
				GlobalWinF.MainForm.LoadStateFile(lua_input.ToString(), Path.GetFileName(lua_input.ToString()), true);
			}
		}

		public void savestate_loadslot(object lua_input)
		{
			int slot = LuaInt(lua_input);

			if (slot >= 0 && slot <= 9)
			{
				GlobalWinF.MainForm.LoadState("QuickSave" + slot.ToString(), true);
			}
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
			int slot = LuaInt(lua_input);

			if (slot >= 0 && slot <= 9)
			{
				GlobalWinF.MainForm.SaveState("QuickSave" + slot.ToString());
			}
		}
	}
}
