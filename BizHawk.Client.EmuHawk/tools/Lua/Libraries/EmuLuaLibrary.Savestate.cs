using System;
using System.Collections.Generic;
using System.IO;

using NLua;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class SavestateLuaLibrary : LuaLibraryBase
	{
		public SavestateLuaLibrary(Lua lua)
			: base(lua) { }

		public SavestateLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "savestate";

		[LuaMethodExample("savestate.load( \"C:\\state.bin\" );")]
		[LuaMethod("load", "Loads a savestate with the given path")]
		public void Load(string path)
		{
			if (!File.Exists(path))
			{
				Log($"could not find file: {path}");
			}
			else
			{
				GlobalWin.MainForm.LoadState(path, Path.GetFileName(path), true);
			}
		}

		[LuaMethodExample("savestate.loadslot( 7 );")]
		[LuaMethod("loadslot", "Loads the savestate at the given slot number (must be an integer between 0 and 9)")]
		public void LoadSlot(int slotNum)
		{
			if (slotNum >= 0 && slotNum <= 9)
			{
				GlobalWin.MainForm.LoadQuickSave("QuickSave" + slotNum, true);
			}
		}

		[LuaMethodExample("savestate.save( \"C:\\state.bin\" );")]
		[LuaMethod("save", "Saves a state at the given path")]
		public void Save(string path)
		{
			GlobalWin.MainForm.SaveState(path, path, true);
		}

		[LuaMethodExample("savestate.saveslot( 7 );")]
		[LuaMethod("saveslot", "Saves a state at the given save slot (must be an integer between 0 and 9)")]
		public void SaveSlot(int slotNum)
		{
			if (slotNum >= 0 && slotNum <= 9)
			{
				GlobalWin.MainForm.SaveQuickSave("QuickSave" + slotNum);
			}
		}
	}
}
