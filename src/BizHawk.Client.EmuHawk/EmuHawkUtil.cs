using System;
using System.IO;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class EmuHawkUtil
	{
		/// <remarks>http://stackoverflow.com/questions/139010/how-to-resolve-a-lnk-in-c-sharp</remarks>
		public static string ResolveShortcut(string filename)
		{
			if (filename.Contains("|") || OSTailoredCode.IsUnixHost || !".lnk".Equals(Path.GetExtension(filename), StringComparison.InvariantCultureIgnoreCase)) return filename; // archive internal files are never shortcuts (and choke when analyzing any further)
			var link = new ShellLinkImports.ShellLink();
			const uint STGM_READ = 0;
			((ShellLinkImports.IPersistFile) link).Load(filename, STGM_READ);
#if false
			// TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.
			((ShellLinkImports.IShellLinkW) link).Resolve(hwnd, 0);
#endif
			var sb = new StringBuilder(Win32Imports.MAX_PATH);
			((ShellLinkImports.IShellLinkW) link).GetPath(sb, sb.Capacity, out _, 0);
			return sb.Length == 0 ? filename : sb.ToString(); // maybe? what if it's invalid?
		}
	}
}
