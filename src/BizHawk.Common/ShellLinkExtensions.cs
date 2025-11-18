using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Shell;

namespace Windows.Win32
{
	public static class ShellLinkExtensions
	{
		/// <seealso cref="UI_Shell_IShellLinkW_Extensions.GetPath(IShellLinkW, Span{char}, ref WIN32_FIND_DATAW, uint)"/>
		public static string GetPath(this IShellLinkW shellLink)
		{
			Span<char> buf = stackalloc char[(int) Win32Imports.MAX_PATH + 1];
			WIN32_FIND_DATAW pfd = default;
			shellLink.GetPath(buf, ref pfd, fFlags: default);
			var i = buf.IndexOf((char) 0);
			if (i >= 0) buf = buf.Slice(start: 0, length: i);
			return buf.ToString();
		}
	}
}
