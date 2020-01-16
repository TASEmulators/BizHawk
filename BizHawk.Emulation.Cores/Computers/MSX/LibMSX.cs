using System;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	/// <summary>
	/// static bindings into MSXHAWK.dll
	/// </summary>
	public static class LibMSX
	{
		/// <returns>opaque state pointer</returns>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr MSX_create();

		/// <param name="core">opaque state pointer</param>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_destroy(IntPtr core);

		/// <summary>
		/// Load ROM image.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="romdata">the rom data, can be disposed of once this function returns</param>
		/// <param name="length">length of romdata in bytes</param>
		/// <param name="mapper">Mapper number to load core with</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int MSX_load(IntPtr core, byte[] romdata, uint length, int mapper);

		/// <summary>
		/// Advance a frame and send controller data.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="ctrl1">controller data for player 1</param>
		/// <param name="ctrl2">controller data for player 2</param>
		/// <param name="render">length of romdata in bytes</param>
		/// <param name="sound">Mapper number to load core with</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool MSX_frame_advance(IntPtr core, byte ctrl1, byte ctrl2, bool render, bool sound);

		/// <summary>
		/// Get Video data
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="ctrl1">controller data for player 1</param>
		/// <param name="ctrl2">controller data for player 2</param>
		/// <param name="render">length of romdata in bytes</param>
		/// <param name="sound">Mapper number to load core with</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_get_video(IntPtr core, int[] videobuf);

		/// <summary>
		/// type of the cpu trace callback
		/// </summary>
		/// <param name="t">type of event</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void TraceCallback(int t);

		/// <summary>
		/// set a callback for trace logging
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="callback">null to clear</param>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_settracecallback(IntPtr core, TraceCallback callback);

		/// <summary>
		/// get the trace logger header
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="h">pointer to const char *</param>
		/// <param name="callback">null to clear</param>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_getheader(IntPtr core, StringBuilder h);

		/// <summary>
		/// get the trace logger header length
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="h">pointer to const char *</param>
		/// <param name="callback">null to clear</param>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int MSX_getheaderlength(IntPtr core);

		/// <summary>
		/// get the register state from the cpu
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="r">pointer to const char *</param>
		/// <param name="t">call type</param>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_getregisterstate(IntPtr core, StringBuilder h, int t);

		/// <summary>
		/// get the register state from the cpu
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="d">pointer to const char *</param>
		/// <param name="t">call type</param>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_getdisassembly(IntPtr core, StringBuilder h, int t);
	}
}
