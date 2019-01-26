using System;
using System.Runtime.InteropServices;
using System.IO;

namespace BizHawk.Common.BizInvoke
{
	public sealed class MemoryBlockWin32 : MemoryBlock
	{
		/// <summary>
		/// handle returned by CreateFileMapping
		/// </summary>
		private IntPtr _handle;

		/// <summary>
		/// allocate size bytes starting at a particular address
		/// </summary>
		/// <param name="start"></param>
		/// <param name="size"></param>
		public MemoryBlockWin32(ulong start, ulong size)
		{
			if (!WaterboxUtils.Aligned(start)) throw new ArgumentOutOfRangeException();
			if (size == 0) throw new ArgumentOutOfRangeException();
			size = WaterboxUtils.AlignUp(size);

			_handle = Kernel32.CreateFileMapping(Kernel32.INVALID_HANDLE_VALUE, IntPtr.Zero, Kernel32.FileMapProtection.PageExecuteReadWrite | Kernel32.FileMapProtection.SectionCommit, (uint) (size >> 32), (uint) size, null);
			if (_handle == IntPtr.Zero) throw new InvalidOperationException("CreateFileMapping() returned NULL");
			Start = start;
			End = start + size;
			Size = size;
			_pageData = new Protection[GetPage(End - 1) + 1];
		}

		/// <summary>
		/// activate the memory block, swapping it in at the specified address
		/// </summary>
		public override void Activate()
		{
			if (Active) throw new InvalidOperationException("Already active");
			if (Kernel32.MapViewOfFileEx(_handle, Kernel32.FileMapAccessType.Read | Kernel32.FileMapAccessType.Write | Kernel32.FileMapAccessType.Execute, 0, 0, Z.UU(Size), Z.US(Start)) != Z.US(Start))
				throw new InvalidOperationException("MapViewOfFileEx() returned NULL");
			ProtectAll();
			Active = true;
		}

		/// <summary>
		/// deactivate the memory block, removing it from RAM but leaving it immediately available to swap back in
		/// </summary>
		public override void Deactivate()
		{
			if (!Active) throw new InvalidOperationException("Not active");
			if (!Kernel32.UnmapViewOfFile(Z.US(Start))) throw new InvalidOperationException("UnmapViewOfFile() returned NULL");
			Active = false;
		}

		/// <summary>
		/// take a snapshot of the entire memory block's contents, for use in GetXorStream
		/// </summary>
		public override void SaveXorSnapshot()
		{
			if (_snapshot != null) throw new InvalidOperationException("Snapshot already taken");
			if (!Active) throw new InvalidOperationException("Not active");

			// temporarily switch the entire block to `R`: in case some areas are unreadable, we don't want
			// that to complicate things
			Kernel32.MemoryProtection old;
			if (!Kernel32.VirtualProtect(Z.UU(Start), Z.UU(Size), Kernel32.MemoryProtection.READONLY, out old))
				throw new InvalidOperationException("VirtualProtect() returned FALSE!");

			_snapshot = new byte[Size];
			var ds = new MemoryStream(_snapshot, true);
			var ss = GetStream(Start, Size, false);
			ss.CopyTo(ds);
			XorHash = WaterboxUtils.Hash(_snapshot);

			ProtectAll();
		}

		/// <summary>
		/// take a hash of the current full contents of the block, including unreadable areas
		/// </summary>
		/// <returns></returns>
		public override byte[] FullHash()
		{
			if (!Active) throw new InvalidOperationException("Not active");
			// temporarily switch the entire block to `R`
			Kernel32.MemoryProtection old;
			if (!Kernel32.VirtualProtect(Z.UU(Start), Z.UU(Size), Kernel32.MemoryProtection.READONLY, out old))
				throw new InvalidOperationException("VirtualProtect() returned FALSE!");
			var ret = WaterboxUtils.Hash(GetStream(Start, Size, false));
			ProtectAll();
			return ret;
		}

		private static Kernel32.MemoryProtection GetKernelMemoryProtectionValue(Protection prot)
		{
			switch (prot)
			{
				case Protection.None: return Kernel32.MemoryProtection.NOACCESS;
				case Protection.R: return Kernel32.MemoryProtection.READONLY;
				case Protection.RW: return Kernel32.MemoryProtection.READWRITE;
				case Protection.RX: return Kernel32.MemoryProtection.EXECUTE_READ;
			}
			throw new ArgumentOutOfRangeException(nameof(prot));
		}

		/// <summary>
		/// restore all recorded protections
		/// </summary>
		protected override void ProtectAll()
		{
			int ps = 0;
			for (int i = 0; i < _pageData.Length; i++)
			{
				if (i == _pageData.Length - 1 || _pageData[i] != _pageData[i + 1])
				{
					var p = GetKernelMemoryProtectionValue(_pageData[i]);
					ulong zstart = GetStartAddr(ps);
					ulong zend = GetStartAddr(i + 1);
					Kernel32.MemoryProtection old;
					if (!Kernel32.VirtualProtect(Z.UU(zstart), Z.UU(zend - zstart), p, out old))
						throw new InvalidOperationException("VirtualProtect() returned FALSE!");
					ps = i + 1;
				}
			}
		}

		/// <summary>
		/// set r/w/x protection on a portion of memory.  rounded to encompassing pages
		/// </summary>
		public override void Protect(ulong start, ulong length, Protection prot)
		{
			if (length == 0) return;
			int pstart = GetPage(start);
			int pend = GetPage(start + length - 1);

			var p = GetKernelMemoryProtectionValue(prot);
			for (int i = pstart; i <= pend; i++) _pageData[i] = prot; // also store the value for later use

			if (Active) // it's legal to Protect() if we're not active; the information is just saved for the next activation
			{
				var computedStart = WaterboxUtils.AlignDown(start);
				var computedEnd = WaterboxUtils.AlignUp(start + length);
				var computedLength = computedEnd - computedStart;

				Kernel32.MemoryProtection old;
				if (!Kernel32.VirtualProtect(Z.UU(computedStart), Z.UU(computedLength), p, out old))
					throw new InvalidOperationException("VirtualProtect() returned FALSE!");
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (_handle != IntPtr.Zero)
			{
				if (Active) Deactivate();
				Kernel32.CloseHandle(_handle);
				_handle = IntPtr.Zero;
			}
		}

		~MemoryBlockWin32()
		{
			Dispose(false);
		}

		private static class Kernel32
		{
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern bool VirtualProtect(UIntPtr lpAddress, UIntPtr dwSize,
			   MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

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

			public static readonly IntPtr INVALID_HANDLE_VALUE = Z.US(0xFFFFFFFFFFFFFFFF);
		}
	}
}
