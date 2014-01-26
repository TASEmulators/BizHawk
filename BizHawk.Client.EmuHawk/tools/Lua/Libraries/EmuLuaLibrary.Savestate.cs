using System.IO;
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
					"saveslot"
				};
			}
		}

		[LuaMethodAttributes(
			"load",
			"TODO"
		)]
		public void Load(string path)
		{
			GlobalWin.MainForm.LoadState(path, Path.GetFileName(path), true);
		}

		[LuaMethodAttributes(
			"loadslot",
			"TODO"
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
			"TODO"
		)]
		public void Save(string path)
		{
			GlobalWin.MainForm.SaveState(path, path, true);
		}

		[LuaMethodAttributes(
			"saveslot",
			"TODO"
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
