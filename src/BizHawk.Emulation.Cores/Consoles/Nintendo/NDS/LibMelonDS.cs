using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public abstract class LibMelonDS : LibWaterboxCore
	{
		[Flags]
		public enum Buttons : uint
		{
			A = 0x0001,
			B = 0x0002,
			SELECT = 0x0004,
			START = 0x0008,
			RIGHT = 0x0010,
			LEFT = 0x0020,
			UP = 0x0040,
			DOWN = 0x0080,
			R = 0x0100,
			L = 0x0200,
			X = 0x0400,
			Y = 0x0800,
			TOUCH = 0x1000,
			LIDOPEN = 0x2000,
			LIDCLOSE = 0x4000,
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public IntPtr Console;
			public Buttons Keys;
			public byte TouchX;
			public byte TouchY;
			public byte MicVolume;
			public byte GBALightSensor;
			public byte ConsiderAltLag;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ConsoleCreationArgs
		{
			public IntPtr NdsRomData;
			public int NdsRomLength;

			public IntPtr GbaRomData;
			public int GbaRomLength;

			public IntPtr Arm9BiosData;
			public int Arm9BiosLength;

			public IntPtr Arm7BiosData;
			public int Arm7BiosLength;

			public IntPtr FirmwareData;
			public int FirmwareLength;

			public IntPtr Arm9iBiosData;
			public int Arm9iBiosLength;

			public IntPtr Arm7iBiosData;
			public int Arm7iBiosLength;

			public IntPtr NandData;
			public int NandLength;

			public IntPtr DsiWareData;
			public int DsiWareLength;

			public IntPtr TmdData;
			public int TmdLength;

			public bool DSi;
			public bool ClearNAND;
			public bool SkipFW;

			public NDS.NDSSettings.AudioBitDepthType BitDepth;
			public NDS.NDSSettings.AudioInterpolationType Interpolation;

			public NDS.NDSSyncSettings.ThreeDeeRendererType ThreeDeeRenderer;
			public bool Threaded3D;
			public int ScaleFactor;
			public bool BetterPolygons;
			public bool HiResCoordinates;

			public int StartYear;
			public int StartMonth;
			public int StartDay;
			public int StartHour;
			public int StartMinute;
			public int StartSecond;

			public FirmwareSettings FwSettings;
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct FirmwareSettings
		{
			public bool OverrideSettings;
			public int UsernameLength;
			public fixed char Username[10];
			public NDS.NDSSyncSettings.Language Language;
			public NDS.NDSSyncSettings.Month BirthdayMonth;
			public int BirthdayDay;
			public NDS.NDSSyncSettings.Color Color;
			public int MessageLength;
			public fixed char Message[26];
			public fixed byte MacAddress[6];
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate IntPtr GetGLProcAddressCallback(string proc);

		public enum LogLevel : int
		{
			Debug,
			Info,
			Warn,
			Error,
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate void LogCallback(LogLevel level, string message);

		[BizImport(CC)]
		public abstract void SetLogCallback(LogCallback logCallback);

		[BizImport(CC)]
		public abstract IntPtr InitGL(GetGLProcAddressCallback getGLProcAddressCallback,
			NDS.NDSSyncSettings.ThreeDeeRendererType threeDeeRenderer, int scaleFactor, bool isWinApi);

		[BizImport(CC)]
		public abstract IntPtr CreateConsole(ref ConsoleCreationArgs args, byte[] error);

		[BizImport(CC)]
		public abstract void ResetConsole(IntPtr console, bool skipFw, ulong dsiTitleId);

		[BizImport(CC)]
		public abstract void PutSaveRam(IntPtr console, byte[] data, uint len);

		[BizImport(CC)]
		public abstract void GetSaveRam(IntPtr console, byte[] data);

		[BizImport(CC)]
		public abstract int GetSaveRamLength(IntPtr console);

		[BizImport(CC)]
		public abstract bool SaveRamIsDirty();

		[BizImport(CC)]
		public abstract void ImportDSiWareSavs(IntPtr console, uint titleId);

		[BizImport(CC)]
		public abstract void ExportDSiWareSavs(IntPtr console, uint titleId);

		[BizImport(CC)]
		public abstract void DSiWareSavsLength(IntPtr console, uint titleId, out int publicSavSize, out int privateSavSize, out int bannerSavSize);

		[BizImport(CC)]
		public abstract void GetRegs(IntPtr console, uint[] regs);

		[BizImport(CC)]
		public abstract void SetReg(IntPtr console, int ncpu, int index, int val);

		[BizImport(CC)]
		public abstract int GetCallbackCycleOffset(IntPtr console);

		[UnmanagedFunctionPointer(CC)]
		public delegate void MemoryCallback(uint addr);

		[BizImport(CC)]
		public abstract void SetMemoryCallback(int which, MemoryCallback callback);

		[Flags]
		public enum TraceMask : uint
		{
			NONE = 0,
			ARM7_THUMB = 1,
			ARM7_ARM = 2,
			ARM9_THUMB = 4,
			ARM9_ARM = 8,
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate void TraceCallback(TraceMask type, uint opcode, IntPtr regs, IntPtr disasm, uint cyclesOff);

		[BizImport(CC)]
		public abstract void SetTraceCallback(TraceCallback callback, TraceMask mask);

		[BizImport(CC)]
		public abstract void GetDisassembly(TraceMask type, uint opcode, byte[] ret);

		[BizImport(CC)]
		public abstract IntPtr GetFrameThreadProc();

		[UnmanagedFunctionPointer(CC)]
		public delegate void ThreadStartCallback();

		[BizImport(CC)]
		public abstract void SetThreadStartCallback(ThreadStartCallback callback);

		[BizImport(CC)]
		public abstract int GetNANDSize(IntPtr console);

		[BizImport(CC)]
		public abstract void GetNANDData(IntPtr console, byte[] buf);

		[BizImport(CC)]
		public abstract int GetGLTexture();

		[BizImport(CC)]
		public abstract void ReadFrameBuffer(int[] buffer);

		public enum ScreenLayout : int
		{
			Natural,
			Vertical,
			Horizontal,
			Hybrid,
		}

		public enum ScreenRotation : int
		{
			Deg0,
			Deg90,
			Deg180,
			Deg270,
		}

		public enum ScreenSizing : int
		{
			Even = 0,
			TopOnly = 4,
			BotOnly = 5,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ScreenSettings
		{
			public ScreenLayout ScreenLayout;
			public ScreenRotation ScreenRotation;
			public ScreenSizing ScreenSizing;
			public int ScreenGap;
			public bool ScreenSwap;
		}

		[BizImport(CC)]
		public abstract void SetScreenSettings(IntPtr console, ref ScreenSettings screenSettings, out int width, out int height, out int vwidth, out int vheight);

		[BizImport(CC)]
		public abstract void SetSoundConfig(IntPtr console, NDS.NDSSettings.AudioBitDepthType bitDepth, NDS.NDSSettings.AudioInterpolationType interpolation);

		[BizImport(CC)]
		public abstract void GetTouchCoords(ref int x, ref int y);

		[BizImport(CC)]
		public abstract void GetScreenCoords(ref float x, ref float y);
	}
}
