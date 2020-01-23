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
		# region Core
		/// <returns>opaque state pointer</returns>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr MSX_create();

		/// <param name="core">opaque state pointer</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_destroy(IntPtr core);

		/// <summary>
		/// Load BIOS and BASIC image. each must be 16K in size
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="bios">the rom data, can be disposed of once this function returns</param>
		/// <param name="basic">length of romdata in bytes</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int MSX_load_bios(IntPtr core, byte[] bios, byte[] basic);

		/// <summary>
		/// Load ROM image.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="romdata_1">the rom data, can be disposed of once this function returns</param>
		/// <param name="length_1">length of romdata in bytes</param>
		/// <param name="mapper_1">Mapper number to load core with</param>
		/// <param name="romdata_2">the rom data, can be disposed of once this function returns</param>
		/// <param name="length_2">length of romdata in bytes</param>
		/// <param name="mapper_2">Mapper number to load core with</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int MSX_load(IntPtr core, byte[] romdata_1, uint length_1, int mapper_1, byte[] romdata_2, uint length_2, int mapper_2);

		/// <summary>
		/// Advance a frame and send controller data.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="ctrl1">controller data for player 1</param>
		/// <param name="ctrl2">controller data for player 2</param>
		/// <param name="render">length of romdata in bytes</param>
		/// <param name="sound">Mapper number to load core with</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool MSX_frame_advance(IntPtr core, byte ctrl1, byte ctrl2, bool render, bool sound);

		/// <summary>
		/// Get Video data
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="videobuf">where to send video to</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_get_video(IntPtr core, int[] videobuf);

		/// <summary>
		/// Get Video data
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="aud_buf_L">where to send left audio to</param>
		/// <param name="aud_buf_R">where to send right audio to</param>
		/// <param name="n_samp_L">number of left samples</param>
		/// <param name="n_samp_R">number of right samples</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint MSX_get_audio(IntPtr core, uint[] aud_buf_L, uint[] aud_buf_R, ref uint n_samp_L, ref uint n_samp_R);

		#endregion

		#region State Save / Load

		/// <summary>
		/// Save State
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="saver">save buffer</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_save_state(IntPtr core, byte[] saver);

		/// <summary>
		/// Load State
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="loader">load buffer</param>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_load_state(IntPtr core, byte[] loader);

		#endregion

		#region Memory Domain Functions

		/// <summary>
		/// Read the system bus
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">system bus address</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte MSX_getsysbus(IntPtr core, int addr);

		/// <summary>
		/// Read the VRAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">vram address</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte MSX_getvram(IntPtr core, int addr);


		#endregion

		#region Tracer
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
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_settracecallback(IntPtr core, TraceCallback callback);

		/// <summary>
		/// get the trace logger header length
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int MSX_getheaderlength(IntPtr core);
		
		/// <summary>
		/// get the trace logger disassembly length, a constant
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int MSX_getdisasmlength(IntPtr core);

		/// <summary>
		/// get the trace logger register string length, a constant
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int MSX_getregstringlength(IntPtr core);

		/// <summary>
		/// get the trace logger header
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="h">pointer to const char *</param>
		/// <param name="callback">null to clear</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_getheader(IntPtr core, StringBuilder h, int l);

		/// <summary>
		/// get the register state from the cpu
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="r">pointer to const char *</param>
		/// <param name="t">call type</param>
		/// <param name="l">copy length, must be obtained from appropriate get legnth function</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_getregisterstate(IntPtr core, StringBuilder h, int t, int l);

		/// <summary>
		/// get the register state from the cpu
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="d">pointer to const char *</param>
		/// <param name="t">call type</param>
		/// <param name="l">copy length, must be obtained from appropriate get legnth function</param>
		[DllImport("MSXHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_getdisassembly(IntPtr core, StringBuilder h, int t, int l);
		#endregion
	}
}
