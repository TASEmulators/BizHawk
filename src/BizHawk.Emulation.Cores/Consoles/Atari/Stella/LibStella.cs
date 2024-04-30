using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Atari.Stella
{
	public abstract class CInterface
	{

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int load_archive_cb(string filename, IntPtr buffer, int maxsize);

		[StructLayout(LayoutKind.Sequential)]
		public class InitSettings
		{
			public uint dummy;
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool stella_init(
			load_archive_cb feload_archive_cb,
			[In]InitSettings settings);

	}
}
