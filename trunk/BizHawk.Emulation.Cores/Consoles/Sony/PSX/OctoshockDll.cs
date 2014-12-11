//TODO - make sure msvc builds with 32bit enums and get rid of the extra marshalling fluff here

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

	public enum eShockFramebufferFlags
	{
		None = 0,
		Normalize = 1
	}

	public enum ePeripheralType
	{
		None = 0, //can be used to signify disconnection

		Pad = 1, //SCPH-1080
		DualShock = 2, //SCPH-1200
		DualAnalog = 3, //SCPH-1180

		Multitap = 10,
	};

	public const int SHOCK_OK = 0;
	public const int SHOCK_ERROR = -1;
	public const int SHOCK_NOCANDO = -2;
	public const int SHOCK_INVALID_ADDRESS = -3;

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
	public struct ShockFramebufferInfo
	{
		public int width, height;
		[MarshalAs(UnmanagedType.I4)]
		public eShockFramebufferFlags flags;
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
	
	public static extern int shock_Peripheral_Connect(
		IntPtr psx, 
		int address,
		[MarshalAs(UnmanagedType.I4)] ePeripheralType type
		);
	
	[DllImport("octoshock.dll")]
	public static extern int shock_Peripheral_SetPadInput(IntPtr psx, int address, uint buttons, byte left_x, byte left_y, byte right_x, byte right_y);

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
	public static extern int shock_GetFramebuffer(IntPtr psx, ref ShockFramebufferInfo fb);

	[DllImport("octoshock.dll")]
	public static extern int shock_GetSamples(void* buffer);
}
