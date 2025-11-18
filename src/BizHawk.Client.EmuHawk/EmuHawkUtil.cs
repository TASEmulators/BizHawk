using System.IO;
using System.Security.Principal;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;

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

			using var link = new ShellLinkImports.ShellLink();

			unsafe
			{
				const uint STGM_READ = 0;
				((ShellLinkImports.IPersistFile*)link)->Load(filename, STGM_READ);

#if false
				// TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.
				((ShellLinkImports.IShellLinkW*)link)->Resolve(hwnd, 0);
#endif

				((ShellLinkImports.IShellLinkW*) link)->GetPath(out var path, (int) Win32Imports.MAX_PATH + 1, 0);
				return path;
			}
		}
	}
}
