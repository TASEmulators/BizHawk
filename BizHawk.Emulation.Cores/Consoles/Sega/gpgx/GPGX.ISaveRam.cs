using System;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			int size = 0;
			IntPtr area = IntPtr.Zero;
			Core.gpgx_get_sram(ref area, ref size);
			if (size <= 0 || area == IntPtr.Zero)
				return new byte[0];
			Core.gpgx_sram_prepread();

			byte[] ret = new byte[size];
			Marshal.Copy(area, ret, 0, size);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			int size = 0;
			IntPtr area = IntPtr.Zero;
			Core.gpgx_get_sram(ref area, ref size);
			if (size <= 0 || area == IntPtr.Zero)
				return;
			if (size != data.Length)
				throw new Exception("Unexpected saveram size");

			Marshal.Copy(data, 0, area, size);
			Core.gpgx_sram_commitwrite();
		}

		public bool SaveRamModified
		{
			get
			{
				int size = 0;
				IntPtr area = IntPtr.Zero;
				Core.gpgx_get_sram(ref area, ref size);
				return size > 0 && area != IntPtr.Zero;
			}
		}
	}
}
