using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

// this is largely a C# mirror of https://github.com/RetroAchievements/RAInterface

namespace BizHawk.Client.EmuHawk
{
	public abstract class RAInterface
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		[BizImport(cc, EntryPoint = "_RA_IntegrationVersion")]
		public abstract IntPtr IntegrationVersion();

		[BizImport(cc, EntryPoint = "_RA_HostName")]
		public abstract IntPtr HostName();

		[BizImport(cc, EntryPoint = "_RA_HostUrl")]
		public abstract IntPtr HostUrl();

		[BizImport(cc, EntryPoint = "_RA_InitI")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool InitI(IntPtr hwnd, int emuID, string clientVer);

		[BizImport(cc, EntryPoint = "_RA_InitOffline")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool InitOffline(IntPtr hwnd, int emuID, string clientVer);

		[BizImport(cc, EntryPoint = "_RA_InitClient")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool InitClient(IntPtr hwnd, string clientName, string clientVer);

		[BizImport(cc, EntryPoint = "_RA_InitClientOffline")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool InitClientOffline(IntPtr hwnd, string clientName, string clientVer);

		[UnmanagedFunctionPointer(cc)]
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

		[BizImport(cc, EntryPoint = "_RA_SetUserAgentDetail")]
		public abstract void SetUserAgentDetail(string detail);

		[BizImport(cc, Compatibility = true, EntryPoint = "_RA_AttemptLogin")]
		public abstract void AttemptLogin([MarshalAs(UnmanagedType.Bool)] bool blocking);

		[BizImport(cc, EntryPoint = "_RA_SetConsoleID")]
		public abstract void SetConsoleID(RetroAchievements.ConsoleID consoleID);

		[BizImport(cc, EntryPoint = "_RA_ClearMemoryBanks")]
		public abstract void ClearMemoryBanks();

		[BizImport(cc, EntryPoint = "_RA_InstallMemoryBank")]
		public abstract void InstallMemoryBank(int bankID, RetroAchievements.ReadMemoryFunc reader, RetroAchievements.WriteMemoryFunc writer, int bankSize);

		[BizImport(cc, EntryPoint = "_RA_InstallMemoryBankBlockReader")]
		public abstract void InstallMemoryBankBlockReader(int bankID, RetroAchievements.ReadMemoryBlockFunc reader);

		[BizImport(cc, EntryPoint = "_RA_Shutdown")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool Shutdown();

		[BizImport(cc, EntryPoint = "_RA_IsOverlayFullyVisible")]
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
		public abstract uint IdentifyRom(byte[] rom, int romSize);

		[BizImport(cc, EntryPoint = "_RA_IdentifyHash")]
		public abstract uint IdentifyHash(string hash);

		[BizImport(cc, EntryPoint = "_RA_ActivateGame")]
		public abstract void ActivateGame(uint gameId);

		[BizImport(cc, EntryPoint = "_RA_OnLoadNewRom")]
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

		[BizImport(cc, EntryPoint = "_RA_UpdateAppTitle")]
		public abstract void UpdateAppTitle(string message);

		[BizImport(cc, EntryPoint = "_RA_UserName")]
		public abstract IntPtr UserName();

		[BizImport(cc, EntryPoint = "_RA_HardcoreModeIsActive")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool HardcoreModeIsActive();

		[BizImport(cc, EntryPoint = "_RA_WarnDisableHardcore")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool WarnDisableHardcore(string activity);

		[BizImport(cc, EntryPoint = "_RA_OnReset")]
		public abstract void OnReset();

		[BizImport(cc, EntryPoint = "_RA_OnSaveState")]
		public abstract void OnSaveState(string filename);

		[BizImport(cc, EntryPoint = "_RA_OnLoadState")]
		public abstract void OnLoadState(string filename);

		[BizImport(cc, EntryPoint = "_RA_CaptureState")]
		public abstract int CaptureState(IntPtr buffer, int bufferSize);

		[BizImport(cc, EntryPoint = "_RA_RestoreState")]
		public abstract void RestoreState(IntPtr buffer);
	}
}
