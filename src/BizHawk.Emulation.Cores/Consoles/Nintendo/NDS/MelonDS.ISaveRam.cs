using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class NDS : ISaveRam
	{
		public new bool SaveRamModified => _core.SaveRamIsDirty();

		public new byte[] CloneSaveRam()
		{
			int length = _core.GetSaveRamLength();

			if (length > 0)
			{
				byte[] ret = new byte[length];
				_core.GetSaveRam(ret);
				return ret;
			}

			return new byte[0];
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (data.Length > 0)
			{
				_core.PutSaveRam(data, (uint)data.Length);
			}
		}
	}
}
