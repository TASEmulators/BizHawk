using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Common
{
	public class Win32ShellContextMenu
	{
		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
		public interface IShellItem
		{
			IntPtr BindToHandler(IntPtr pbc,
				[MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
				[MarshalAs(UnmanagedType.LPStruct)] Guid riid);

			[PreserveSig]
			int GetParent(out IShellItem ppsi);

			IntPtr GetDisplayName(uint sigdnName);

			uint GetAttributes(uint sfgaoMask);

			int Compare(IShellItem psi, uint hint);
		}


		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214F2-0000-0000-C000-000000000046")]
		public interface IEnumIDList
		{
			[PreserveSig]
			int Next(uint celt, out IntPtr rgelt, out uint pceltFetched);

			[PreserveSig]
			int Skip(uint celt);

			[PreserveSig]
			int Reset();

			IEnumIDList Clone();
		}

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214E6-0000-0000-C000-000000000046")]
		public interface IShellFolder
		{
			void ParseDisplayName(
				[In] IntPtr hwnd,
				[In] IntPtr pbc,
				[In, MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName,
				[Out] out uint pchEaten,
				[Out] out IntPtr ppidl,
				[In, Out] ref uint pdwAttributes);

			[PreserveSig]
			int EnumObjects(
				[In] IntPtr hwnd,
				[In] SHCONTF grfFlags,
				[Out] out IEnumIDList ppenumIDList);

			void BindToObject(IntPtr pidl, IntPtr pbc,
							  [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
							  out IntPtr ppv);

			void BindToStorage(IntPtr pidl, IntPtr pbc,
							   [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
							   out IntPtr ppv);

			[PreserveSig]
			short CompareIDs(uint lParam, IntPtr pidl1, IntPtr pidl2);

			IntPtr CreateViewObject(IntPtr hwndOwner,
				[MarshalAs(UnmanagedType.LPStruct)] Guid riid);

			void GetAttributesOf(uint cidl,
				[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl,
				ref uint rgfInOut);

			void GetUIObjectOf(IntPtr hwndOwner, uint cidl,
				[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] apidl,
				[MarshalAs(UnmanagedType.LPStruct)] Guid riid,
				uint rgfReserved,
				out IntPtr ppv);

			void GetDisplayNameOf(IntPtr pidl, int uFlags, out STRRET pName);

			void SetNameOf(IntPtr hwnd, IntPtr pidl, string pszName, SHCONTF uFlags, out IntPtr ppidlOut);

			public enum SHCONTF
			{
				FOLDERS = 0x0020,
				NONFOLDERS = 0x0040,
				INCLUDEHIDDEN = 0x0080,
				INIT_ON_FIRST_NEXT = 0x0100,
				NETPRINTERSRCH = 0x0200,
				SHAREABLE = 0x0400,
				STORAGE = 0x0800
			}

			[StructLayout(LayoutKind.Explicit, Size = 264)]
			public struct STRRET
			{
				[FieldOffset(0)]
				public uint uType;
				[FieldOffset(4)]
				public IntPtr pOleStr;
				[FieldOffset(4)]
				public IntPtr pStr;
				[FieldOffset(4)]
				public uint uOffset;
				[FieldOffset(4)]
				public IntPtr cStr;
			}
		}

		[Flags]
		public enum TPM
		{
			TPM_LEFTBUTTON = 0x0000,
			TPM_RIGHTBUTTON = 0x0002,
			TPM_LEFTALIGN = 0x0000,
			TPM_CENTERALIGN = 0x000,
			TPM_RIGHTALIGN = 0x000,
			TPM_TOPALIGN = 0x0000,
			TPM_VCENTERALIGN = 0x0010,
			TPM_BOTTOMALIGN = 0x0020,
			TPM_HORIZONTAL = 0x0000,
			TPM_VERTICAL = 0x0040,
			TPM_NONOTIFY = 0x0080,
			TPM_RETURNCMD = 0x0100,
			TPM_RECURSE = 0x0001,
			TPM_HORPOSANIMATION = 0x0400,
			TPM_HORNEGANIMATION = 0x0800,
			TPM_VERPOSANIMATION = 0x1000,
			TPM_VERNEGANIMATION = 0x2000,
			TPM_NOANIMATION = 0x4000,
			TPM_LAYOUTRTL = 0x8000,
		}

		[Flags]
		public enum CMF : uint
		{
			NORMAL = 0x00000000,
			DEFAULTONLY = 0x00000001,
			VERBSONLY = 0x00000002,
			EXPLORE = 0x00000004,
			NOVERBS = 0x00000008,
			CANRENAME = 0x00000010,
			NODEFAULT = 0x00000020,
			INCLUDESTATIC = 0x00000040,
			EXTENDEDVERBS = 0x00000100,
			RESERVED = 0xffff0000,
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct CMINVOKECOMMANDINFO
		{
			public int cbSize;
			public int fMask;
			public IntPtr hwnd;
			public string lpVerb;
			public string lpParameters;
			public string lpDirectory;
			public int nShow;
			public int dwHotKey;
			public IntPtr hIcon;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct CMINVOKECOMMANDINFO_ByIndex
		{
			public int cbSize;
			public int fMask;
			public IntPtr hwnd;
			public int iVerb;
			public string lpParameters;
			public string lpDirectory;
			public int nShow;
			public int dwHotKey;
			public IntPtr hIcon;
		}

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214e4-0000-0000-c000-000000000046")]
		public interface IContextMenu
		{
			[PreserveSig]
			int QueryContextMenu(IntPtr hMenu, uint indexMenu, int idCmdFirst, int idCmdLast, CMF uFlags);

			void InvokeCommand(ref CMINVOKECOMMANDINFO pici);

			[PreserveSig]
			int GetCommandString(int idcmd, uint uflags, int reserved,
				[MarshalAs(UnmanagedType.LPStr)] StringBuilder commandstring,
				int cch);
		}

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214f4-0000-0000-c000-000000000046")]
		public interface IContextMenu2 : IContextMenu
		{
			[PreserveSig]
			new int QueryContextMenu(IntPtr hMenu, uint indexMenu, int idCmdFirst, int idCmdLast, CMF uFlags);

			void InvokeCommand(ref CMINVOKECOMMANDINFO_ByIndex pici);

			[PreserveSig]
			new int GetCommandString(int idcmd, uint uflags, int reserved,
				[MarshalAs(UnmanagedType.LPStr)] StringBuilder commandstring,
				int cch);

			[PreserveSig]
			int HandleMenuMsg(int uMsg, IntPtr wParam, IntPtr lParam);
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
		public static extern IShellItem SHCreateItemFromParsingName(
			[In] string pszPath,
			[In] IntPtr pbc,
			[In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

		[DllImport("shell32.dll", PreserveSig = false)]
		public static extern IntPtr SHGetIDListFromObject([In, MarshalAs(UnmanagedType.IUnknown)] object punk);

		[DllImport("shell32.dll", EntryPoint = "#16")]
		public static extern IntPtr ILFindLastID(IntPtr pidl);

		[DllImport("user32.dll")]
		public static extern int TrackPopupMenuEx(IntPtr hmenu, TPM fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

		private IContextMenu ComInterface { get; }
		private IContextMenu2 ComInterface2 { get; }

		private static readonly Guid SFObject = new("3981e224-f559-11d3-8e3a-00c04f6837d5");

		private Win32ShellContextMenu(string path)
		{
			Uri uri = new(path);

			// this should be the only scheme used in practice
			if (uri.Scheme != "file")
			{
				throw new NotSupportedException("Non-file Uri schemes are unsupported");
			}

			var shellItem = SHCreateItemFromParsingName(uri.LocalPath, IntPtr.Zero, typeof(IShellItem).GUID);

			IntPtr[] pidls = new IntPtr[1];
			pidls[0] = ILFindLastID(SHGetIDListFromObject(shellItem));
			shellItem.GetParent(out var parent);

			var result = parent.BindToHandler(IntPtr.Zero, SFObject, typeof(IShellFolder).GUID);

			IShellFolder shellFolder = (IShellFolder)Marshal.GetObjectForIUnknown(result);
			shellFolder.GetUIObjectOf(IntPtr.Zero, 1, pidls, typeof(IContextMenu).GUID, 0, out result);

			ComInterface = (IContextMenu)Marshal.GetObjectForIUnknown(result);
			ComInterface2 = (IContextMenu2)ComInterface;
		}

		private ref struct TempMenu
		{
			[DllImport("user32.dll")]
			private static extern IntPtr CreatePopupMenu();

			[DllImport("user32.dll", SetLastError = true)]
			private static extern bool DestroyMenu(IntPtr hMenu);

			public IntPtr Handle { get; private set; }

			public TempMenu()
			{
				Handle = CreatePopupMenu();
				if (Handle == IntPtr.Zero)
				{
					throw new InvalidOperationException($"{nameof(CreatePopupMenu)} returned NULL!");
				}
			}

			public void Dispose()
			{
				if (Handle != IntPtr.Zero)
				{
					_ = DestroyMenu(Handle);
					Handle = IntPtr.Zero;
				}
			}
		}

		public static void ShowContextMenu(string path, IntPtr parentWindow, int x, int y)
		{
			Win32ShellContextMenu ctxMenu = new(path);
			using TempMenu menu = new();
			const int CmdFirst = 0x8000;
			ctxMenu.ComInterface.QueryContextMenu(menu.Handle, 0, CmdFirst, int.MaxValue, CMF.EXPLORE);
			int command = TrackPopupMenuEx(menu.Handle, TPM.TPM_RETURNCMD, x, y, parentWindow, IntPtr.Zero);
			if (command > 0)
			{
				const int SW_SHOWNORMAL = 1;
				CMINVOKECOMMANDINFO_ByIndex invoke = default;
				invoke.cbSize = Marshal.SizeOf(invoke);
				invoke.iVerb = command - CmdFirst;
				invoke.nShow = SW_SHOWNORMAL;
				ctxMenu.ComInterface2.InvokeCommand(ref invoke);
			}
		}
	}
}
