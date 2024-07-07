//TODO - make sure msvc builds with 32bit enums and get rid of the extra marshalling fluff here

using System.Runtime.InteropServices;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public static unsafe class OctoshockDll
	{
		private const CallingConvention cc = CallingConvention.Cdecl;
		private const string dd = "octoshock.dll";

		public enum eRegion : int
		{
			JP = 0,
			NA = 1,
			EU = 2,
			NONE = 3 //TODO - whats the difference between unset, and region unknown?
		}

		public enum eVidStandard : int
		{
			NTSC = 0,
			PAL = 1,
		}

		public enum eShockStep
		{
			Frame
		}

		public enum eShockFramebufferFlags
		{
			None = 0,
			Normalize = 1
		}

		public enum eMemType
		{
			MainRAM = 0, //2048K
			BiosROM = 1, //512K
			PIOMem = 2, //64K
			GPURAM = 3, //512K
			SPURAM = 4, //512K
			DCache = 5 //1K
		}

		public enum eShockStateTransaction : int
		{
			BinarySize = 0,
			BinaryLoad = 1,
			BinarySave = 2,
			TextLoad = 3,
			TextSave = 4
		}

		public enum eShockMemcardTransaction
		{
			Connect = 0, //connects it to the addressed port (not supported yet)
			Disconnect = 1, //disconnects it from the addressed port (not supported yet)
			Write = 2, //writes from the frontend to the memcard
			Read = 3, //reads from the memcard to the frontend. Also clears the dirty flag
			CheckDirty = 4, //checks whether the memcard is dirty
		}


		public enum ePeripheralType : int
		{
			None = 0, //can be used to signify disconnection

			Pad = 1, //SCPH-1080
			DualShock = 2, //SCPH-1200
			DualAnalog = 3, //SCPH-1180

			NegCon = 4,

			Multitap = 10,
		}

		/// <summary>
		/// this is implemented as an overall render type instead of a horizontal clip control
		/// in case the Framebuffer render type ever develops any differences in its Y-handling.
		/// At that time, we might need to change the GUI to separate the vertical and horizontal components, or something like that
		/// </summary>
		public enum eShockRenderType : int
		{
			Normal,
			ClipOverscan,
			Framebuffer
		}

		public enum eShockDeinterlaceMode : int
		{
			Weave,
			Bob,
			BobOffset
		}

		[Flags]
		public enum eShockMemCb : int
		{
			None = 0,
			Read = 1,
			Write = 2,
			Execute = 4
		}

		public const int SHOCK_OK = 0;
		public const int SHOCK_FALSE = 0;
		public const int SHOCK_TRUE = 1;
		public const int SHOCK_ERROR = -1;
		public const int SHOCK_NOCANDO = -2;
		public const int SHOCK_INVALID_ADDRESS = -3;
		public const int SHOCK_OVERFLOW = -4;

		[StructLayout(LayoutKind.Sequential)]
		public struct ShockDiscInfo
		{
			public eRegion region;
			public unsafe fixed sbyte id[5]; //SCEI, SCEA, SCEE, etc. with null terminator
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ShockTOCTrack
		{
			public byte adr;
			public byte control;
			public uint lba;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ShockTOC
		{
			public byte first_track;
			public byte last_track;
			public byte disc_type;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ShockFramebufferInfo
		{
			public int width, height;
			[MarshalAs(UnmanagedType.I4)]
			public eShockFramebufferFlags flags;
			public void* ptr;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ShockRenderOptions
		{
			public int scanline_start, scanline_end;
			public eShockRenderType renderType;
			public eShockDeinterlaceMode deinterlaceMode;
			public bool skip;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ShockMemcardTransaction
		{
			[MarshalAs(UnmanagedType.I4)]
			public eShockMemcardTransaction transaction;
			public void* buffer128k;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ShockRegisters_CPU
		{
			public fixed uint GPR[32];
			public uint PC, PC_NEXT;
			public uint IN_BD_SLOT;
			public uint LO, HI;
			public uint SR, CAUSE, EPC;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ShockStateTransaction
		{
			public eShockStateTransaction transaction;
			public void* buffer;
			public int bufferLength;
			public TextStateFPtrs ff;
		}


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ShockCallback_Trace(IntPtr opaque, uint PC, uint inst, string dis);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ShockDisc_ReadTOC(IntPtr opaque, ShockTOC* read_target, ShockTOCTrack* tracks101);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ShockDisc_ReadLBA(IntPtr opaque, int lba, void* dst);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ShockCallback_Mem(uint address, eShockMemCb type, uint size, uint value);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_Util_DisassembleMIPS(uint PC, uint instr, StringBuilder outbuf, int buflen);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_CreateDisc(out IntPtr outDisc, IntPtr Opaque, int lbaCount, ShockDisc_ReadTOC ReadTOC, ShockDisc_ReadLBA ReadLBA2448, bool suppliesDeinterleavedSubcode);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_DestroyDisc(IntPtr disc);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_AnalyzeDisc(IntPtr disc, out ShockDiscInfo info);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_Create(out IntPtr psx, eRegion region, void* firmware512k);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_Destroy(IntPtr psx);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_Peripheral_Connect(
			IntPtr psx,
			int address,
			[MarshalAs(UnmanagedType.I4)] ePeripheralType type
			);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_Peripheral_SetPadInput(IntPtr psx, int address, uint buttons, byte left_x, byte left_y, byte right_x, byte right_y);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_Peripheral_SetNegconInput(IntPtr psx, int address, uint buttons, byte twist, byte analog1, byte analog2, byte analogL);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_Peripheral_MemcardTransact(IntPtr psx, int address, ref ShockMemcardTransaction transaction);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_Peripheral_PollActive(IntPtr psx, int address, bool clear);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_MountEXE(IntPtr psx, void* exebuf, int size, bool ignore_pcsp);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_PowerOn(IntPtr psx);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_SoftReset(IntPtr psx);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_PowerOff(IntPtr psx);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_OpenTray(IntPtr psx);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_SetDisc(IntPtr psx, IntPtr disc);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_PokeDisc(IntPtr psx, IntPtr disc);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_CloseTray(IntPtr psx);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_SetRenderOptions(IntPtr psx, ref ShockRenderOptions opts);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_Step(IntPtr psx, eShockStep step);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_GetFramebuffer(IntPtr psx, ref ShockFramebufferInfo fb);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_GetSamples(IntPtr psx, void* buffer);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_GetMemData(
			IntPtr psx,
			out IntPtr ptr,
			out int size,
			[MarshalAs(UnmanagedType.I4)] eMemType memType
			);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_StateTransaction(IntPtr psx, ref ShockStateTransaction transaction);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_GetRegisters_CPU(IntPtr psx, ref ShockRegisters_CPU buffer);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_SetRegister_CPU(IntPtr psx, int index, uint value);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_SetTraceCallback(IntPtr psx, IntPtr opaque, ShockCallback_Trace callback);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_SetMemCb(IntPtr psx, ShockCallback_Mem cb, eShockMemCb cbMask);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_SetLEC(IntPtr psx, bool enable);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_GetGPUUnlagged(IntPtr psx);
		
		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_PeekMemory(IntPtr psx, uint address, out byte value);
		
		[DllImport(dd, CallingConvention = cc)]
		public static extern int shock_PokeMemory(IntPtr psx, uint address, byte value);
	}
}