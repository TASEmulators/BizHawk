using System.IO;

namespace BizHawk.Client.Common
{
	public sealed class SaveStateApi : ISaveStateApi
	{
		private const string ERR_MSG_NOT_A_SLOT = "saveslots are 1 through 10";

		private const string ERR_MSG_USE_SLOT_10 = "pass 10 for slot 10, not 0";

		private readonly IMainFormForApi _mainForm;

		private readonly Action<string> LogCallback;

		public SaveStateApi(Action<string> logCallback, IMainFormForApi mainForm)
		{
			LogCallback = logCallback;
			_mainForm = mainForm;
		}

		public bool Load(string path, bool suppressOSD)
		{
			if (!File.Exists(path))
			{
				LogCallback($"could not find file: {path}");
				return false;
			}
			return _mainForm.LoadState(path: path, userFriendlyStateName: Path.GetFileName(path), suppressOSD);
		}

		public bool LoadSlot(int slotNum, bool suppressOSD)
		{
			if (slotNum is < 0 or > 10) throw new ArgumentOutOfRangeException(paramName: nameof(slotNum), message: ERR_MSG_NOT_A_SLOT);
			if (slotNum is 0)
			{
				LogCallback(ERR_MSG_USE_SLOT_10);
				slotNum = 10;
			}
			return _mainForm.LoadQuickSave(slotNum, suppressOSD: suppressOSD);
		}

		public void Save(string path, bool suppressOSD) => _mainForm.SaveState(path, path, true, suppressOSD);

		public void SaveSlot(int slotNum, bool suppressOSD)
		{
			if (slotNum is < 0 or > 10) throw new ArgumentOutOfRangeException(paramName: nameof(slotNum), message: ERR_MSG_NOT_A_SLOT);
			if (slotNum is 0)
			{
				LogCallback(ERR_MSG_USE_SLOT_10);
				slotNum = 10;
			}
			_mainForm.SaveQuickSave(slotNum, suppressOSD: suppressOSD, fromLua: true);
		}
	}
}
