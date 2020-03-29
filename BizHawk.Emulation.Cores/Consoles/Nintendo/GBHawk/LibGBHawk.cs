using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	/// <summary>
	/// static bindings into GBHawk.dll
	/// </summary>
	public static class LibGBHawk
	{
		# region Core
		/// <returns>opaque state pointer</returns>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GB_create();

		/// <param name="core">opaque state pointer</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_destroy(IntPtr core);

		/// <summary>
		/// Load BIOS and BASIC image. each must be 16K in size
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="bios">the rom data, can be disposed of once this function returns</param>
		/// <param name="is_GBC">is it GBC console</param>
		/// <param name="GBC_as_GBA">is it in GBA mode</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GB_load_bios(IntPtr core, byte[] bios, bool is_GBC, bool GBC_as_GBA);

		/// <summary>
		/// Load ROM image.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="romdata_1">the rom data, can be disposed of once this function returns</param>
		/// <param name="length_1">length of romdata in bytes</param>
		/// <param name="MD5">Hash used for mapper loading</param>
		/// <param name="RTC_init">Initial RTC time</param>
		/// <param name="RTC_offset">Clck offset for RTC</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GB_load(IntPtr core, byte[] romdata_1, uint length_1, char[] MD5, uint RTC_init, uint RTC_offset);

		/// <summary>
		/// Advance a frame and send controller data.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="ctrl1">controller data for player 1</param>
		/// <param name="ctrl2">controller data for player 2</param>
		/// <param name="render">length of romdata in bytes</param>
		/// <param name="sound">Mapper number to load core with</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool GB_frame_advance(IntPtr core, byte ctrl1, byte ctrl2, byte[] kbrows, bool render, bool sound);

		/// <summary>
		/// Get Video data
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="videobuf">where to send video to</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_get_video(IntPtr core, int[] videobuf);

		/// <summary>
		/// Get Video data
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="aud_buf">where to send left audio to</param>
		/// <param name="n_samp">number of left samples</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint GB_get_audio(IntPtr core, int[] aud_buf,  ref uint n_samp);

		#endregion

		#region State Save / Load

		/// <summary>
		/// Save State
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="saver">save buffer</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_save_state(IntPtr core, byte[] saver);

		/// <summary>
		/// Load State
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="loader">load buffer</param>
		[DllImport("MSXHAWK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_load_state(IntPtr core, byte[] loader);

		#endregion

		#region Memory Domain Functions

		/// <summary>
		/// Read the RAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">ram address</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte GB_getram(IntPtr core, int addr);

		/// <summary>
		/// Read the VRAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">vram address</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte GB_getvram(IntPtr core, int addr);

		/// <summary>
		/// Read the VRAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">vram address</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte GB_getoam(IntPtr core, int addr);

		/// <summary>
		/// Read the VRAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">vram address</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte GB_gethram(IntPtr core, int addr);

		/// <summary>
		/// Read the system bus
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">system bus address</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte GB_getsysbus(IntPtr core, int addr);

		/// <summary>
		/// Read the RAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">ram address</param>
		/// <param name="value">write value</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_setram(IntPtr core, int addr, byte value);

		/// <summary>
		/// Read the VRAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">vram address</param>
		/// <param name="value">write value</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_setvram(IntPtr core, int addr, byte value);

		/// <summary>
		/// Read the VRAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">vram address</param>
		/// <param name="value">write value</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_setoam(IntPtr core, int addr, byte value);

		/// <summary>
		/// Read the VRAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">vram address</param>
		/// <param name="value">write value</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_sethram(IntPtr core, int addr, byte value);

		/// <summary>
		/// Read the system bus
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">system bus address</param>
		/// <param name="value">write value</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_setsysbus(IntPtr core, int addr, byte value);

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
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_settracecallback(IntPtr core, TraceCallback callback);

		/// <summary>
		/// get the trace logger header length
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GB_getheaderlength(IntPtr core);
		
		/// <summary>
		/// get the trace logger disassembly length, a constant
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GB_getdisasmlength(IntPtr core);

		/// <summary>
		/// get the trace logger register string length, a constant
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GB_getregstringlength(IntPtr core);

		/// <summary>
		/// get the trace logger header
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="h">pointer to const char *</param>
		/// <param name="callback">null to clear</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_getheader(IntPtr core, StringBuilder h, int l);

		/// <summary>
		/// get the register state from the cpu
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="r">pointer to const char *</param>
		/// <param name="t">call type</param>
		/// <param name="l">copy length, must be obtained from appropriate get legnth function</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_getregisterstate(IntPtr core, StringBuilder h, int t, int l);

		/// <summary>
		/// get the opcode disassembly
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="d">pointer to const char *</param>
		/// <param name="t">call type</param>
		/// <param name="l">copy length, must be obtained from appropriate get legnth function</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_getdisassembly(IntPtr core, StringBuilder h, int t, int l);
		#endregion

		#region PPU_Viewer

		/// <summary>
		/// type of the cpu trace callback
		/// </summary>
		/// <param name="lcdc">type of event</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ScanlineCallback(byte lcdc);

		/// <summary>
		/// Get PPU Pointers
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="sel">region to get</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GB_get_ppu_pntrs(IntPtr core, int sel);

		/// <summary>
		/// Get PPU Pointers
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte GB_get_LCDC(IntPtr core);

		/// <summary>
		/// set a callback to occur when ly reaches a particular scanline (so at the beginning of the scanline).
		/// when the LCD is active, typically 145 will be the first callback after the beginning of frame advance,
		/// and 144 will be the last callback right before frame advance returns
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="callback">null to clear</param>
		/// <param name="sl">0-153 inclusive</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_setscanlinecallback(IntPtr core, ScanlineCallback callback, int sl);

		#endregion

		#region debuggable funcitons

		/// <summary>
		/// get the current cpu cycle count
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GB_cpu_cycles(IntPtr core);

		/// <summary>
		/// get the registers
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="reg">reg number (see the DLL)</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte GB_cpu_get_regs(IntPtr core, int reg);

		/// <summary>
		/// get the flags
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="flag">flag number (see the DLL)</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool GB_cpu_get_flags(IntPtr core, int flag);

		/// <summary>
		/// get the registers
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="reg">reg number (see the DLL)</param>
		/// <param name="value">value to set</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_cpu_set_regs(IntPtr core, int reg, byte value);

		/// <summary>
		/// get the flags
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="flag">flag number (see the DLL)</param>
		/// <param name="value">value to set</param>
		[DllImport("GBHawk.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void GB_cpu_set_flags(IntPtr core, int flag, bool value);

		#endregion

	}
}
