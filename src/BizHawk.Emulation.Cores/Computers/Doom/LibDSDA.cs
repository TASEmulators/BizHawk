using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Computers.Doom
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
		public abstract void dsda_get_audio(ref int n, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool dsda_init(
			string fileName,
			load_archive_cb feload_archive_cb,
			[In] InitSettings settings);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_frame_advance();

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_video(out int w, out int h, out int pitch, ref IntPtr buffer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void input_cb();

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_set_input_callback(input_cb cb);
	}
}
