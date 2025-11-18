using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

using HANDLE = System.IntPtr;
using HEAP_FLAGS = uint;

namespace BizHawk.Common
{
	public static class HeapApiImports
	{
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern HANDLE GetProcessHeap();

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
		public static extern IntPtr HeapAlloc(HANDLE hHeap, HEAP_FLAGS dwFlags, nuint dwBytes);

		/// <remarks>used in <c>#if false</c> code in <c>AviWriter.CodecToken.DeallocateAVICOMPRESSOPTIONS</c>, don't delete it</remarks>
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool HeapFree(HANDLE hHeap, HEAP_FLAGS dwFlags, IntPtr lpMem);
	}
}
