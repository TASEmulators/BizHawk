using System;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class SaveStateApi : ISaveState
	{
		private readonly Action<string> _logCallback;

		public SaveStateApi(Action<string> logCallback)
		{
			_logCallback = logCallback;
		}

		public SaveStateApi() : this(Console.WriteLine) {}

		public void Load(string path, bool suppressOSD)
		{
			if (!File.Exists(path))
			{
				_logCallback($"could not find file: {path}");
				return;
			}
			GlobalWin.MainForm.LoadState(path, Path.GetFileName(path), true, suppressOSD);
		}

		public void LoadSlot(int slotNum, bool suppressOSD)
		{
			if (0.RangeTo(9).Contains(slotNum)) GlobalWin.MainForm.LoadQuickSave($"QuickSave{slotNum}", true, suppressOSD);
		}

		public void Save(string path, bool suppressOSD) => GlobalWin.MainForm.SaveState(path, path, true, suppressOSD);

		public void SaveSlot(int slotNum, bool suppressOSD)
		{
			if (0.RangeTo(9).Contains(slotNum)) GlobalWin.MainForm.SaveQuickSave($"QuickSave{slotNum}", true, suppressOSD);
		}
	}
}
