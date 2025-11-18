using System.Runtime.InteropServices;

namespace Windows.Win32
{
	public static partial class Win32Imports
	{
		/// <remarks>alias for <see cref="Marshal.GetLastWin32Error"/>, per <c>PInvoke003</c>/<see href="https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.getlastwin32error?view=netstandard-2.0#remarks"/></remarks>
		public static uint GetLastError()
			=> unchecked((uint) Marshal.GetLastWin32Error());
	}
}
