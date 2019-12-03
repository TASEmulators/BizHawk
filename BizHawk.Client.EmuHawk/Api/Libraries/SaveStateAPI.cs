using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class SaveStateApi : ISaveState
	{
		public SaveStateApi() : base()
		{ }

		public void Load(string path)
		{
			if (!File.Exists(path))
			{
				Console.WriteLine($"could not find file: {path}");
			}
			else
			{
				GlobalWin.MainForm.LoadState(path, Path.GetFileName(path), true);
			}
		}

		public void LoadSlot(int slotNum)
		{
			if (slotNum >= 0 && slotNum <= 9)
			{
				GlobalWin.MainForm.LoadQuickSave($"QuickSave{slotNum}", true);
			}
		}

		public void Save(string path)
		{
			GlobalWin.MainForm.SaveState(path, path, true);
		}

		public void SaveSlot(int slotNum)
		{
			if (slotNum >= 0 && slotNum <= 9)
			{
				GlobalWin.MainForm.SaveQuickSave($"QuickSave{slotNum}");
			}
		}
	}
}
