using System;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			int size = 0;
			IntPtr area = Core.gpgx_get_sram(ref size);
			if (size == 0 || area == IntPtr.Zero)
				return new byte[0];

			byte[] ret = new byte[size];
			using (_elf.EnterExit())
				Marshal.Copy(area, ret, 0, size);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (!Core.gpgx_put_sram(data, data.Length))
				throw new Exception("Core rejected saveram");
		}

		public bool SaveRamModified
		{
			get
			{
				int size = 0;
				IntPtr area = Core.gpgx_get_sram(ref size);
				return size > 0 && area != IntPtr.Zero;
			}
		}
	}
}
