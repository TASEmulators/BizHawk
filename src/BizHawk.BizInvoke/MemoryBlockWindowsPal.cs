using System;
using System.Runtime.InteropServices;
using static BizHawk.BizInvoke.MemoryBlock;

namespace BizHawk.BizInvoke
{
	internal sealed unsafe class MemoryBlockWindowsPal : IMemoryBlockPal
	{
		/// <summary>
		/// handle returned by CreateFileMapping
		/// </summary>
		private IntPtr _handle;
		private ulong _start;
		private ulong _size;
		private bool _guardActive;

		/// <summary>
		/// Reserve bytes to later be swapped in, but do not map them
		/// </summary>
		/// <param name="start">eventual mapped address</param>
		/// <param name="size"></param>
		public MemoryBlockWindowsPal(ulong start, ulong size)
		{
			_start = start;
			_size = size;
			_handle = Kernel32.CreateFileMapping(
				Kernel32.INVALID_HANDLE_VALUE,
				IntPtr.Zero,
				Kernel32.FileMapProtection.PageExecuteReadWrite | Kernel32.FileMapProtection.SectionReserve,
				(uint)(_size >> 32),
				(uint)_size,
				null
			);
			if (_handle == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(Kernel32.CreateFileMapping)}() returned NULL");
			}
		}

		public void Activate()
		{
			if (Kernel32.MapViewOfFileEx(
					_handle,
					Kernel32.FileMapAccessType.Read | Kernel32.FileMapAccessType.Write | Kernel32.FileMapAccessType.Execute,
					0,
					0,
					Z.UU(_size),
					Z.US(_start)
				) != Z.US(_start))
			{
				throw new InvalidOperationException($"{nameof(Kernel32.MapViewOfFileEx)}() returned NULL");
			}
			if ((IntPtr)WinGuard.AddTripGuard(Z.UU(_start), Z.UU(_size)) == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(WinGuard.AddTripGuard)}() returned NULL");
			}
			_guardActive = true;
		}

		public void Deactivate()
		{
			if (!Kernel32.UnmapViewOfFile(Z.US(_start)))
				throw new InvalidOperationException($"{nameof(Kernel32.UnmapViewOfFile)}() returned NULL");
			if (!WinGuard.RemoveTripGuard(Z.UU(_start), Z.UU(_size)))
				throw new InvalidOperationException($"{nameof(WinGuard.RemoveTripGuard)}() returned FALSE");
			_guardActive = false;
		}

		public void Protect(ulong start, ulong size, Protection prot)
		{
			if (!Kernel32.VirtualProtect(Z.UU(start), Z.UU(size), GetKernelMemoryProtectionValue(prot), out var old))
				throw new InvalidOperationException($"{nameof(Kernel32.VirtualProtect)}() returned FALSE!");
		}

		public void Commit(ulong length)
		{
			if (Kernel32.VirtualAlloc(Z.UU(_start), Z.UU(length), Kernel32.AllocationType.MEM_COMMIT, Kernel32.MemoryProtection.READWRITE) != Z.UU(_start))
				throw new InvalidOperationException($"{nameof(Kernel32.VirtualAlloc)}() returned NULL!");
		}

		private static Kernel32.MemoryProtection GetKernelMemoryProtectionValue(Protection prot)
		{
			Kernel32.MemoryProtection p;
			switch (prot)
			{
				case Protection.None: p = Kernel32.MemoryProtection.NOACCESS; break;
				case Protection.R: p = Kernel32.MemoryProtection.READONLY; break;
				case Protection.RW: p = Kernel32.MemoryProtection.READWRITE; break;
				case Protection.RX: p = Kernel32.MemoryProtection.EXECUTE_READ; break;
				// VEH can't work when the stack is not writable, because VEH is delivered on that selfsame stack.  The kernel
				// simply declines to return to user mode on a first chance exception if the stack is not writable.
				// So we use guard pages instead, which are much worse because reads set them off as well, but it doesn't matter
				// in this case.
				case Protection.RW_Stack: p = Kernel32.MemoryProtection.READWRITE | Kernel32.MemoryProtection.GUARD_Modifierflag; break;
				default: throw new ArgumentOutOfRangeException(nameof(prot));
			}
			return p;
		}

		public void Dispose()
		{
			if (_handle != IntPtr.Zero)
			{
				Kernel32.CloseHandle(_handle);
				_handle = IntPtr.Zero;
				if (_guardActive)
				{
					WinGuard.RemoveTripGuard(Z.UU(_start), Z.UU(_size));
					_guardActive = false;
				}
				GC.SuppressFinalize(this);
			}
		}

		public void GetWriteStatus(WriteDetectionStatus[] dest, Protection[] pagedata)
		{
			var p = (IntPtr)WinGuard.ExamineTripGuard(Z.UU(_start), Z.UU(_size));
			if (p == IntPtr.Zero)
				throw new InvalidOperationException($"{nameof(WinGuard.ExamineTripGuard)}() returned NULL!");
			Marshal.Copy(p, (byte[])(object)dest, 0, dest.Length);

			// guard pages do not trigger VEH when they are actually being used as a stack and there are other
			// free pages below them, so virtualquery to get that information out
			Kernel32.MEMORY_BASIC_INFORMATION mbi;
			for (int i = 0; i < pagedata.Length;)
			{
				if (pagedata[i] == Protection.RW_Stack)
				{
					var q = ((ulong)i << WaterboxUtils.PageShift) + _start;
					Kernel32.VirtualQuery(Z.UU(q), &mbi, Z.SU(sizeof(Kernel32.MEMORY_BASIC_INFORMATION)));
					var pstart = (int)(((ulong)mbi.BaseAddress - _start) >> WaterboxUtils.PageShift);
					var pend = pstart + (int)((ulong)mbi.RegionSize >> WaterboxUtils.PageShift);
					if (pstart != i)
						throw new Exception("Huh?");

					if ((mbi.Protect & Kernel32.MemoryProtection.GUARD_Modifierflag) == 0)
					{
						// tripped!
						for (int j = pstart; j < pend; j++)
							dest[j] |= WriteDetectionStatus.DidChange;
					}
					i = pend;
				}
				else
				{
					i++;
				}
			}

		}

		public void SetWriteStatus(WriteDetectionStatus[] src)
		{
			var p = (IntPtr)WinGuard.ExamineTripGuard(Z.UU(_start), Z.UU(_size));
			if (p == IntPtr.Zero)
				throw new InvalidOperationException($"{nameof(WinGuard.ExamineTripGuard)}() returned NULL!");
			Marshal.Copy((byte[])(object)src, 0, p, src.Length);
		}

		~MemoryBlockWindowsPal()
		{
			Dispose();
		}

		private static class Kernel32
		{
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern UIntPtr VirtualAlloc(UIntPtr lpAddress, UIntPtr dwSize,
				AllocationType flAllocationType, MemoryProtection flProtect);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool VirtualProtect(UIntPtr lpAddress, UIntPtr dwSize,
			   MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

			[Flags]
			public enum AllocationType : uint
			{
				MEM_COMMIT = 0x00001000,
				MEM_RESERVE = 0x00002000,
				MEM_RESET = 0x00080000,
				MEM_RESET_UNDO = 0x1000000,
				MEM_LARGE_PAGES = 0x20000000,
				MEM_PHYSICAL = 0x00400000,
				MEM_TOP_DOWN = 0x00100000,
				MEM_WRITE_WATCH = 0x00200000
			}

			[Flags]
			public enum MemoryProtection : uint
			{
				EXECUTE = 0x10,
				EXECUTE_READ = 0x20,
				EXECUTE_READWRITE = 0x40,
				EXECUTE_WRITECOPY = 0x80,
				NOACCESS = 0x01,
				READONLY = 0x02,
				READWRITE = 0x04,
				WRITECOPY = 0x08,
				GUARD_Modifierflag = 0x100,
				NOCACHE_Modifierflag = 0x200,
				WRITECOMBINE_Modifierflag = 0x400
			}

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr CreateFileMapping(
				IntPtr hFile,
				IntPtr lpFileMappingAttributes,
				FileMapProtection flProtect,
				uint dwMaximumSizeHigh,
				uint dwMaximumSizeLow,
				string lpName);

			[Flags]
			public enum FileMapProtection : uint
			{
				PageReadonly = 0x02,
				PageReadWrite = 0x04,
				PageWriteCopy = 0x08,
				PageExecuteRead = 0x20,
				PageExecuteReadWrite = 0x40,
				SectionCommit = 0x8000000,
				SectionImage = 0x1000000,
				SectionNoCache = 0x10000000,
				SectionReserve = 0x4000000,
			}

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool CloseHandle(IntPtr hObject);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

			[DllImport("kernel32.dll")]
			public static extern IntPtr MapViewOfFileEx(IntPtr hFileMappingObject,
			   FileMapAccessType dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow,
			   UIntPtr dwNumberOfBytesToMap, IntPtr lpBaseAddress);

			[Flags]
			public enum FileMapAccessType : uint
			{
				Copy = 0x01,
				Write = 0x02,
				Read = 0x04,
				AllAccess = 0x08,
				Execute = 0x20,
			}

			public static readonly IntPtr INVALID_HANDLE_VALUE = Z.US(0xffffffffffffffff);

			[StructLayout(LayoutKind.Sequential)]
			public struct MEMORY_BASIC_INFORMATION
			{
				public IntPtr BaseAddress;
				public IntPtr AllocationBase;
				public MemoryProtection AllocationProtect;
				public UIntPtr RegionSize;
				public StateEnum State;
				public MemoryProtection Protect;
				public TypeEnum Type;
			}
			public enum StateEnum : uint
			{
				MEM_COMMIT = 0x1000,
				MEM_FREE = 0x10000,
				MEM_RESERVE = 0x2000
			}

			public enum TypeEnum : uint
			{
				MEM_IMAGE = 0x1000000,
				MEM_MAPPED = 0x40000,
				MEM_PRIVATE = 0x20000
			}

			[DllImport("kernel32.dll")]
			public static extern UIntPtr VirtualQuery(UIntPtr lpAddress, MEMORY_BASIC_INFORMATION* lpBuffer, UIntPtr dwLength);
		}

		private static unsafe class WinGuard
		{
			/// <summary>
			/// Add write detection to an area of memory.  Any page in the specified range that has CanChange
			/// set and triggers an access violation on write
			/// will be noted, set to read+write permissions, and execution will be continued.
			/// CALLER'S RESPONSIBILITY: All addresses are page aligned.
			/// CALLER'S RESPONSIBILITY: No other thread enters any WinGuard function, or trips any tracked page during this call.
			/// CALLER'S RESPONSIBILITY: Pages to be tracked are VirtualProtected to R (no G) beforehand.  Pages with write permission
			/// cause no issues, but they will not trip.  WinGuard will not intercept Guard flag exceptions in any way.
			/// </summary>
			/// <returns>The same information as ExamineTripGuard, or null on failure</returns>
			[DllImport("winguard.dll")]
			public static extern WriteDetectionStatus* AddTripGuard(UIntPtr start, UIntPtr length);
			/// <summary>
			/// Remove write detection from the specified addresses.
			/// CALLER'S RESPONSIBILITY: All addresses are page aligned.
			/// CALLER'S RESPONSIBILITY: No other thread enters any WinGuard function, or trips any tracked guard page during this call.
			/// </summary>
			/// <returns>false on failure (usually, the address range did not match a known one)</returns>
			[DllImport("winguard.dll")]
			public static extern bool RemoveTripGuard(UIntPtr start, UIntPtr length);
			/// <summary>
			/// Examines a previously installed guard page detection.
			/// CALLER'S RESPONSIBILITY: All addresses are page aligned.
			/// CALLER'S RESPONSIBILITY: No other thread enters any WinGuard function, or trips any tracked guard page during this call.
			/// </summary>
			/// <returns>
			/// A pointer to an array of bytes, one byte for each memory page in the range.  Caller should set CanChange on pages to
			/// observe, and read back DidChange to see if things changed.
			/// </returns>
			[DllImport("winguard.dll")]
			public static extern WriteDetectionStatus* ExamineTripGuard(UIntPtr start, UIntPtr length);
		}
	}
}
