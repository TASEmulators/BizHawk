using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	/// <summary>
	/// static bindings into MSXHawk.dll
	/// </summary>
	public static class LibMSX
	{
		private const string lib = "MSXHawk";
		private const CallingConvention cc = CallingConvention.Cdecl;

		/// <returns>opaque state pointer</returns>
		[DllImport(lib, CallingConvention = cc)]
		public static extern IntPtr MSX_create();

		/// <param name="core">opaque state pointer</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void MSX_destroy(IntPtr core);

		/// <summary>
		/// Load BIOS and BASIC image. each must be 16K in size
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="bios">the rom data, can be disposed of once this function returns</param>
		/// <param name="basic">length of romdata in bytes</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport(lib, CallingConvention = cc)]
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
		[DllImport(lib, CallingConvention = cc)]
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
		[DllImport(lib, CallingConvention = cc)]
		public static extern bool MSX_frame_advance(IntPtr core, byte ctrl1, byte ctrl2, byte[] kbrows, bool render, bool sound);

		/// <summary>
		/// Get Video data
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="videobuf">where to send video to</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void MSX_get_video(IntPtr core, int[] videobuf);

		/// <summary>
		/// Get Video data
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="aud_buf">where to send left audio to</param>
		/// <param name="n_samp">number of left samples</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern uint MSX_get_audio(IntPtr core, int[] aud_buf,  ref uint n_samp);

		/// <summary>
		/// get messages length
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern int MSX_getmessagelength(IntPtr core);

		/// <summary>
		/// get messages from the core
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="h">pointer to const char *</param>
		/// <param name="l">length of message to fetch</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void MSX_getmessage(IntPtr core, StringBuilder h, int l);

		/// <summary>
		/// Save State
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="saver">save buffer</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void MSX_save_state(IntPtr core, byte[] saver);

		/// <summary>
		/// Load State
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="loader">load buffer</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void MSX_load_state(IntPtr core, byte[] loader);

		/// <summary>
		/// Read the system bus
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">system bus address</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern byte MSX_getsysbus(IntPtr core, int addr);

		/// <summary>
		/// Read the VRAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">vram address</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern byte MSX_getvram(IntPtr core, int addr);

		/// <summary>
		/// Read the RAM
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="addr">ram address</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern byte MSX_getram(IntPtr core, int addr);

		/// <summary>
		/// type of the cpu trace callback
		/// </summary>
		/// <param name="t">type of event</param>
		[UnmanagedFunctionPointer(cc)]
		public delegate void TraceCallback(int t);

		/// <summary>
		/// set a callback for trace logging
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="callback">null to clear</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void MSX_settracecallback(IntPtr core, TraceCallback callback);

		/// <summary>
		/// get the trace logger header length
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern int MSX_getheaderlength(IntPtr core);

		/// <summary>
		/// get the trace logger disassembly length, a constant
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern int MSX_getdisasmlength(IntPtr core);

		/// <summary>
		/// get the trace logger register string length, a constant
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern int MSX_getregstringlength(IntPtr core);

		/// <summary>
		/// get the trace logger header
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="h">pointer to const char *</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void MSX_getheader(IntPtr core, StringBuilder h, int l);

		/// <summary>
		/// get the register state from the cpu
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="h">pointer to const char *</param>
		/// <param name="t">call type</param>
		/// <param name="l">copy length, must be obtained from appropriate get legnth function</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void MSX_getregisterstate(IntPtr core, StringBuilder h, int t, int l);

		/// <summary>
		/// get the register state from the cpu
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="h">pointer to const char *</param>
		/// <param name="t">call type</param>
		/// <param name="l">copy length, must be obtained from appropriate get legnth function</param>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void MSX_getdisassembly(IntPtr core, StringBuilder h, int t, int l);
	}
}
