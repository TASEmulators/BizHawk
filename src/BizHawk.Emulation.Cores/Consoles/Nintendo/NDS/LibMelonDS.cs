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
			public long Time;
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
		public struct InitConfig
		{
			public bool SkipFW;
			public bool HasGBACart;
			public bool DSi;
			public bool ClearNAND;
			public bool LoadDSiWare;
			public NDS.NDSSyncSettings.ThreeDeeRendererType ThreeDeeRenderer;
			public RenderSettings RenderSettings;
		}

		public enum ConfigEntry
		{
			ExternalBIOSEnable,

			BIOS9Path,
			BIOS7Path,
			FirmwarePath,

			DSi_BIOS9Path,
			DSi_BIOS7Path,
			DSi_FirmwarePath,
			DSi_NANDPath,

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

			Firm_OverrideSettings,
			Firm_Username,
			Firm_Language,
			Firm_BirthdayMonth,
			Firm_BirthdayDay,
			Firm_Color,
			Firm_Message,
			Firm_MAC,

			WifiSettingsPath,

			AudioBitDepth,

			DSi_FullBIOSBoot,

			// BizHawk-melonDS specific
			UseRealTime,
			FixedBootTime,
			TimeAtBoot,
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
		public delegate IntPtr RequestGLContextCallback();

		[UnmanagedFunctionPointer(CC)]
		public delegate void ReleaseGLContextCallback(IntPtr context);

		[UnmanagedFunctionPointer(CC)]
		public delegate void ActivateGLContextCallback(IntPtr context);

		[UnmanagedFunctionPointer(CC)]
		public delegate IntPtr GetGLProcAddressCallback(string proc);

		[StructLayout(LayoutKind.Sequential)]
		public struct GLCallbackInterface
		{
			public RequestGLContextCallback RequestGLContext;
			public ReleaseGLContextCallback ReleaseGLContext;
			public ActivateGLContextCallback ActivateGLContext;
			public GetGLProcAddressCallback GetGLProcAddress;

			public IntPtr[] AllCallbacksInArray(ICallingConventionAdapter adapter)
			{
				return new Delegate[] { RequestGLContext, ReleaseGLContext, ActivateGLContext, GetGLProcAddress }
					.Select(adapter.GetFunctionPointerForDelegate).ToArray();
			}
		}

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
			/* ref ConfigCallbackInterface */ IntPtr[] configCallbackInterface,
			/* ref FileCallbackInterface */ IntPtr[] fileCallbackInterface,
			// /* ref GLCallbackInterface */ IntPtr[] glCallbackInterface, // TODO
			LogCallback logCallback);

		[BizImport(CC)]
		public abstract void PutSaveRam(byte[] data, uint len);

		[BizImport(CC)]
		public abstract void GetSaveRam(byte[] data);

		[BizImport(CC)]
		public abstract int GetSaveRamLength();

		[BizImport(CC)]
		public abstract bool SaveRamIsDirty();

		[BizImport(CC)]
		public abstract void ImportDSiWareSavs(uint titleId);

		[BizImport(CC)]
		public abstract void ExportDSiWareSavs(uint titleId);

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
	}
}
