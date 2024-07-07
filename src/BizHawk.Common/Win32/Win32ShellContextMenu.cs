using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable UnusedMember.Local

namespace BizHawk.Common
{
	public unsafe class Win32ShellContextMenu : IDisposable
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct IShellItem
		{
			public static readonly Guid Guid = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

			[StructLayout(LayoutKind.Sequential)]
			public struct IShellItemVtbl
			{
				// IUnknown functions
				public delegate* unmanaged[Stdcall]<IShellItem*, in Guid, out IntPtr, int> QueryInterface;
				public delegate* unmanaged[Stdcall]<IShellItem*, uint> AddRef;
				public delegate* unmanaged[Stdcall]<IShellItem*, uint> Release;
				// IShellItem functions
				public delegate* unmanaged[Stdcall]<IShellItem*, IntPtr, in Guid, in Guid, out IntPtr, int> BindToHandler;
				public delegate* unmanaged[Stdcall]<IShellItem*, out IShellItem*, int> GetParent;
				public delegate* unmanaged[Stdcall]<IShellItem*, int, out IntPtr, int> GetDisplayName;
				public delegate* unmanaged[Stdcall]<IShellItem*, uint, out uint, int> GetAttributes;
				public delegate* unmanaged[Stdcall]<IShellItem*, IShellItem*, uint, out int, int> Compare;
			}

			public IShellItemVtbl* lpVtbl;

			public void BindToHandler(IntPtr pbc, Guid bhid, Guid riid, out IntPtr ppv)
			{
				var hr = lpVtbl->BindToHandler((IShellItem*)Unsafe.AsPointer(ref this), pbc, in bhid, in riid, out ppv);
				Marshal.ThrowExceptionForHR(hr);
			}

			public void GetParent(out IShellItem* ppsi)
			{
				var hr = lpVtbl->GetParent((IShellItem*)Unsafe.AsPointer(ref this), out ppsi);
				Marshal.ThrowExceptionForHR(hr);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct IShellFolder
		{
			public static readonly Guid Guid = new("000214E6-0000-0000-C000-000000000046");

			[StructLayout(LayoutKind.Sequential)]
			public struct IShellFolderVtbl
			{
				// IUnknown functions
				public delegate* unmanaged[Stdcall]<IShellFolder*, in Guid, out IntPtr, int> QueryInterface;
				public delegate* unmanaged[Stdcall]<IShellFolder*, uint> AddRef;
				public delegate* unmanaged[Stdcall]<IShellFolder*, uint> Release;
				// IShellFolder functions
				public delegate* unmanaged[Stdcall]<IShellFolder*, IntPtr, IntPtr, IntPtr, uint*, out IntPtr, uint*, int> ParseDisplayName;
				public delegate* unmanaged[Stdcall]<IShellFolder*, IntPtr, uint, out IntPtr, int> EnumObjects;
				public delegate* unmanaged[Stdcall]<IShellFolder*, IntPtr, IntPtr, in Guid, out IntPtr, int> BindToObject;
				public delegate* unmanaged[Stdcall]<IShellFolder*, IntPtr, IntPtr, in Guid, out IntPtr, int> BindToStorage;
				public delegate* unmanaged[Stdcall]<IShellFolder*, IntPtr, IntPtr, IntPtr, int> CompareIDs;
				public delegate* unmanaged[Stdcall]<IShellFolder*, IntPtr, in Guid, out IntPtr, int> CreateViewObject;
				public delegate* unmanaged[Stdcall]<IShellFolder*, uint, IntPtr*, ref uint, int> GetAttributesOf;
				public delegate* unmanaged[Stdcall]<IShellFolder*, IntPtr, uint, IntPtr*, in Guid, uint*, out IntPtr, int> GetUIObjectOf;
				public delegate* unmanaged[Stdcall]<IShellFolder*, IntPtr, uint, IntPtr, int> GetDisplayNameOf;
				public delegate* unmanaged[Stdcall]<IShellFolder*, IntPtr, IntPtr, IntPtr, uint, out IntPtr, int> SetNameOf;
			}

			public IShellFolderVtbl* lpVtbl;

			public void GetUIObjectOf(IntPtr hwndOwner, uint cidl, IntPtr* apidl, Guid riid, out IntPtr ppv)
			{
				var hr = lpVtbl->GetUIObjectOf((IShellFolder*)Unsafe.AsPointer(ref this), hwndOwner, cidl, apidl, in riid, null, out ppv);
				Marshal.ThrowExceptionForHR(hr);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct IContextMenu
		{
			public static readonly Guid Guid = new("000214e4-0000-0000-c000-000000000046");

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

			[StructLayout(LayoutKind.Sequential)]
			public struct CMINVOKECOMMANDINFO
			{
				public uint cbSize;
				public uint fMask;
				public IntPtr hwnd;
				public IntPtr lpVerb;
				public IntPtr lpParameters;
				public IntPtr lpDirectory;
				public int nShow;
				public uint dwHotKey;
				public IntPtr hIcon;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct IContextMenuVtbl
			{
				// IUnknown functions
				public delegate* unmanaged[Stdcall]<IContextMenu*, in Guid, out IntPtr, int> QueryInterface;
				public delegate* unmanaged[Stdcall]<IContextMenu*, uint> AddRef;
				public delegate* unmanaged[Stdcall]<IContextMenu*, uint> Release;
				// IContextMenu functions
				public delegate* unmanaged[Stdcall]<IContextMenu*, IntPtr, uint, uint, uint, CMF, int> QueryContextMenu;
				public delegate* unmanaged[Stdcall]<IContextMenu*, CMINVOKECOMMANDINFO*, int> InvokeCommand;
				public delegate* unmanaged[Stdcall]<IContextMenu*, UIntPtr, uint, uint*, IntPtr, uint, int> GetCommandString;
			}

			public IContextMenuVtbl* lpVtbl;

			public void QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, CMF uFlags)
			{
				var hr = lpVtbl->QueryContextMenu((IContextMenu*)Unsafe.AsPointer(ref this), hmenu, indexMenu, idCmdFirst, idCmdLast, uFlags);
				Marshal.ThrowExceptionForHR(hr);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct IContextMenu2
		{
			public static readonly Guid Guid = new("000214f4-0000-0000-c000-000000000046");

			[StructLayout(LayoutKind.Sequential)]
			public struct IContextMenu2Vtbl
			{
				// IUnknown functions
				public delegate* unmanaged[Stdcall]<IContextMenu2*, in Guid, out IntPtr, int> QueryInterface;
				public delegate* unmanaged[Stdcall]<IContextMenu2*, uint> AddRef;
				public delegate* unmanaged[Stdcall]<IContextMenu2*, uint> Release;
				// IContextMenu functions
				public delegate* unmanaged[Stdcall]<IContextMenu2*, IntPtr, uint, uint, uint, IContextMenu.CMF, int> QueryContextMenu;
				public delegate* unmanaged[Stdcall]<IContextMenu2*, IContextMenu.CMINVOKECOMMANDINFO*, int> InvokeCommand;
				public delegate* unmanaged[Stdcall]<IContextMenu2*, UIntPtr, uint, uint*, IntPtr, uint, int> GetCommandString;
				// IContextMenu2 functions
				public delegate* unmanaged[Stdcall]<IContextMenu2*, uint, IntPtr, IntPtr, int> HandleMenuMsg;
			}

			public IContextMenu2Vtbl* lpVtbl;

			public void InvokeCommand(IContextMenu.CMINVOKECOMMANDINFO* pici)
			{
				var hr = lpVtbl->InvokeCommand((IContextMenu2*)Unsafe.AsPointer(ref this), pici);
				Marshal.ThrowExceptionForHR(hr);
			}
		}

		private IContextMenu* CMI;
		private IContextMenu2* CM2I;

		private static readonly Guid SFObject = new("3981e224-f559-11d3-8e3a-00c04f6837d5");

		private Win32ShellContextMenu(string path)
		{
			var uri = new Uri(path);

			// this should be the only scheme used in practice
			if (uri.Scheme != "file")
			{
				throw new NotSupportedException("Non-file Uri schemes are unsupported");
			}

			var hr = Shell32Imports.SHCreateItemFromParsingName(uri.LocalPath, IntPtr.Zero, IShellItem.Guid, out var psi);
			Marshal.ThrowExceptionForHR(hr);
			var sii = (IShellItem*)psi;

			IntPtr pidls;
			IShellItem* psii;
			try
			{
				hr = Shell32Imports.SHGetIDListFromObject(psi, out var ppidl);
				Marshal.ThrowExceptionForHR(hr);

				pidls = Shell32Imports.ILFindLastID(ppidl);
				sii->GetParent(out psii);
			}
			finally
			{
				sii->lpVtbl->Release(sii);
			}

			IShellFolder* sfi;
			try
			{
				psii->BindToHandler(IntPtr.Zero, SFObject, IShellFolder.Guid, out var psf);
				sfi = (IShellFolder*)psf;
			}
			finally
			{
				psii->lpVtbl->Release(psii);
			}

			IContextMenu* cmi;
			try
			{
				sfi->GetUIObjectOf(IntPtr.Zero, 1, &pidls, IContextMenu.Guid, out var pcm);
				cmi = (IContextMenu*)pcm;
			}
			finally
			{
				sfi->lpVtbl->Release(sfi);
			}

			IContextMenu2* cm2i;
			try
			{
				cmi->lpVtbl->QueryInterface(cmi, in IContextMenu2.Guid, out var pcm2);
				cm2i = (IContextMenu2*)pcm2;
			}
			catch
			{
				cmi->lpVtbl->Release(cmi);
				throw;
			}

			CMI = cmi;
			CM2I = cm2i;
		}

		public void Dispose()
		{
			if (CM2I != null)
			{
				CM2I->lpVtbl->Release(CM2I);
				CM2I = null;
			}

			if (CMI != null)
			{
				CMI->lpVtbl->Release(CMI);
				CMI = null;
			}
		}

		private ref struct TempMenu
		{
			public IntPtr Handle { get; private set; }

			public TempMenu()
			{
				Handle = Win32Imports.CreatePopupMenu();
				if (Handle == IntPtr.Zero)
				{
					throw new InvalidOperationException($"{nameof(Win32Imports.CreatePopupMenu)} returned NULL!");
				}
			}

			public void Dispose()
			{
				if (Handle != IntPtr.Zero)
				{
					_ = Win32Imports.DestroyMenu(Handle);
					Handle = IntPtr.Zero;
				}
			}
		}

		public static void ShowContextMenu(string path, IntPtr parentWindow, int x, int y)
		{
			using var ctxMenu = new Win32ShellContextMenu(path);
			using var menu = new TempMenu();

			const int CmdFirst = 0x8000;
			ctxMenu.CMI->QueryContextMenu(menu.Handle, 0, CmdFirst, uint.MaxValue, IContextMenu.CMF.EXPLORE);

			var command = Win32Imports.TrackPopupMenuEx(menu.Handle, Win32Imports.TPM.RETURNCMD, x, y, parentWindow, IntPtr.Zero);
			if (command > 0)
			{
				const int SW_SHOWNORMAL = 1;
				IContextMenu.CMINVOKECOMMANDINFO invoke = default;
				invoke.cbSize = (uint)Marshal.SizeOf<IContextMenu.CMINVOKECOMMANDINFO>();
				invoke.lpVerb = new(command - CmdFirst);
				invoke.nShow = SW_SHOWNORMAL;
				ctxMenu.CM2I->InvokeCommand(&invoke);
			}
		}
	}
}
