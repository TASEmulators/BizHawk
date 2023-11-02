using System;
using System.Linq;
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
			POWER = 0x8000,
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public Buttons Keys;
			public byte TouchX;
			public byte TouchY;
			public byte MicVolume;
			public byte GBALightSensor;
			public bool ConsiderAltLag;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RenderSettings
		{
			public bool SoftThreaded;
			public int GLScaleFactor;
			public bool GLBetterPolygons;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NDSTime
		{
			public int Year;
			public int Month;
			public int Day;
			public int Hour;
			public int Minute;
			public int Second;
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
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InitConfig
		{
			public bool SkipFW;
			public bool HasGBACart;
			public bool DSi;
			public bool ClearNAND;
			public bool LoadDSiWare;
			public bool IsWinApi;
			public NDS.NDSSyncSettings.ThreeDeeRendererType ThreeDeeRenderer;
			public RenderSettings RenderSettings;
			public NDSTime StartTime;
			public FirmwareSettings FirmwareSettings;
		}

		public enum ConfigEntry
		{
			// JIT_ENABLED define would add 5 entries here
			// it is currently not (and unlikely ever to be) defined

			ExternalBIOSEnable,

			DLDI_Enable,
			DLDI_ImagePath,
			DLDI_ImageSize,
			DLDI_ReadOnly,
			DLDI_FolderSync,
			DLDI_FolderPath,

			DSiSD_Enable,
			DSiSD_ImagePath,
			DSiSD_ImageSize,
			DSiSD_ReadOnly,
			DSiSD_FolderSync,
			DSiSD_FolderPath,

			Firm_MAC,

			WifiSettingsPath,

			AudioBitDepth,

			DSi_FullBIOSBoot,

			// GDBSTUB_ENABLED define would add 5 entries here
			// it will not be defined for our purposes
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate bool GetBooleanSettingCallback(ConfigEntry configEntry);

		[UnmanagedFunctionPointer(CC)]
		public delegate int GetIntegerSettingCallback(ConfigEntry configEntry);

		[UnmanagedFunctionPointer(CC)]
		public delegate void GetStringSettingCallback(ConfigEntry configEntry, IntPtr buffer, int bufferSize);

		[UnmanagedFunctionPointer(CC)]
		public delegate void GetArraySettingCallback(ConfigEntry configEntry, IntPtr buffer);

		[StructLayout(LayoutKind.Sequential)]
		public struct ConfigCallbackInterface
		{
			public GetBooleanSettingCallback GetBoolean;
			public GetIntegerSettingCallback GetInteger;
			public GetStringSettingCallback GetString;
			public GetArraySettingCallback GetArray;

			public IntPtr[] AllCallbacksInArray(ICallingConventionAdapter adapter)
			{
				return new Delegate[] { GetBoolean, GetInteger, GetString, GetArray }
					.Select(adapter.GetFunctionPointerForDelegate).ToArray();
			}
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate int GetFileLengthCallback(string path);

		[UnmanagedFunctionPointer(CC)]
		public delegate void GetFileDataCallback(string path, IntPtr buffer);

		[StructLayout(LayoutKind.Sequential)]
		public struct FileCallbackInterface
		{
			public GetFileLengthCallback GetLength;
			public GetFileDataCallback GetData;

			public IntPtr[] AllCallbacksInArray(ICallingConventionAdapter adapter)
			{
				return new Delegate[] { GetLength, GetData }
					.Select(adapter.GetFunctionPointerForDelegate).ToArray();
			}
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
		public abstract IntPtr Init(
			ref InitConfig loadData,
			IntPtr[] configCallbackInterface, /* ref ConfigCallbackInterface */
			IntPtr[] fileCallbackInterface, /* ref FileCallbackInterface */
			LogCallback logCallback,
			GetGLProcAddressCallback getGLProcAddressCallback);

		[BizImport(CC)]
		public abstract void PutSaveRam(byte[] data, uint len);

		[BizImport(CC)]
		public abstract void GetSaveRam(byte[] data);

		[BizImport(CC)]
		public abstract int GetSaveRamLength();

		[BizImport(CC)]
		public abstract bool SaveRamIsDirty();

		[BizImport(CC)]
		public abstract void ImportDSiWareSavs(uint titleId, byte[] data);

		[BizImport(CC)]
		public abstract void ExportDSiWareSavs(uint titleId, byte[] data);

		[BizImport(CC)]
		public abstract void DSiWareSavsLength(uint titleId, out int publicSavSize, out int privateSavSize, out int bannerSavSize);

		[BizImport(CC)]
		public abstract void GetRegs(uint[] regs);

		[BizImport(CC)]
		public abstract void SetReg(int ncpu, int index, int val);

		[BizImport(CC)]
		public abstract int GetCallbackCycleOffset();

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
		public abstract int GetNANDSize();

		[BizImport(CC)]
		public abstract void GetNANDData(byte[] buf);

		[BizImport(CC)]
		public abstract int GetGLTexture();

		[BizImport(CC)]
		public abstract void ReadFrameBuffer(int[] buffer);

		public enum ScreenLayout : int
		{
			Natural,
			Vertical,
			Horizontal,
			// TODO? do we want this?
			// Hybrid,
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
		public abstract void SetScreenSettings(ref ScreenSettings screenSettings, out int width, out int height, out int vwidth, out int vheight);

		[BizImport(CC)]
		public abstract void GetTouchCoords(ref int x, ref int y);

		[BizImport(CC)]
		public abstract void GetScreenCoords(ref float x, ref float y);
	}
}
