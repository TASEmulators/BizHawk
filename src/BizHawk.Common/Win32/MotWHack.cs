namespace BizHawk.Common
{
	/// <remarks>This code (and an import for <see cref="Win32Imports.DeleteFileW"/>) is duplicated in each executable project because it needs to be used before loading assemblies.</remarks>
	public static partial class MotWHack
	{
		public static void RemoveMOTW(string path)
			=> Win32Imports.DeleteFileW($"{path}:Zone.Identifier");
	}
}
