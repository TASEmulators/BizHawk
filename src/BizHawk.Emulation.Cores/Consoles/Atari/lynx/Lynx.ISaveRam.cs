using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	public partial class Lynx : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			if (!LibLynx.GetSaveRamPtr(Core, out var size, out var data))
			{
				return null;
			}

			byte[] ret = new byte[size];
			Marshal.Copy(data, ret, 0, size);
			return ret;
		}

		public void StoreSaveRam(byte[] srcData)
		{
			if (!LibLynx.GetSaveRamPtr(Core, out var size, out var data))
			{
				throw new InvalidOperationException();
			}

			if (srcData.Length != size) throw new ArgumentException(message: "buffer too small", paramName: nameof(srcData));

			Marshal.Copy(srcData, 0, data, size);
		}

		public bool SaveRamModified => LibLynx.GetSaveRamPtr(Core, out int unused, out IntPtr unused2);
	}
}
