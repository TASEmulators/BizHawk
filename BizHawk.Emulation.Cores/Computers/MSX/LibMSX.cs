using System;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	/// <summary>
	/// static bindings into libMSX.dll
	/// </summary>
	public static class LibMSX
	{
		/// <returns>opaque state pointer</returns>
		[DllImport("libMSX.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr MSX_create();

		/// <param name="core">opaque state pointer</param>
		[DllImport("libMSX.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void MSX_destroy(IntPtr core);
	}
}
