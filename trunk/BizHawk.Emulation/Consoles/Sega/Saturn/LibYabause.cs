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

		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool libyabause_init();
	}
}
