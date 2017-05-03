using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	static class PInvokes
	{
		[DllImport("shfolder.dll", CharSet = CharSet.Auto)]
		internal static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, StringBuilder lpszPath);

		[Flags()]
		public enum SLGP_FLAGS
		{
			/// <summary>Retrieves the standard short (8.3 format) file name</summary>
			SLGP_SHORTPATH = 0x1,
			/// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
			SLGP_UNCPRIORITY = 0x2,
			/// <summary>Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded</summary>
			SLGP_RAWPATH = 0x4
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct WIN32_FIND_DATAW
		{
			public uint dwFileAttributes;
			public long ftCreationTime;
			public long ftLastAccessTime;
			public long ftLastWriteTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
			public uint dwReserved0;
			public uint dwReserved1;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string cFileName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			public string cAlternateFileName;
		}

		[Flags()]
		public enum SLR_FLAGS
		{
			/// <summary>
			/// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
			/// the high-order word of fFlags can be set to a time-out value that specifies the
			/// maximum amount of time to be spent resolving the link. The function returns if the
			/// link cannot be resolved within the time-out duration. If the high-order word is set
			/// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
			/// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
			/// duration, in milliseconds.
			/// </summary>
			SLR_NO_UI = 0x1,
			/// <summary>Obsolete and no longer used</summary>
			SLR_ANY_MATCH = 0x2,
			/// <summary>If the link object has changed, update its path and list of identifiers.
			/// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
			/// whether or not the link object has changed.</summary>
			SLR_UPDATE = 0x4,
			/// <summary>Do not update the link information</summary>
			SLR_NOUPDATE = 0x8,
			/// <summary>Do not execute the search heuristics</summary>
			SLR_NOSEARCH = 0x10,
			/// <summary>Do not use distributed link tracking</summary>
			SLR_NOTRACK = 0x20,
			/// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
			/// removable media across multiple devices based on the volume name. It also uses the
			/// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
			/// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
			SLR_NOLINKINFO = 0x40,
			/// <summary>Call the Microsoft Windows Installer</summary>
			SLR_INVOKE_MSI = 0x80
		}


		/// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
		[ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
		public interface IShellLinkW
		{
			/// <summary>Retrieves the path and file name of a Shell link object</summary>
			void GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags);
			/// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
			void GetIDList(out IntPtr ppidl);
			/// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
			void SetIDList(IntPtr pidl);
			/// <summary>Retrieves the description string for a Shell link object</summary>
			void GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
			/// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
			void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
			/// <summary>Retrieves the name of the working directory for a Shell link object</summary>
			void GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
			/// <summary>Sets the name of the working directory for a Shell link object</summary>
			void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
			/// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
			void GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
			/// <summary>Sets the command-line arguments for a Shell link object</summary>
			void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
			/// <summary>Retrieves the hot key for a Shell link object</summary>
			void GetHotkey(out short pwHotkey);
			/// <summary>Sets a hot key for a Shell link object</summary>
			void SetHotkey(short wHotkey);
			/// <summary>Retrieves the show command for a Shell link object</summary>
			void GetShowCmd(out int piShowCmd);
			/// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
			void SetShowCmd(int iShowCmd);
			/// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
			void GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
					int cchIconPath, out int piIcon);
			/// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
			void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
			/// <summary>Sets the relative path to the Shell link object</summary>
			void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
			/// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
			void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);
			/// <summary>Sets the path and file name of a Shell link object</summary>
			void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);

		}

		[ComImport, Guid("0000010c-0000-0000-c000-000000000046"),
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IPersist
		{
			[PreserveSig]
			void GetClassID(out Guid pClassID);
		}


		[ComImport, Guid("0000010b-0000-0000-C000-000000000046"),
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IPersistFile : IPersist
		{
			new void GetClassID(out Guid pClassID);
			[PreserveSig]
			int IsDirty();

			[PreserveSig]
			void Load([In, MarshalAs(UnmanagedType.LPWStr)]string pszFileName, uint dwMode);

			[PreserveSig]
			void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
					[In, MarshalAs(UnmanagedType.Bool)] bool fRemember);

			[PreserveSig]
			void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

			[PreserveSig]
			void GetCurFile([In, MarshalAs(UnmanagedType.LPWStr)] string ppszFileName);
		}

		public const uint STGM_READ = 0;
		public const int MAX_PATH = 260;

		// CLSID_ShellLink from ShlGuid.h 
		[
				ComImport(),
				Guid("00021401-0000-0000-C000-000000000046")
		]
		public class ShellLink
		{
		}
	}

	public static class Win32PInvokes
	{
		//http://stackoverflow.com/questions/139010/how-to-resolve-a-lnk-in-c-sharp
		public static string ResolveShortcut(string filename)
		{
			// archive internal files are never shortcuts (and choke when analyzing any further)
			if (filename.Contains("|"))
			{
				return filename;
			}

			if (Path.GetExtension(filename).ToLowerInvariant() != ".lnk")
			{
				return filename;
			}

			PInvokes.ShellLink link = new PInvokes.ShellLink();
			((PInvokes.IPersistFile)link).Load(filename, PInvokes.STGM_READ);
			// TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.  
			// ((IShellLinkW)link).Resolve(hwnd, 0) 
			StringBuilder sb = new StringBuilder(PInvokes.MAX_PATH);
			PInvokes.WIN32_FIND_DATAW data = new PInvokes.WIN32_FIND_DATAW();
			((PInvokes.IShellLinkW)link).GetPath(sb, sb.Capacity, out data, 0);

			//maybe? what if it's invalid?
			if (sb.Length == 0)
				return filename;
			return sb.ToString();
		}

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsProcessorFeaturePresent(ProcessorFeature processorFeature);

		public enum ProcessorFeature : uint
		{
			/// <summary>
			/// On a Pentium, a floating-point precision error can occur in rare circumstances
			/// </summary>
			FloatingPointPrecisionErrata = 0,
			/// <summary>
			/// Floating-point operations are emulated using a software emulator.
			/// This function returns a nonzero value if floating-point operations are emulated; otherwise, it returns zero.
			/// </summary>
			FloatingPointEmulated = 1,
			/// <summary>
			/// The atomic compare and exchange operation (cmpxchg) is available
			/// </summary>
			CompareExchangeDouble = 2,
			/// <summary>
			/// The MMX instruction set is available
			/// </summary>
			InstructionsMMXAvailable = 3,
			/// <summary>
			/// The SSE instruction set is available
			/// </summary>
			InstructionsXMMIAvailable = 6,
			/// <summary>
			/// The 3D-Now instruction set is available.
			/// </summary>
			Instruction3DNowAvailable = 7,
			/// <summary>
			/// The RDTSC instruction is available
			/// </summary>
			InstructionRDTSCAvailable = 8,
			/// <summary>
			/// The processor is PAE-enabled
			/// </summary>
			PAEEnabled = 9,
			/// <summary>
			/// The SSE2 instruction set is available
			/// </summary>
			InstructionsXMMI64Available = 10,
			/// <summary>
			/// Data execution prevention is enabled. (This feature is not supported until Windows XP SP2 and Windows Server 2003 SP1)
			/// </summary>
			NXEnabled = 12,
			/// <summary>
			/// The SSE3 instruction set is available. (This feature is not supported until Windows Vista)
			/// </summary>
			InstructionsSSE3Available = 13,
			/// <summary>
			/// The atomic compare and exchange 128-bit operation (cmpxchg16b) is available. (This feature is not supported until Windows Vista)
			/// </summary>
			CompareExchange128 = 14,
			/// <summary>
			/// The atomic compare 64 and exchange 128-bit operation (cmp8xchg16) is available (This feature is not supported until Windows Vista.)
			/// </summary>
			Compare64Exchange128 = 15,
			/// <summary>
			/// TBD
			/// </summary>
			ChannelsEnabled = 16,
		}



	}

	//largely from https://raw.githubusercontent.com/noserati/tpl/master/ThreadAffinityTaskScheduler.cs (MIT license)
	public static class Win32ThreadHacks
	{
		internal static class NativeMethods
		{
			[DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
			public static extern Int32 WaitForSingleObject(Microsoft.Win32.SafeHandles.SafeWaitHandle handle, uint milliseconds);

			public const uint QS_KEY = 0x0001;
			public const uint QS_MOUSEMOVE = 0x0002;
			public const uint QS_MOUSEBUTTON = 0x0004;
			public const uint QS_POSTMESSAGE = 0x0008;
			public const uint QS_TIMER = 0x0010;
			public const uint QS_PAINT = 0x0020;
			public const uint QS_SENDMESSAGE = 0x0040;
			public const uint QS_HOTKEY = 0x0080;
			public const uint QS_ALLPOSTMESSAGE = 0x0100;
			public const uint QS_RAWINPUT = 0x0400;

			public const uint QS_MOUSE = (QS_MOUSEMOVE | QS_MOUSEBUTTON);
			public const uint QS_INPUT = (QS_MOUSE | QS_KEY | QS_RAWINPUT);
			public const uint QS_ALLEVENTS = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY);
			public const uint QS_ALLINPUT = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE);

			public const uint MWMO_INPUTAVAILABLE = 0x0004;
			public const uint MWMO_WAITALL = 0x0001;

			public const uint PM_REMOVE = 0x0001;
			public const uint PM_NOREMOVE = 0;

			public const uint WAIT_TIMEOUT = 0x00000102;
			public const uint WAIT_FAILED = 0xFFFFFFFF;
			public const uint INFINITE = 0xFFFFFFFF;
			public const uint WAIT_OBJECT_0 = 0;
			public const uint WAIT_ABANDONED_0 = 0x00000080;
			public const uint WAIT_IO_COMPLETION = 0x000000C0;

			[StructLayout(LayoutKind.Sequential)]
			public struct MSG
			{
				public IntPtr hwnd;
				public uint message;
				public IntPtr wParam;
				public IntPtr lParam;
				public uint time;
				public int x;
				public int y;
			}

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll")]
			public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool TranslateMessage([In] ref MSG lpMsg);

			[DllImport("ole32.dll", PreserveSig = false)]
			public static extern void OleInitialize(IntPtr pvReserved);

			[DllImport("ole32.dll", PreserveSig = true)]
			public static extern void OleUninitialize();

			[DllImport("kernel32.dll")]
			public static extern uint GetTickCount();

			[DllImport("user32.dll")]
			public static extern uint GetQueueStatus(uint flags);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern uint MsgWaitForMultipleObjectsEx(
					uint nCount, IntPtr[] pHandles, uint dwMilliseconds, uint dwWakeMask, uint dwFlags);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern uint WaitForMultipleObjects(
					uint nCount, IntPtr[] lpHandles, bool bWaitAll, uint dwMilliseconds);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SetEvent(IntPtr hEvent);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool CloseHandle(IntPtr hObject);
		}

		/// <summary>
		/// Analyze the result of the native wait API
		/// </summary>
		static bool IsNativeWaitSuccessful(uint count, uint nativeResult, out int managedResult)
		{
			if (nativeResult == (NativeMethods.WAIT_OBJECT_0 + count))
			{
				// a is message pending, only valid for MsgWaitForMultipleObjectsEx
				managedResult = unchecked((int)nativeResult);
				return false;
			}

			if (nativeResult >= NativeMethods.WAIT_OBJECT_0 && nativeResult < (NativeMethods.WAIT_OBJECT_0 + count))
			{
				managedResult = unchecked((int)(nativeResult - NativeMethods.WAIT_OBJECT_0));
				return true;
			}

			if (nativeResult >= NativeMethods.WAIT_ABANDONED_0 && nativeResult < (NativeMethods.WAIT_ABANDONED_0 + count))
			{
				managedResult = unchecked((int)(nativeResult - NativeMethods.WAIT_ABANDONED_0));
				throw new AbandonedMutexException();
			}

			if (nativeResult == NativeMethods.WAIT_TIMEOUT)
			{
				managedResult = WaitHandle.WaitTimeout;
				return false;
			}

			throw new InvalidOperationException();
		}

		/// <summary>
		/// functionally the same as WaitOne, but does not message pump
		/// </summary>
		public static void HackyPinvokeWaitOne(WaitHandle handle)
		{
			NativeMethods.WaitForSingleObject(handle.SafeWaitHandle, 0xFFFFFFFF);
		}

		/// <summary>
		/// Functionally the same as WaitOne(), but pumps com messa
		/// </summary>
		/// <param name="handle"></param>
		public static void HackyComWaitOne(WaitHandle handle)
		{
			uint nativeResult; // result of the native wait API (WaitForMultipleObjects or MsgWaitForMultipleObjectsEx)
			int managedResult; // result to return from WaitHelper

			IntPtr[] waitHandles = new IntPtr[]{
				handle.SafeWaitHandle.DangerousGetHandle() };
			uint count = 1;

			uint QS_MASK = NativeMethods.QS_ALLINPUT; // message queue status
			QS_MASK = 0; //bizhawk edit?? did we need any messages here?? apparently not???

			// the core loop
			var msg = new NativeMethods.MSG();
			while (true)
			{
				// MsgWaitForMultipleObjectsEx with MWMO_INPUTAVAILABLE returns,
				// even if there's a message already seen but not removed in the message queue
				nativeResult = NativeMethods.MsgWaitForMultipleObjectsEx(
						count, waitHandles,
						(uint)0xFFFFFFFF,
						QS_MASK,
						NativeMethods.MWMO_INPUTAVAILABLE);

				if (IsNativeWaitSuccessful(count, nativeResult, out managedResult) || WaitHandle.WaitTimeout == managedResult)
					break;
				// there is a message, pump and dispatch it
				if (NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, NativeMethods.PM_REMOVE))
				{
					NativeMethods.TranslateMessage(ref msg);
					NativeMethods.DispatchMessage(ref msg);
				}
			}
			//m64pFrameComplete.WaitOne();
		}

	}

	public static class Win32Hacks
	{
		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);

		//warning: youll have to copy this into the main assembly for your exes in order to run it when booting.
		//I only put this for use here by external cores
		public static void RemoveMOTW(string path)
		{
			DeleteFileW(path + ":Zone.Identifier");
		}
	}

}