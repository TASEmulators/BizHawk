using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

namespace BizHawk.Common
{
	public static class HeapApiImports
	{
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr GetProcessHeap();

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
		public static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, int dwBytes);

		/// <remarks>used in <c>#if false</c> code in <c>AviWriter.CodecToken.DeallocateAVICOMPRESSOPTIONS</c>, don't delete it</remarks>
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);
	}
}
