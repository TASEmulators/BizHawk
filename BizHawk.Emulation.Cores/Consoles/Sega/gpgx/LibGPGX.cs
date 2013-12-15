using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public static class LibGPGX
	{
		[DllImport("libgenplusgx.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gpgx_get_video(ref int w, ref int h, ref int pitch, ref IntPtr buffer);

		[DllImport("libgenplusgx.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gpgx_get_audio(ref int n, ref IntPtr buffer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int load_archive_cb(string filename, IntPtr buffer, int maxsize);

		[DllImport("libgenplusgx.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gpgx_advance();

		[DllImport("libgenplusgx.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool gpgx_init(string feromextension, load_archive_cb feload_archive_cb);

	}
}
