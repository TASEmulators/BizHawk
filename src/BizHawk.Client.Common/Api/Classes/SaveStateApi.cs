using System;
using System.IO;

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
			if (slotNum is >= 0 and <= 9) _mainForm.LoadQuickSave(slotNum, suppressOSD: suppressOSD);
		}

		public void Save(string path, bool suppressOSD) => _mainForm.SaveState(path, path, true, suppressOSD);

		public void SaveSlot(int slotNum, bool suppressOSD)
		{
			if (slotNum is >= 0 and <= 9) _mainForm.SaveQuickSave(slotNum, suppressOSD: suppressOSD, fromLua: true);
		}
	}
}
