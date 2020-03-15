using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class SaveSlotManager
	{
		private readonly bool[] _slots = new bool[10];
		private readonly bool[] _redo = new bool[10];

		public void Update(string saveStatePrefix)
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
				if (Global.MovieSession.Movie is TasMovie tasMovie)
				{
					_slots[i] = i < tasMovie.Branches.Count;
				}
				else
				{
					var file = new FileInfo($"{saveStatePrefix}.QuickSave{i}.State");
					if (file.Directory != null && file.Directory.Exists == false)
					{
						file.Directory.Create();
					}

					_slots[i] = file.Exists;
				}
			}
		}

		public bool HasSlot(int slot, string savestatePrefix)
		{
			if (!Global.Emulator.HasSavestates())
			{
				return false;
			}

			if (!0.RangeTo(10).Contains(slot))
			{
				return false;
			}

			Update(savestatePrefix);
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
			if (0.RangeTo(9).Contains(slot) && !(Global.MovieSession.Movie is TasMovie)) _redo[slot] ^= true;
		}

		public bool IsRedo(int slot) => 0.RangeTo(9).Contains(slot) && !(Global.MovieSession.Movie is TasMovie) && _redo[slot];

		public void SwapBackupSavestate(string path)
		{
			// Takes the .state and .bak files and swaps them
			var state = new FileInfo(path);
			var backup = new FileInfo($"{path}.bak");
			var temp = new FileInfo($"{path}.bak.tmp");

			if (!state.Exists || !backup.Exists)
			{
				return;
			}

			if (temp.Exists)
			{
				temp.Delete();
			}

			backup.CopyTo($"{path}.bak.tmp");
			backup.Delete();
			state.CopyTo($"{path}.bak");
			state.Delete();
			temp.CopyTo(path);
			temp.Delete();

			ToggleRedo(Global.Config.SaveSlot);
		}
	}
}
