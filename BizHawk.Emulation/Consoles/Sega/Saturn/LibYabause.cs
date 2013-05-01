using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.Sega.Saturn
{
	public static class LibYabause
	{
		/// <summary>
		/// set video buffer
		/// </summary>
		/// <param name="buff">704x512x32bit, should persist over time</param>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_setvidbuff(IntPtr buff);

		/// <summary>
		/// soft reset, or something like that
		/// </summary>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_softreset();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="w">width of framebuffer</param>
		/// <param name="h">height of framebuffer</param>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_frameadvance(out int w, out int h);

		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_deinit();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="intf">cd interface.  struct need not persist after call, but the function pointers better</param>
		/// <returns></returns>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool libyabause_init(ref CDInterface intf);

		public struct CDInterface
		{
			public int DontTouch;
			public IntPtr DontTouch2;
			/// <summary>
			/// init cd functions
			/// </summary>
			/// <param name="unused"></param>
			/// <returns>0 on success, -1 on failure</returns>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int Init(string unused);
			public Init InitFunc;
			/// <summary>
			/// deinit cd functions
			/// </summary>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate void DeInit();
			public DeInit DeInitFunc;
			/// <summary>
			/// 0 = cd present, spinning
			/// 1 = cd present, not spinning
			/// 2 = no cd
			/// 3 = tray open
			/// </summary>
			/// <returns></returns>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int GetStatus();
			public GetStatus GetStatusFunc;
			/// <summary>
			/// read all TOC entries
			/// </summary>
			/// <param name="dest">place to copy to</param>
			/// <returns>number of bytes written.  should be 408</returns>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int ReadTOC(IntPtr dest);
			public ReadTOC ReadTOCFunc;
			/// <summary>
			/// read a sector, should be 2352 bytes
			/// </summary>
			/// <param name="FAD"></param>
			/// <param name="dest"></param>
			/// <returns></returns>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int ReadSectorFAD(int FAD, IntPtr dest);
			public ReadSectorFAD ReadSectorFADFunc;
			/// <summary>
			/// hint the next sector, for async loading
			/// </summary>
			/// <param name="FAD"></param>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate void ReadAheadFAD(int FAD);
			public ReadAheadFAD ReadAheadFADFunc;
		}
	}
}
