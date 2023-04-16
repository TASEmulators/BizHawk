using System;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public sealed class SaveStateApi : ISaveStateApi
	{
		private readonly IMainFormForApi _mainForm;

		private readonly Action<string> LogCallback;

		public SaveStateApi(Action<string> logCallback, IMainFormForApi mainForm)
		{
			LogCallback = logCallback;
			_mainForm = mainForm;
		}

		public void Load(string path, bool suppressOSD)
		{
			if (!File.Exists(path))
			{
				LogCallback($"could not find file: {path}");
				return;
			}

			_mainForm.LoadState(path, Path.GetFileName(path), suppressOSD);
		}

		public void LoadSlot(int slotNum, bool suppressOSD)
		{
			if (0.RangeTo(9).Contains(slotNum)) _mainForm.LoadQuickSave($"QuickSave{slotNum}", suppressOSD);
		}

		public void Save(string path, bool suppressOSD) => _mainForm.SaveState(path, path, true, suppressOSD);

		public void SaveSlot(int slotNum, bool suppressOSD)
		{
			if (0.RangeTo(9).Contains(slotNum)) _mainForm.SaveQuickSave($"QuickSave{slotNum}", true, suppressOSD);
		}
	}
}
