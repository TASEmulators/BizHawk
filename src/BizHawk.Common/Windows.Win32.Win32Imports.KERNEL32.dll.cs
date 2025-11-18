using System.Runtime.InteropServices;

using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;

namespace Windows.Win32
{
	public static partial class Win32Imports
	{
		/// <inheritdoc cref="GetShortPathNameW(PCWSTR, PWSTR, uint)"/>
		public static unsafe uint GetShortPathNameW(string lpszLongPath)
		{
			fixed (char* lpszLongPathLocal = lpszLongPath)
			{
				return Win32Imports.GetShortPathNameW(lpszLongPathLocal, default, 0U);
			}
		}

		/// <seealso cref="HeapAlloc(SafeHandle, HEAP_FLAGS, nuint)"/>
		public static unsafe IntPtr HeapAlloc(int dwBytes, HEAP_FLAGS dwFlags = HEAP_FLAGS.HEAP_NONE)
			=> unchecked((IntPtr) HeapAlloc(GetProcessHeap_SafeHandle(), dwFlags, dwBytes: (UIntPtr) dwBytes));

		/// <inheritdoc cref="IsWow64Process(HANDLE, BOOL*)"/>
		public static unsafe BOOL IsWow64Process(HANDLE hProcess, out BOOL Wow64Process)
		{
			fixed (BOOL* ptr = &Wow64Process) return IsWow64Process(hProcess, ptr);
		}

		/// <inheritdoc cref="VirtualAlloc(void*, nuint, VIRTUAL_ALLOCATION_TYPE, PAGE_PROTECTION_FLAGS)"/>
		public static unsafe UIntPtr VirtualAlloc(
			UIntPtr lpAddress,
			UIntPtr dwSize,
			VIRTUAL_ALLOCATION_TYPE flAllocationType,
			PAGE_PROTECTION_FLAGS flProtect)
				=> unchecked((UIntPtr) VirtualAlloc(
					lpAddress: (void*) lpAddress,
					dwSize: dwSize,
					flAllocationType,
					flProtect));

		/// <inheritdoc cref="VirtualFree(void*, nuint, VIRTUAL_FREE_TYPE)"/>
		public static unsafe BOOL VirtualFree(
			UIntPtr lpAddress,
			UIntPtr dwSize,
			VIRTUAL_FREE_TYPE dwFreeType)
				=> unchecked(VirtualFree((void*) lpAddress, (nuint) dwSize, dwFreeType));

		/// <inheritdoc cref="VirtualProtect(void*, nuint, PAGE_PROTECTION_FLAGS, out PAGE_PROTECTION_FLAGS)"/>
		public static unsafe BOOL VirtualProtect(
			UIntPtr lpAddress,
			UIntPtr dwSize,
			PAGE_PROTECTION_FLAGS flNewProtect,
			out PAGE_PROTECTION_FLAGS lpflOldProtect)
				=> unchecked(VirtualProtect((void*) lpAddress, (nuint) dwSize, flNewProtect, out lpflOldProtect));
	}
}
