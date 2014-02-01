using System.IO;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class SavestateLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "savestate"; } }

		[LuaMethodAttributes(
			"load",
			"Loads a savestate with the given path"
		)]
		public void Load(string path)
		{
			GlobalWin.MainForm.LoadState(path, Path.GetFileName(path), true);
		}

		[LuaMethodAttributes(
			"loadslot",
			"Loads the savestate at the given slot number (must be an integer between 0 and 9)"
		)]
		public void LoadSlot(object slotNum)
		{
			var slot = LuaInt(slotNum);

			if (slot >= 0 && slot <= 9)
			{
				GlobalWin.MainForm.LoadQuickSave("QuickSave" + slot, true);
			}
		}

		[LuaMethodAttributes(
			"save",
			"Saves a state at the given path"
		)]
		public void Save(string path)
		{
			GlobalWin.MainForm.SaveState(path, path, true);
		}

		[LuaMethodAttributes(
			"saveslot",
			"Saves a state at the given save slot (must be an integer between 0 and 9)"
		)]
		public void SaveSlot(object slotNum)
		{
			var slot = LuaInt(slotNum);

			if (slot >= 0 && slot <= 9)
			{
				GlobalWin.MainForm.SaveQuickSave("QuickSave" + slot);
			}
		}
	}
}
