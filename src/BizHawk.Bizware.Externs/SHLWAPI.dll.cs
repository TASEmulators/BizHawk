using System.IO;

using Windows.Win32.Foundation;

namespace Windows.Win32
{
	public static partial class Win32Imports
	{
		/// <inheritdoc cref="PathRelativePathToW(PWSTR, PCWSTR, uint, PCWSTR, uint)"/>
		public static BOOL PathRelativePathToW(
			Span<char> pszPath,
			string pszFrom,
			FileAttributes dwAttrFrom,
			string pszTo,
			FileAttributes dwAttrTo)
				=> PathRelativePathToW(
					pszPath: pszPath,
					pszFrom: pszFrom,
					dwAttrFrom: unchecked((uint) dwAttrFrom),
					pszTo: pszTo,
					dwAttrTo: unchecked((uint) dwAttrTo));
	}
}
