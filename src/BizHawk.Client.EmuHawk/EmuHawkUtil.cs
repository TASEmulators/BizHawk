using System.IO;
using System.Security.Principal;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;

using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace BizHawk.Client.EmuHawk
{
	public static class EmuHawkUtil
	{
		/// <summary><see langword="true"/> iff running as Administrator (on Windows) or Superuser (on Unix under Mono)</summary>
		/// <remarks>TODO check .NET Core</remarks>
		public static readonly bool CLRHostHasElevatedPrivileges = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

		/// <remarks>http://stackoverflow.com/questions/139010/how-to-resolve-a-lnk-in-c-sharp</remarks>
		public static string ResolveShortcut(string filename)
		{
			if (OSTailoredCode.IsUnixHost || HawkFile.PathContainsPipe(filename)
				|| !".lnk".EqualsIgnoreCase(Path.GetExtension(filename)))
			{
				return filename; // archive internal files are never shortcuts (and choke when analyzing any further)
			}

			ShellLink link = new();
			((IPersistFile) link).Load(filename, unchecked((uint) STGM.STGM_READ));
#if false
			// TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.
			((IShellLinkW) link).Resolve(hwnd, 0);
#endif
			return ((IShellLinkW) link).GetPath();
		}
	}
}
