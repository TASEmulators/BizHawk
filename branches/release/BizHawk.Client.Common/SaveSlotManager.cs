using System.IO;
using System.Linq;

using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public class SaveSlotManager
	{
		private readonly bool[] _slots = new bool[10];
		private readonly bool[] _redo = new bool[10];

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
					_slots[i] = false;
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

				_slots[i] = file.Exists;
			}
		}

		public bool HasSavestateSlots
		{
			get
			{
				if (!Global.Emulator.HasSavestates())
				{
					return false;
				}

				Update();
				return _slots.Any(slot => slot);
			}
		}

		public bool HasSlot(int slot)
		{
			if (!Global.Emulator.HasSavestates())
			{
				return false;
			}

			if (slot < 0 || slot > 10)
			{
				return false;
			}

			Update();
			return _slots[slot];
		}

		public void ClearRedoList()
		{
			for (int i = 0; i < 10; i++)
			{
				_redo[i] = false;
			}
		}

		public void ToggleRedo(int slot)
		{
			if (slot < 0 || slot > 9)
			{
				return;
			}

			_redo[slot] ^= true;
		}

		public bool IsRedo(int slot)
		{
			if (slot < 0 || slot > 9)
			{
				return false;
			}

			return _redo[slot];
		}

		public void Clear()
		{
			ClearRedoList();
			Update();
		}

		public void SwapBackupSavestate(string path)
		{
			// Takes the .state and .bak files and swaps them
			var state = new FileInfo(path);
			var backup = new FileInfo(path + ".bak");
			var temp = new FileInfo(path + ".bak.tmp");

			if (!state.Exists || !backup.Exists)
			{
				return;
			}

			if (temp.Exists)
			{
				temp.Delete();
			}

			backup.CopyTo(path + ".bak.tmp");
			backup.Delete();
			state.CopyTo(path + ".bak");
			state.Delete();
			temp.CopyTo(path);
			temp.Delete();

			ToggleRedo(Global.Config.SaveSlot);
		}
	}
}
