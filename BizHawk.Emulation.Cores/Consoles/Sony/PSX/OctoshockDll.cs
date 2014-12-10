//API TODO
//get rid of the 2048 byte reader

using System;
using System.Runtime.InteropServices;

public unsafe static class OctoshockDll
{
	public enum eRegion : int
	{
		JP = 0,
		NA = 1,
		EU = 2,
		NONE = 3 //TODO - whats the difference between unset, and region unknown?
	}

	public enum eShockStep
	{
		Frame
	};

	public const int SHOCK_OK = 0;
	public const int SHOCK_ERROR = -1;
	public const int SHOCK_NOCANDO = -2;

	[StructLayout(LayoutKind.Sequential)]
	public struct ShockDiscInfo
	{
		public eRegion region;
		public unsafe fixed sbyte id[5]; //SCEI, SCEA, SCEE, etc. with null terminator
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct ShockTOCTrack
	{
		public byte adr;
		public byte control;
		public uint lba;
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct ShockTOC
	{
		public byte first_track;
		public byte last_track;
		public byte disc_type;
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct ShockFramebufferJob
	{
		public int width, height;
		public void* ptr;
	};

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int ShockDisc_ReadTOC(IntPtr opaque, ShockTOC* read_target, ShockTOCTrack* tracks101);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int ShockDisc_ReadLBA(IntPtr opaque, int lba, void* dst);

	[DllImport("octoshock.dll")]
	public static extern int shock_CreateDisc(out IntPtr outDisc, IntPtr Opaque, int lbaCount, ShockDisc_ReadTOC ReadTOC, ShockDisc_ReadLBA ReadLBA2448, bool suppliesDeinterleavedSubcode);

	[DllImport("octoshock.dll")]
	public static extern int shock_DestroyDisc(IntPtr disc);

	[DllImport("octoshock.dll")]
	public static extern int shock_AnalyzeDisc(IntPtr disc, out ShockDiscInfo info);

	[DllImport("octoshock.dll")]
	public static extern int shock_Create(out IntPtr psx, eRegion region, void* firmware512k);

	[DllImport("octoshock.dll")]
	public static extern int shock_Destroy(IntPtr psx);

	[DllImport("octoshock.dll")]
	public static extern int shock_PowerOn(IntPtr psx);

	[DllImport("octoshock.dll")]
	public static extern int shock_PowerOff(IntPtr psx);

	[DllImport("octoshock.dll")]
	public static extern int shock_OpenTray(IntPtr psx);

	[DllImport("octoshock.dll")]
	public static extern int shock_SetDisc(IntPtr psx, IntPtr disc);

	[DllImport("octoshock.dll")]
	public static extern int shock_CloseTray(IntPtr psx);

	[DllImport("octoshock.dll")]
	public static extern int shock_Step(IntPtr psx, eShockStep step);

	[DllImport("octoshock.dll")]
	public static extern int shock_GetFramebuffer(IntPtr psx, ref ShockFramebufferJob fb);
}
