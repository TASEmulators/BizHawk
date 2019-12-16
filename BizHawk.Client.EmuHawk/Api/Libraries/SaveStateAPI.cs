using System;
using System.IO;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class SaveStateApi : ISaveState
	{
		public SaveStateApi(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public SaveStateApi() : this(Console.WriteLine) {}

		private readonly Action<string> LogCallback;

		public void Load(string path, bool suppressOSD)
		{
			if (!File.Exists(path))
			{
				LogCallback($"could not find file: {path}");
				return;
			}
			GlobalWin.MainForm.LoadState(path, Path.GetFileName(path), true, suppressOSD);
		}

		public void LoadSlot(int slotNum, bool suppressOSD)
		{
			if (0 <= slotNum && slotNum <= 9) GlobalWin.MainForm.LoadQuickSave($"QuickSave{slotNum}", true, suppressOSD);
		}

		public void Save(string path, bool suppressOSD) => GlobalWin.MainForm.SaveState(path, path, true, suppressOSD);

		public void SaveSlot(int slotNum, bool suppressOSD)
		{
			if (0 <= slotNum && slotNum <= 9) GlobalWin.MainForm.SaveQuickSave($"QuickSave{slotNum}", true, suppressOSD);
		}
	}
}
