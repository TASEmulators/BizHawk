using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public abstract unsafe class LibNymaCore : LibWaterboxCore
	{
		[StructLayout(LayoutKind.Sequential)]
		public class InitData
		{
			/// <summary>
			/// Filename without extension.  Used in autodetect
			/// </summary>
			public string FileNameBase;
			/// <summary>
			/// Just the extension.  Used in autodetect.  LOWERCASE PLEASE.
			/// </summary>
			public string FileNameExt;
			/// <summary>
			/// Full filename.  This will be fopen()ed by the core
			/// </summary>
			public string FileNameFull;
		}

		/// <summary>
		/// Do this before calling anything, even settings queries
		/// </summary>
		[BizImport(CC)]
		public abstract void PreInit();

		/// <summary>
		/// Set the initial frontend time, this needs to be done before InitRom/InitCd
		/// As init process might query the frontend time
		/// </summary>
		[BizImport(CC)]
		public abstract void SetInitialTime(long initialTime);

		/// <summary>
		/// Load a ROM
		/// </summary>
		[BizImport(CC, Compatibility = true)]
		public abstract bool InitRom([In]InitData data);

		/// <summary>
		/// Load some CDs
		/// </summary>
		[BizImport(CC)]
		public abstract bool InitCd(int numdisks);

		public enum CommandType : int
		{
			NONE = 0x00,
			RESET = 0x01,
			POWER = 0x02,

			INSERT_COIN = 0x07,

			TOGGLE_DIP0 = 0x10,
			TOGGLE_DIP1,
			TOGGLE_DIP2,
			TOGGLE_DIP3,
			TOGGLE_DIP4,
			TOGGLE_DIP5,
			TOGGLE_DIP6,
			TOGGLE_DIP7,
			TOGGLE_DIP8,
			TOGGLE_DIP9,
			TOGGLE_DIP10,
			TOGGLE_DIP11,
			TOGGLE_DIP12,
			TOGGLE_DIP13,
			TOGGLE_DIP14,
			TOGGLE_DIP15,
		}

		[Flags]
		public enum BizhawkFlags : int
		{
			// skip video output
			SkipRendering = 1,
			// skip sound output
			SkipSoundening = 2,
			// render at LCM * LCM instead of raw
			RenderConstantSize = 4,
			// open disk tray, if possible
			OpenTray = 8,
			// close disk tray, if possible
			CloseTray = 16
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public BizhawkFlags Flags;
			/// <summary>
			/// a single command to run at the start of this frame
			/// </summary>
			public CommandType Command;
			/// <summary>
			/// raw data for each input port, assumed to be MAX_PORTS * MAX_PORT_DATA long
			/// </summary>
			public byte* InputPortData;
			/// <summary>
			/// If the core calls time functions, this is the value that will be used
			/// </summary>
			public long FrontendTime;
			/// <summary>
			/// disk index to use if close tray is done
			/// </summary>
			public int DiskIndex;
		}

		/// <summary>
		/// Gets raw layer data to be handled by NymaCore.GetLayerData
		/// </summary>
		[BizImport(CC)]
		public abstract byte* GetLayerData();

		/// <summary>
		/// Set enabled layers (or is it disabled layers?).  Only call if NymaCore.GetLayerData() returned non null
		/// </summary>
		/// <param name="layers">bitmask in order defined by NymaCore.GetLayerData</param>
		[BizImport(CC)]
		public abstract void SetLayers(ulong layers);

		/// <summary>
		/// Gets an input device override for a port
		/// Corresponds to Game->DesiredInput[port].device_name
		/// </summary>
		[BizImport(CC)]
		public abstract IntPtr GetInputDeviceOverride(int port);

		[BizImport(CC)]
		public abstract void DumpInputs();

		/// <summary>
		/// Set what input devices we're going to use
		/// </summary>
		/// <param name="devices">MUST end with a null string</param>
		[BizImport(CC, Compatibility = true)]
		public abstract void SetInputDevices(string[] devices);

		public enum VideoSystem : int
		{
			NONE,
			PAL,
			PAL_M,
			NTSC,
			SECAM
		}

		public enum GameMediumTypes : int
		{
			GMT_NONE = 0,
			GMT_ARCADE,
			GMT_PLAYER
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SystemInfo
		{
			public int MaxWidth;
			public int MaxHeight;
			public int NominalWidth;
			public int NominalHeight;
			public VideoSystem VideoSystem;
			public GameMediumTypes GameType;
			public int FpsFixed;
			public long MasterClock;
			public int LcmWidth;
			public int LcmHeight;
			public int PointerScaleX;
			public int PointerScaleY;
			public int PointerOffsetX;
			public int PointerOffsetY;
		}

		[BizImport(CC, Compatibility = true)]
		public abstract SystemInfo* GetSystemInfo();

		[BizImport(CC)]
		public abstract void DumpSettings();

		/// <summary>
		/// Call when a non-sync setting changes value after emulation started.
		/// The new value should already be available from FrontendSettingQuery
		/// </summary>
		[BizImport(CC)]
		public abstract void NotifySettingChanged(string name);

		public delegate void FrontendSettingQuery(string setting, IntPtr dest);
		[BizImport(CC)]
		public abstract void SetFrontendSettingQuery(FrontendSettingQuery q);

		public delegate void FrontendFirmwareNotify(string name);
		/// <summary>
		/// Set a callback to be called whenever the core calls MDFN_MakeFName for a firmware, so that we can load firmware on demand
		/// </summary>
		[BizImport(CC)]
		public abstract void SetFrontendFirmwareNotify(FrontendFirmwareNotify cb);

		[StructLayout(LayoutKind.Sequential)]
		public class TOC
		{
			public int FirstTrack;
			public int LastTrack;
			public int DiskType;

			[StructLayout(LayoutKind.Sequential)]
			public struct Track
			{
				public int Adr;
				public int Control;
				public int Lba;
				public int Valid;
			}

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 101)]
			public Track[] Tracks;
		}
		/// <summary>
		/// Callback to receive a disk TOC
		/// </summary>
		/// <param name="dest">Deposit a LibNymaCore.TOC here</param>
		[UnmanagedFunctionPointer(CC)]
		public delegate void CDTOCCallback(int disk, IntPtr dest);
		[UnmanagedFunctionPointer(CC)]
		public delegate void CDSectorCallback(int disk, int lba, IntPtr dest);
		[BizImport(CC)]
		public abstract void SetCDCallbacks(CDTOCCallback toccallback, CDSectorCallback sectorcallback);
		[BizImport(CC)]
		public abstract IntPtr GetFrameThreadProc();
	}
}
