using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BizHawk.Common
{
	public unsafe class Win32ShellContextMenu : IDisposable
	{
		private IContextMenu? CMI;
		private IContextMenu2? CM2I;

		private static readonly Guid GUID_ICONTEXTMENU = new("000214E4-0000-0000-C000-000000000046");

		private static readonly Guid GUID_ICONTEXTMENU2 = new("000214F4-0000-0000-C000-000000000046");

		private static readonly Guid GUID_ISHELLFOLDER = new("000214E6-0000-0000-C000-000000000046");

		private static readonly Guid GUID_ISHELLITEM = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");

		private static readonly Guid SFObject = new("3981e224-f559-11d3-8e3a-00c04f6837d5");

		private Win32ShellContextMenu(string path)
		{
			var uri = new Uri(path);

			// this should be the only scheme used in practice
			if (uri.Scheme != "file")
			{
				throw new NotSupportedException("Non-file Uri schemes are unsupported");
			}

			var hr = Shell32Imports.SHCreateItemFromParsingName(uri.LocalPath, default, in GUID_ISHELLITEM, out var psi);
			Marshal.ThrowExceptionForHR(hr);
			var sii = (IShellItem) psi;

			ITEMIDLIST* pidls;
			IShellItem psii;
			try
			{
				hr = Shell32Imports.SHGetIDListFromObject(psi, out var ppidl);
				Marshal.ThrowExceptionForHR(hr);

				pidls = Shell32Imports.ILFindLastID(ppidl);
				sii.GetParent(out psii);
			}
			finally
			{
//				sii.Release(sii);
			}

			IShellFolder sfi;
			try
			{
				psii.BindToHandler(default, in SFObject, in GUID_ISHELLFOLDER, out var psf);
				sfi = (IShellFolder) psf;
			}
			finally
			{
//				psii.Release(psii);
			}

			IContextMenu cmi;
			try
			{
				sfi.GetUIObjectOf(HWND.Null, cidl: 1, &pidls, in GUID_ICONTEXTMENU, out var pcm);
				cmi = (IContextMenu) pcm;
			}
			finally
			{
//				sfi.Release(sfi);
			}

			IContextMenu2 cm2i;
			try
			{
//				cmi.QueryInterface(cmi, in GUID_ICONTEXTMENU2, out var pcm2);
				var pcm2 = cmi;
				cm2i = (IContextMenu2) pcm2;
			}
			catch
			{
//				cmi.Release(cmi);
				throw;
			}

			CMI = cmi;
			CM2I = cm2i;
		}

		public void Dispose()
		{
			if (CM2I != null)
			{
//				CM2I.Release(CM2I);
				CM2I = null;
			}

			if (CMI != null)
			{
//				CMI.Release(CMI);
				CMI = null;
			}
		}

		private ref struct TempMenu
		{
			public HMENU Handle { get; private set; }

			public TempMenu()
			{
				Handle = Win32Imports.CreatePopupMenu();
				if (Handle.IsNull)
				{
					throw new InvalidOperationException($"{nameof(Win32Imports.CreatePopupMenu)} returned NULL!");
				}
			}

			public void Dispose()
			{
				if (!Handle.IsNull)
				{
					_ = Win32Imports.DestroyMenu(Handle);
					Handle = HMENU.Null;
				}
			}
		}

		public static void ShowContextMenu(string path, IntPtr parentWindow, int x, int y)
		{
			using var ctxMenu = new Win32ShellContextMenu(path);
			using var menu = new TempMenu();

			const int CmdFirst = 0x8000;
			ctxMenu.CMI!.QueryContextMenu(menu.Handle, 0, CmdFirst, uint.MaxValue, Win32Imports.CMF_EXPLORE);

			var command = Win32Imports.TrackPopupMenuEx(
				menu.Handle,
				TPMFLAGS.RETURNCMD,
				x: x,
				y: y,
				new(parentWindow),
				lptpm: default).Value;
			if (command > 0)
			{
				const int SW_SHOWNORMAL = 1;
				CMINVOKECOMMANDINFO invoke = default;
				invoke.cbSize = (uint)Marshal.SizeOf<CMINVOKECOMMANDINFO>();
				invoke.lpVerb = new(unchecked((byte*) (IntPtr) (command - CmdFirst)));
				invoke.nShow = SW_SHOWNORMAL;
				ctxMenu.CM2I!.InvokeCommand(&invoke);
			}
		}
	}
}
