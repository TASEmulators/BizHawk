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
		public void LoadSlot(int slotNum)
		{
			if (slotNum >= 0 && slotNum <= 9)
			{
				GlobalWin.MainForm.LoadQuickSave("QuickSave" + slotNum, true);
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
		public void SaveSlot(int slotNum)
		{
			if (slotNum >= 0 && slotNum <= 9)
			{
				GlobalWin.MainForm.SaveQuickSave("QuickSave" + slotNum);
			}
		}
	}
}
