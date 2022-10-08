using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Client.EmuHawk
{
	public abstract class RAInterface
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		public const int BizHawkEmuID = 12; // this is UnknownEmulator for now

		public enum ConsoleID : int
		{
			UnknownConsoleID = 0,
			MegaDrive = 1,
			N64 = 2,
			SNES = 3,
			GB = 4,
			GBA = 5,
			GBC = 6,
			NES = 7,
			PCEngine = 8,
			SegaCD = 9,
			Sega32X = 10,
			MasterSystem = 11,
			PlayStation = 12,
			Lynx = 13,
			NeoGeoPocket = 14,
			GameGear = 15,
			GameCube = 16,
			Jaguar = 17,
			DS = 18,
			WII = 19,
			WIIU = 20,
			PlayStation2 = 21,
			Xbox = 22,
			MagnavoxOdyssey = 23,
			PokemonMini = 24,
			Atari2600 = 25,
			MSDOS = 26,
			Arcade = 27,
			VirtualBoy = 28,
			MSX = 29,
			C64 = 30,
			ZX81 = 31,
			Oric = 32,
			SG1000 = 33,
			VIC20 = 34,
			Amiga = 35,
			AtariST = 36,
			AmstradCPC = 37,
			AppleII = 38,
			Saturn = 39,
			Dreamcast = 40,
			PSP = 41,
			CDi = 42,
			ThreeDO = 43,
			Colecovision = 44,
			Intellivision = 45,
			Vectrex = 46,
			PC8800 = 47,
			PC9800 = 48,
			PCFX = 49,
			Atari5200 = 50,
			Atari7800 = 51,
			X68K = 52,
			WonderSwan = 53,
			CassetteVision = 54,
			SuperCassetteVision = 55,
			NeoGeoCD = 56,
			FairchildChannelF = 57,
			FMTowns = 58,
			ZXSpectrum = 59,
			GameAndWatch = 60,
			NokiaNGage = 61,
			Nintendo3DS = 62,
			Supervision = 63,
			SharpX1 = 64,
			Tic80 = 65,
			ThomsonTO8 = 66,
			PC6000 = 67,
			Pico = 68,
			MegaDuck = 69,
			Zeebo = 70,
			Arduboy = 71,
			WASM4 = 72,
			Arcadia2001 = 73,
			IntertonVC4000 = 74,
			ElektorTVGamesComputer = 75,
			PCEngineCD = 76,

			NumConsoleIDs
		}

		[BizImport(cc, EntryPoint = "_RA_IntegrationVersion")]
		public abstract IntPtr IntegrationVersion();

		[BizImport(cc, EntryPoint = "_RA_HostName")]
		public abstract IntPtr HostName();

		[BizImport(cc, EntryPoint = "_RA_HostUrl")]
		public abstract IntPtr HostUrl();

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_InitI")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool InitI(IntPtr hwnd, int emuID, string clientVer);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_InitOffline")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool InitOffline(IntPtr hwnd, int emuID, string clientVer);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_InitClient")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool InitClient(IntPtr hwnd, int emuID, string clientVer);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_InitClientOffline")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool InitClientOffline(IntPtr hwnd, int emuID, string clientVer);

		[UnmanagedFunctionPointer(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public delegate bool IsActiveDelegate();

		[UnmanagedFunctionPointer(cc)]
		public delegate void UnpauseDelegate();

		[UnmanagedFunctionPointer(cc)]
		public delegate void PauseDelegate();

		[UnmanagedFunctionPointer(cc)]
		public delegate void RebuildMenuDelegate();

		[UnmanagedFunctionPointer(cc)]
		public delegate void EstimateTitleDelegate(IntPtr buffer);

		[UnmanagedFunctionPointer(cc)]
		public delegate void ResetEmulatorDelegate();

		[UnmanagedFunctionPointer(cc)]
		public delegate void LoadROMDelegate(string filename);

		[BizImport(cc, EntryPoint = "_RA_InstallSharedFunctionsExt")]
		public abstract void InstallSharedFunctionsExt(IsActiveDelegate isActive,
			UnpauseDelegate unpause, PauseDelegate pause, RebuildMenuDelegate rebuildMenu,
			EstimateTitleDelegate estimateTitle, ResetEmulatorDelegate resetEmulator, LoadROMDelegate loadROM);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_SetForceRepaint")]
		public abstract void SetForceRepaint([MarshalAs(UnmanagedType.Bool)] bool enable);

		[BizImport(cc, EntryPoint = "_RA_CreatePopupMenu")]
		public abstract IntPtr CreatePopupMenu();

		[StructLayout(LayoutKind.Sequential)]
		public struct MenuItem
		{
			public IntPtr Label;
			public IntPtr ID;
			public int Checked;
		}

		[BizImport(cc, EntryPoint = "_RA_GetPopupMenuItems")]
		public abstract int GetPopupMenuItems(MenuItem[] items);

		[BizImport(cc, EntryPoint = "_RA_InvokeDialog")]
		public abstract void InvokeDialog(IntPtr lparam);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_SetUserAgentDetail")]
		public abstract void SetUserAgentDetail(string detail);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_AttemptLogin")]
		public abstract void AttemptLogin([MarshalAs(UnmanagedType.Bool)] bool blocking);

		[BizImport(cc, EntryPoint = "_RA_SetConsoleID")]
		public abstract void SetConsoleID(ConsoleID consoleID);

		[BizImport(cc, EntryPoint = "_RA_ClearMemoryBanks")]
		public abstract void ClearMemoryBanks();

		[UnmanagedFunctionPointer(cc)]
		public delegate byte ReadMemoryFunc(int address);

		[UnmanagedFunctionPointer(cc)]
		public delegate void WriteMemoryFunc(int address, byte value);

		[BizImport(cc, EntryPoint = "_RA_InstallMemoryBank")]
		public abstract void InstallMemoryBank(int bankID, ReadMemoryFunc reader, WriteMemoryFunc writer, int bankSize);

		[UnmanagedFunctionPointer(cc)]
		public delegate int ReadMemoryBlockFunc(int address, IntPtr buffer, int bytes);

		[BizImport(cc, EntryPoint = "_RA_InstallMemoryBankBlockReader")]
		public abstract void InstallMemoryBankBlockReader(int bankID, ReadMemoryBlockFunc reader);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_Shutdown")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool Shutdown();

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_IsOverlayFullyVisible")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool IsOverlayFullyVisible();

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_SetPaused")]
		public abstract void SetPaused([MarshalAs(UnmanagedType.Bool)] bool isPaused);

		[StructLayout(LayoutKind.Sequential)]
		public struct ControllerInput
		{
			[MarshalAs(UnmanagedType.Bool)]
			public bool UpPressed;
			[MarshalAs(UnmanagedType.Bool)]
			public bool DownPressed;
			[MarshalAs(UnmanagedType.Bool)]
			public bool LeftPressed;
			[MarshalAs(UnmanagedType.Bool)]
			public bool RightPressed;
			[MarshalAs(UnmanagedType.Bool)]
			public bool ConfirmPressed;
			[MarshalAs(UnmanagedType.Bool)]
			public bool CancelPressed;
			[MarshalAs(UnmanagedType.Bool)]
			public bool QuitPressed;
		}

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_NavigateOverlay")]
		public abstract void NavigateOverlay(ref ControllerInput input);

		[BizImport(cc, EntryPoint = "_RA_UpdateHWnd")]
		public abstract void UpdateHWnd(IntPtr hwnd);

		[BizImport(cc, EntryPoint = "_RA_IdentifyRom")]
		public abstract int IdentifyRom(byte[] rom, int romSize);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_IdentifyHash")]
		public abstract int IdentifyHash(string hash);

		[BizImport(cc, EntryPoint = "_RA_ActivateGame")]
		public abstract void ActivateGame(int gameId);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_OnLoadNewRom")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool OnLoadNewRom(byte[] rom, int romSize);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_ConfirmLoadNewRom")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool ConfirmLoadNewRom([MarshalAs(UnmanagedType.Bool)] bool quitting);

		[BizImport(cc, EntryPoint = "_RA_DoAchievementsFrame")]
		public abstract void DoAchievementsFrame();

		[BizImport(cc, EntryPoint = "_RA_SuspendRepaint")]
		public abstract void SuspendRepaint();

		[BizImport(cc, EntryPoint = "_RA_ResumeRepaint")]
		public abstract void ResumeRepaint();

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_UpdateAppTitle")]
		public abstract void UpdateAppTitle(string message);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_UserName")]
		public abstract string UserName();

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_HardcoreModeIsActive")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool HardcoreModeIsActive();

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_WarnDisableHardcore")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool WarnDisableHardcore(string activity);

		[BizImport(cc, EntryPoint = "_RA_OnReset")]
		public abstract void OnReset();

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_OnSaveState")]
		public abstract void OnSaveState(string filename);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_OnLoadState")]
		public abstract void OnLoadState(string filename);

		[BizImport(cc, EntryPoint = "_RA_CaptureState")]
		public abstract int CaptureState(IntPtr buffer, int bufferSize);

		[BizImport(cc, EntryPoint = "_RA_RestoreState")]
		public abstract void RestoreState(IntPtr buffer);
	}
}
