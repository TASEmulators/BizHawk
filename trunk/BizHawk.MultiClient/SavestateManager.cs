using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.MultiClient
{
	class SavestateManager
	{
		private bool[] slots = new bool[10];

		public SavestateManager()
		{
			Update();
		}

		public void Update()
		{
			string path;
			if (Global.Game == null || Global.Emulator == null)
			{
				for (int x = 0; x < 10; x++)
					slots[x] = false;
				return;
			}
			for (int x = 0; x < 10; x++)
			{
				path = Global.Game.SaveStatePrefix + "." + "QuickSave" + x + ".State";
				var file = new FileInfo(path);
				if (file.Directory.Exists == false)
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
	}
}
