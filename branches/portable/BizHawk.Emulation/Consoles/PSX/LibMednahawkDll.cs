using System;
using System.Runtime.InteropServices;

public unsafe static class LibMednahawkDll
{
	public enum eProp : int
	{
		GetPtr_FramebufferPointer,
		GetPtr_FramebufferPitchPixels,
		GetPtr_FramebufferWidth,
		GetPtr_FramebufferHeight,
		SetPtr_FopenCallback,
		SetPtr_FcloseCallback,
		SetPtr_FopCallback
	}

	public enum FOP: int
	{
		FOP_fread,
		FOP_fwrite,
		FOP_fflush,
		FOP_fseeko,
		FOP_ftello,
		FOP_ferror,
		FOP_clearerr,
		FOP_size
	};

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate IntPtr t_FopenCallback(string fname, string mode);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int t_FcloseCallback(IntPtr fp);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate long t_FopCallback(int op, IntPtr ptr, long a, long b, IntPtr fp);

	[DllImport("libmednahawk.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr dll_GetPropPtr(eProp prop);

	[DllImport("libmednahawk.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void dll_SetPropPtr(eProp prop, IntPtr val);

	[DllImport("libmednahawk.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool dll_Initialize();

	[DllImport("libmednahawk.dll", CallingConvention = CallingConvention.Cdecl)]
	public static extern void psx_FrameAdvance();

	[DllImport("libmednahawk.dll", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool psx_LoadCue(string path);
}
