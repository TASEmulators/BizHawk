using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public partial class BsnesCore : ISaveRam
	{
		private IntPtr _saveRam;
		private int _saveRamSize;

		public bool SaveRamModified => _saveRamSize != 0;

		public byte[] CloneSaveRam(bool clearDirty)
		{
			if (_saveRamSize == 0)
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");

			byte[] saveRamCopy = new byte[_saveRamSize];
			using (Api.exe.EnterExit())
			{
				if (_isSGB)
				{
					Api.core.snes_sgb_save_battery(saveRamCopy, _saveRamSize);
				}
				else
				{
					Marshal.Copy(_saveRam, saveRamCopy, 0, _saveRamSize);
				}
			}

			return saveRamCopy;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (_saveRamSize == 0)
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");

			if (data.Length != _saveRamSize)
			{
				throw new InvalidOperationException("Size of saveram data does not match expected!");
			}

			using (Api.exe.EnterExit())
			{
				if (_isSGB)
				{
					Api.core.snes_sgb_load_battery(data, _saveRamSize);
				}
				else
				{
					Marshal.Copy(data, 0, _saveRam, _saveRamSize);
				}
			}
		}
	}
}
