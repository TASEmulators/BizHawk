using System.IO;

namespace BizHawk.MultiClient
{
	class SavestateManager
	{
		private readonly bool[] slots = new bool[10];
		private readonly bool[] redo = new bool[10];

		public SavestateManager()
		{
			Update();
		}

		public void Update()
		{
			if (Global.Game == null || Global.Emulator == null)
			{
				for (int x = 0; x < 10; x++)
					slots[x] = false;
				return;
			}
			for (int x = 0; x < 10; x++)
			{
				string path = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave" + x + ".State";
				var file = new FileInfo(path);
				if (file.Directory != null && file.Directory.Exists == false)
					file.Directory.Create();
				slots[x] = file.Exists;
			}
		}

		public bool HasSavestateSlots()
		{
			Update();
			for (int x = 0; x < 10; x++)
			{
				if (slots[x]) return true;
			}
			return false;
		}

		public bool HasSlot(int slot)
		{
			if (slot < 0 || slot > 10) return false;

			Update();
			return slots[slot];
		}

		public void ClearRedoList()
		{
			for (int x = 0; x < 10; x++)
			{
				redo[x] = false;
			}
		}

		public void ToggleRedo(int slot)
		{
			if (slot < 0 || slot > 9)
				return;

			redo[slot] ^= true;
		}

		public bool IsRedo(int slot)
		{
			if (slot < 0 || slot > 9)
				return false;

			return redo[slot];
		}

		public void Clear()
		{
			ClearRedoList();
			Update();
		}
	}
}
