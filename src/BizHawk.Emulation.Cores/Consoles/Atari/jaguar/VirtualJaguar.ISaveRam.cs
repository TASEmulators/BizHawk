using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public partial class VirtualJaguar : ISaveRam
	{
		public new bool SaveRamModified => _core.SaveRamIsDirty();

		public new byte[] CloneSaveRam()
		{
			byte[] ret = new byte[128];
			_core.GetSaveRam(ret);
			return ret;
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (data.Length != 128)
			{
				throw new ArgumentException(message: "buffer wrong size", paramName: nameof(data));
			}

			_core.PutSaveRam(data);
		}
	}
}
