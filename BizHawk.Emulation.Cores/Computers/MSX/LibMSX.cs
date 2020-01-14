using System;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

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
	}
}
