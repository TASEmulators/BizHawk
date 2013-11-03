using System.IO;

namespace BizHawk.Client.Common
{
	public class SaveSlotManager
	{
		private readonly bool[] slots = new bool[10];
		private readonly bool[] redo = new bool[10];

		public SaveSlotManager()
		{
			Update();
		}

		public void Update()
		{
			if (Global.Game == null || Global.Emulator == null)
			{
				for (int i = 0; i < 10; i++)
				{
					slots[i] = false;
				}
				return;
			}
			for (int i = 0; i < 10; i++)
			{
				var file = new FileInfo(
					PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave" + i + ".State"
				);
				if (file.Directory != null && file.Directory.Exists == false)
				{
					file.Directory.Create();
				}
				slots[i] = file.Exists;
			}
		}

		public bool HasSavestateSlots
		{
			get
			{
				Update();
				for (int i = 0; i < 10; i++)
				{
					if (slots[i]) return true;
				}
				return false;
			}
		}

		public bool HasSlot(int slot)
		{
			if (Global.Emulator is NullEmulator)
			{
				return false;
			}

			if (slot < 0 || slot > 10)
			{
				return false;
			}

			Update();
			return slots[slot];
		}

		public void ClearRedoList()
		{
			for (int i = 0; i < 10; i++)
			{
				redo[i] = false;
			}
		}

		public void ToggleRedo(int slot)
		{
			if (slot < 0 || slot > 9)
			{
				return;
			}

			redo[slot] ^= true;
		}

		public bool IsRedo(int slot)
		{
			if (slot < 0 || slot > 9)
			{
				return false;
			}

			return redo[slot];
		}

		public void Clear()
		{
			ClearRedoList();
			Update();
		}
	}
}
