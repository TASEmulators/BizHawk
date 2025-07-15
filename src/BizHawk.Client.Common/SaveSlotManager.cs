using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class SaveSlotManager
	{
		private readonly bool[] _slots = new bool[10];
		private readonly bool[] _redo = new bool[10];

		public void Update(IEmulator emulator, IMovie movie, string saveStatePrefix)
		{
			if (!emulator.HasSavestates())
			{
				for (int i = 0; i < 10; i++)
				{
					_slots[i] = false;
				}

				return;
			}

			for (int i = 1; i <= 10; i++)
			{
				if (movie is ITasMovie tasMovie)
				{
					_slots[i - 1] = (i - 1) < tasMovie.Branches.Count;
				}
				else
				{
					var file = new FileInfo($"{saveStatePrefix}.QuickSave{i % 10}.State");
					file.Directory?.Create();
					_slots[i - 1] = file.Exists;
				}
			}
		}

		public bool HasSlot(IEmulator emulator, IMovie movie, int slot, string savestatePrefix)
		{
			if (!emulator.HasSavestates())
			{
				return false;
			}

			if (!0.RangeTo(10).Contains(slot))
			{
				return false;
			}

			Update(emulator, movie, savestatePrefix);
			return _slots[slot - 1];
		}

		public void ClearRedoList()
		{
			for (int i = 0; i < 10; i++)
			{
				_redo[i] = false;
			}
		}

		public void ToggleRedo(IMovie movie, int slot)
		{
			if (slot is >= 1 and <= 10 && movie is not ITasMovie) _redo[slot - 1] = !_redo[slot - 1];
		}

		public bool IsRedo(IMovie movie, int slot)
			=> slot is >= 1 and <= 10 && movie is not ITasMovie && _redo[slot - 1];

		/// <summary>
		/// Takes the .state and .bak files and swaps them
		/// </summary>
		public FileWriteResult SwapBackupSavestate(IMovie movie, string path, int currentSlot)
		{
			string backupPath = $"{path}.bak";
			string tempPath = $"{path}.bak.tmp";

			var state = new FileInfo(path);
			var backup = new FileInfo(backupPath);

			if (!state.Exists || !backup.Exists)
			{
				return new();
			}

			// Delete old temp file if it exists.
			try
			{
				if (File.Exists(tempPath)) File.Delete(tempPath);
			}
			catch (Exception ex)
			{
				return new(FileWriteEnum.FailedToDeleteGeneric, new(tempPath, ""), ex);
			}

			// Move backup to temp.
			try
			{
				backup.MoveTo(tempPath);
			}
			catch (Exception ex)
			{
				return new(FileWriteEnum.FailedToMoveForSwap, new(tempPath, backupPath), ex);
			}
			// Move current to backup.
			try
			{
				state.MoveTo(backupPath);
			}
			catch (Exception ex)
			{
				// Attempt to restore the backup
				try { backup.MoveTo(backupPath); } catch { /* eat? unlikely to fail here */ }
				return new(FileWriteEnum.FailedToMoveForSwap, new(backupPath, path), ex);
			}
			// Move backup to current.
			try
			{
				backup.MoveTo(path);
			}
			catch (Exception ex)
			{
				// Should we attempt to restore? Unlikely to fail here since we've already touched all files.
				return new(FileWriteEnum.FailedToMoveForSwap, new(path, tempPath), ex);
			}

			ToggleRedo(movie, currentSlot);
			return new();
		}
	}
}
