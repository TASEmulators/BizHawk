using Windows.Win32;

namespace BizHawk.Common
{
	/// <remarks>This code (and an import for <c>DeleteFileW</c>) is duplicated in each executable project because it needs to be used before loading assemblies.</remarks>
	public static class MotWHack
	{
		public static void RemoveMOTW(string path)
			=> Win32Imports.DeleteFileW($"{path}:Zone.Identifier");
	}
}
