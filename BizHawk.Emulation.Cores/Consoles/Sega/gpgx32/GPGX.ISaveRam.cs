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
			if (disposed)
			{
				if (_disposedSaveRam != null)
				{
					return (byte[])_disposedSaveRam.Clone();
				}

				return new byte[0];
			}
			else
			{
				int size = 0;
				IntPtr area = IntPtr.Zero;
				LibGPGX.gpgx_get_sram(ref area, ref size);
				if (size <= 0 || area == IntPtr.Zero)
					return new byte[0];
				LibGPGX.gpgx_sram_prepread();

				byte[] ret = new byte[size];
				Marshal.Copy(area, ret, 0, size);
				return ret;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			if (disposed)
			{
				throw new ObjectDisposedException(typeof(GPGX).ToString());
			}
			else
			{
				int size = 0;
				IntPtr area = IntPtr.Zero;
				LibGPGX.gpgx_get_sram(ref area, ref size);
				if (size <= 0 || area == IntPtr.Zero)
					return;
				if (size != data.Length)
					throw new Exception("Unexpected saveram size");

				Marshal.Copy(data, 0, area, size);
				LibGPGX.gpgx_sram_commitwrite();
			}
		}

		public bool SaveRamModified
		{
			get
			{
				if (disposed)
				{
					return _disposedSaveRam != null;
				}
				else
				{
					int size = 0;
					IntPtr area = IntPtr.Zero;
					LibGPGX.gpgx_get_sram(ref area, ref size);
					return size > 0 && area != IntPtr.Zero;
				}
			}
		}

		private byte[] _disposedSaveRam = null;
	}
}
