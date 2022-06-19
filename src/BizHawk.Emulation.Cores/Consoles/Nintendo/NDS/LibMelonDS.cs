using System;
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
		}

		[Flags]
		public enum LoadFlags : uint
		{
			NONE = 0x00,
			USE_REAL_BIOS = 0x01,
			SKIP_FIRMWARE = 0x02,
			GBA_CART_PRESENT = 0x04,
			RESERVED_FLAG = 0x08,
			FIRMWARE_OVERRIDE = 0x10,
			IS_DSI = 0x20,
			LOAD_DSIWARE = 0x40,
			THREADED_RENDERING = 0x80,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LoadData
		{
			public IntPtr DsRomData;
			public int DsRomLength;
			public IntPtr GbaRomData;
			public int GbaRomLength;
			public IntPtr GbaRamData;
			public int GbaRamLength;
			public IntPtr NandData;
			public int NandLength;
			public IntPtr TmdData;
			public NDS.NDSSettings.AudioBitrateType AudioBitrate;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct FirmwareSettings
		{
			public IntPtr FirmwareUsername; // max 10 length (then terminator)
			public int FirmwareUsernameLength;
			public NDS.NDSSyncSettings.Language FirmwareLanguage;
			public NDS.NDSSyncSettings.Month FirmwareBirthdayMonth;
			public int FirmwareBirthdayDay;
			public NDS.NDSSyncSettings.Color FirmwareFavouriteColour;
			public IntPtr FirmwareMessage; // max 26 length (then terminator)
			public int FirmwareMessageLength;
		}

		[BizImport(CC)]
		public abstract bool Init(LoadFlags loadFlags, ref LoadData loadData, ref FirmwareSettings fwSettings);

		[BizImport(CC)]
		public abstract void PutSaveRam(byte[] data, uint len);

		[BizImport(CC)]
		public abstract void GetSaveRam(byte[] data);

		[BizImport(CC)]
		public abstract int GetSaveRamLength();

		[BizImport(CC)]
		public abstract bool SaveRamIsDirty();

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

		// bit 0 -> ARM9 or ARM7
		// bit 1 -> ARM or THUMB mode
		public enum CpuTypes : uint
		{
			ARM9,
			ARM7,
			ARM9_THUMB,
			ARM7_THUMB,
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate void TraceCallback(CpuTypes _cpu, IntPtr _regs, uint _opcode);

		[BizImport(CC)]
		public abstract void SetTraceCallback(TraceCallback callback);

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
		public abstract void ResetCaches();
	}
}
