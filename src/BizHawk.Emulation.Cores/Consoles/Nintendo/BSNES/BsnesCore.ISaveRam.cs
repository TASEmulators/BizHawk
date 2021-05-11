using System;
using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public unsafe partial class BsnesCore : ISaveRam
	{
		private byte* _saveRam;
		private int _saveRamSize;

		// yeah this is not the best. this will basically always return true as long as the saveRam exists.
		public bool SaveRamModified => _saveRamSize != 0;

		public byte[] CloneSaveRam()
		{
			if (_saveRamSize == 0) return null;

			byte[] saveRamCopy = new byte[_saveRamSize];
			using (Api.exe.EnterExit())
			{
				Marshal.Copy((IntPtr) _saveRam, saveRamCopy, 0, _saveRamSize);
			}

			return saveRamCopy;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (_saveRamSize == 0) return;

			using (Api.exe.EnterExit())
			{
				Marshal.Copy(data, 0, (IntPtr) _saveRam, _saveRamSize);
			}
		}
	}
}
