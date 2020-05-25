using System;
using System.Runtime.InteropServices;
using static BizHawk.BizInvoke.MemoryBlock;

namespace BizHawk.BizInvoke
{
	internal sealed class MemoryBlockWindowsPal : IMemoryBlockPal
	{
		/// <summary>
		/// handle returned by CreateFileMapping
		/// </summary>
		private IntPtr _handle;
		private ulong _start;
		private ulong _size;

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
		}

		public void Deactivate()
		{
			if (!Kernel32.UnmapViewOfFile(Z.US(_start)))
				throw new InvalidOperationException($"{nameof(Kernel32.UnmapViewOfFile)}() returned NULL");
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
				GC.SuppressFinalize(this);
			}
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
		}
	}
}
