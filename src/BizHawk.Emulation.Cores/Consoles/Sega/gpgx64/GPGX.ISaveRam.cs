using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			var size = 0;
			var area = Core.gpgx_get_sram(ref size);
			if (size == 0 || area == IntPtr.Zero)
			{
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			}

			var ret = new byte[size];
			using (_elf.EnterExit())
			{
				Marshal.Copy(area, ret, 0, size);
			}

			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Length == 0)
			{
				// not sure how this is happening, but reject them
				return;
			}

			var size = 0;
			var area = Core.gpgx_get_sram(ref size);
			if (size == 0 || area == IntPtr.Zero)
			{
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			}

			if (!Core.gpgx_put_sram(data, data.Length))
			{
				throw new Exception("Core rejected saveram");
			}
		}

		public bool SaveRamModified
		{
			get
			{
				var size = 0;
				var area = Core.gpgx_get_sram(ref size);
				return size > 0 && area != IntPtr.Zero;
			}
		}
	}
}
