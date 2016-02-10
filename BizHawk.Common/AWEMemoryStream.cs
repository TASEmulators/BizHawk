using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>
	/// A MemoryStream that uses AWE to achieve a large addressable area, without being subject to 32 bit address space limits.
	/// </summary>
	public class AWEMemoryStream : Stream
	{
		const int kBlockSizeBits = 20;
		const int kBlockSize = 1 << 20;

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool VirtualFree(IntPtr lpAddress, IntPtr dwSize, uint dwFreeType);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr VirtualAlloc(IntPtr lpAddress, IntPtr dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

		[Flags]
		enum MemoryProtection : uint
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

		[Flags]
		enum AllocationType : uint
		{
			COMMIT = 0x1000,
			RESERVE = 0x2000,
			RESET = 0x80000,
			LARGE_PAGES = 0x20000000,
			PHYSICAL = 0x400000,
			TOP_DOWN = 0x100000,
			WRITE_WATCH = 0x200000
		}

		public AWEMemoryStream()
		{
			//bootstrap the datastructures
			Position = 0;

			//allocate the window (address space that we'll allocate physical pages into)
			mWindow = VirtualAlloc(IntPtr.Zero, new IntPtr(kBlockSize), AllocationType.RESERVE | AllocationType.PHYSICAL, MemoryProtection.READWRITE);
		}

		protected override void Dispose(bool disposing)
		{
			if (mWindow != IntPtr.Zero)
			{
				VirtualFree(mWindow, IntPtr.Zero, 0x8000U); //MEM_RELEASE
				mWindow = IntPtr.Zero;
			}

			if (disposing)
			{
				foreach (var block in mBlocks)
					block.Dispose();
			}
		}

		~AWEMemoryStream()
		{
			Dispose(false);
		}

		long mLength = 0, mPosition = -1;
		long mCurrBlock = -1;
		List<AWEMemoryBlock> mBlocks = new List<AWEMemoryBlock>();
		IntPtr mWindow;

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return true; } }
		public override bool CanWrite { get { return true; } }
		public override void Flush() { }
		public override long Length { get { return mLength; } }
		public override long Position
		{
			get
			{
				return mPosition;
			}
			set
			{
				if (!Ensure(value + 1))
					throw new OutOfMemoryException("Couldn't set AWEMemoryStream to specified Position");
				mPosition = value;
			}
		}


		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin: Position = offset; return Position;
				case SeekOrigin.Current: Position += offset; return Position;
				case SeekOrigin.End: Position = Length + offset; return Position;
				default: throw new InvalidOperationException();
			}
		}

		bool Ensure(long len)
		{
			long blocksNeeded = (len + kBlockSize - 1) >> kBlockSizeBits;
			while (mBlocks.Count < blocksNeeded)
			{
				var block = new AWEMemoryBlock();
				if (!block.Allocate(kBlockSize))
					return false;
				mBlocks.Add(block);
			}
			return true;
		}

		public override void SetLength(long value)
		{
			if (!Ensure(value))
				throw new OutOfMemoryException("Couldn't set AWEMemoryStream to specified Length");
			mLength = value;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count + mPosition > mLength)
				count = (int)(mLength - mPosition);
			int ocount = count;
			while (count > 0)
			{
				int todo = count;
				long lblock = mPosition >> kBlockSizeBits;
				if (lblock > int.MaxValue) throw new ArgumentOutOfRangeException();
				int block = (int)lblock;
				int blockOfs = (int)(mPosition - (block << kBlockSizeBits));
				int remainsInBlock = kBlockSize - blockOfs;
				if (remainsInBlock < todo)
					todo = remainsInBlock;
				if (mCurrBlock != block)
				{
					mCurrBlock = block;
					if (!mBlocks[block].Map(mWindow))
						throw new Exception("Couldn't map required memory for AWEMemoryStream.Write");
				}
				Marshal.Copy(IntPtr.Add(mWindow, blockOfs), buffer, offset, todo);
				count -= todo;
				mPosition += todo;
				offset += todo;
			}
			return ocount - count;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			long end = mPosition + count;
			if (!Ensure(end))
				throw new OutOfMemoryException("Couldn't reserve required resources for AWEMemoryStream.Write");
			SetLength(end);
			while (count > 0)
			{
				int todo = count;
				long lblock = mPosition >> kBlockSizeBits;
				if (lblock > int.MaxValue) throw new ArgumentOutOfRangeException();
				int block = (int)lblock;
				int blockOfs = (int)(mPosition - (block << kBlockSizeBits));
				int remainsInBlock = kBlockSize - blockOfs;
				if (remainsInBlock < todo)
					todo = remainsInBlock;
				if (mCurrBlock != block)
				{
					mCurrBlock = block;
					if (!mBlocks[block].Map(mWindow))
						throw new Exception("Couldn't map required memory for AWEMemoryStream.Write");
				}
				Marshal.Copy(buffer, offset, IntPtr.Add(mWindow, blockOfs), todo);
				count -= todo;
				mPosition += todo;
				offset += todo;
			}
		}

		unsafe class AWEMemoryBlock : IDisposable
		{
			[DllImport("kernel32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			static extern bool AllocateUserPhysicalPages(IntPtr hProcess, ref uint NumberOfPages, IntPtr PageArray);

			[DllImport("kernel32.dll")]
			static extern bool MapUserPhysicalPages(IntPtr lpAddress, uint NumberOfPages, IntPtr UserPfnArray);

			[DllImport("kernel32.dll")]
			static extern bool FreeUserPhysicalPages(IntPtr hProcess, ref uint NumberOfPages, IntPtr UserPfnArray);

			public enum ProcessorArchitecture
			{
				X86 = 0,
				X64 = 9,
				Arm = -1,
				Itanium = 6,
				Unknown = 0xFFFF,
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct SystemInfo
			{
				public ProcessorArchitecture ProcessorArchitecture; // WORD
				public uint PageSize; // DWORD
				public IntPtr MinimumApplicationAddress; // (long)void*
				public IntPtr MaximumApplicationAddress; // (long)void*
				public IntPtr ActiveProcessorMask; // DWORD*
				public uint NumberOfProcessors; // DWORD (WTF)
				public uint ProcessorType; // DWORD
				public uint AllocationGranularity; // DWORD
				public ushort ProcessorLevel; // WORD
				public ushort ProcessorRevision; // WORD
			}

			[DllImport("kernel32", SetLastError = true)]
			public static extern void GetSystemInfo(out SystemInfo lpSystemInfo);

			[StructLayout(LayoutKind.Sequential)]
			public struct LUID
			{
				private uint lp;
				private int hp;

				public uint LowPart
				{
					get { return lp; }
					set { lp = value; }
				}

				public int HighPart
				{
					get { return hp; }
					set { hp = value; }
				}
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct LUID_AND_ATTRIBUTES
			{
				private LUID luid;
				private uint attributes;

				public LUID LUID
				{
					get { return luid; }
					set { luid = value; }
				}

				public uint Attributes
				{
					get { return attributes; }
					set { attributes = value; }
				}
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct TOKEN_PRIVILEGES
			{
				private uint prvct;
				[MarshalAs(UnmanagedType.ByValArray)] //edited from stackoverflow article
				private LUID_AND_ATTRIBUTES[] privileges;

				public uint PrivilegeCount
				{
					get { return prvct; }
					set { prvct = value; }
				}

				public LUID_AND_ATTRIBUTES[] Privileges
				{
					get { return privileges; }
					set { privileges = value; }
				}
			}

			[DllImport("advapi32", SetLastError = true)]
			public static extern bool OpenProcessToken(IntPtr ProcessHandle, TokenAccessLevels DesiredAccess, out IntPtr TokenHandle);

			[DllImport("advapi32.dll", SetLastError = true)]
			public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, out TOKEN_PRIVILEGES PreviousState, out uint ReturnLength);

			[Flags]
			internal enum TokenAccessLevels
			{
				AssignPrimary = 0x00000001,
				Duplicate = 0x00000002,
				Impersonate = 0x00000004,
				Query = 0x00000008,
				QuerySource = 0x00000010,
				AdjustPrivileges = 0x00000020,
				AdjustGroups = 0x00000040,
				AdjustDefault = 0x00000080,
				AdjustSessionId = 0x00000100,

				Read = 0x00020000 | Query,

				Write = 0x00020000 | AdjustPrivileges | AdjustGroups | AdjustDefault,

				AllAccess = 0x000F0000 |
						AssignPrimary |
						Duplicate |
						Impersonate |
						Query |
						QuerySource |
						AdjustPrivileges |
						AdjustGroups |
						AdjustDefault |
						AdjustSessionId,

				MaximumAllowed = 0x02000000
			}

			//http://stackoverflow.com/questions/13616330/c-sharp-adjusttokenprivileges-not-working-on-32bit
			[DllImport("advapi32.dll", SetLastError = true)]
			public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);
			public static bool EnableDisablePrivilege(string PrivilegeName, bool EnableDisable)
			{
				var htok = IntPtr.Zero;
				if (!OpenProcessToken(System.Diagnostics.Process.GetCurrentProcess().Handle, TokenAccessLevels.AdjustPrivileges | TokenAccessLevels.Query, out htok))
				{
					Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
					return false;
				}
				var tkp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Privileges = new LUID_AND_ATTRIBUTES[1] };
				LUID luid;
				if (!LookupPrivilegeValue(null, PrivilegeName, out luid))
				{
					Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
					return false;
				}
				tkp.Privileges[0].LUID = luid;
				tkp.Privileges[0].Attributes = (uint)(EnableDisable ? 2 : 0);
				TOKEN_PRIVILEGES prv;
				uint rb;
				if (!AdjustTokenPrivileges(htok, false, ref tkp, 256, out prv, out rb))
				{
					Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
					return false;
				}

				return true;
			}

			static AWEMemoryBlock()
			{
				var si = new SystemInfo();
				GetSystemInfo(out si);
				PageSize = si.PageSize;
			}

			static uint PageSize;
			static bool PrivilegeAcquired;
			static object StaticLock = new object();

			byte[] pageList;

			static bool TryAcquirePrivilege()
			{
				lock (StaticLock)
				{
					if (PrivilegeAcquired)
						return true;
					if (EnableDisablePrivilege("SeLockMemoryPrivilege", true))
					{
						PrivilegeAcquired = true;
						return true;
					}
					else return false;
				}
			}

			public bool Allocate(int byteSize)
			{
				if (!TryAcquirePrivilege())
					return false;

				long lnPagesRequested = byteSize / PageSize;
				if (lnPagesRequested > uint.MaxValue)
					throw new InvalidOperationException();
				uint nPagesRequested = (uint)lnPagesRequested;

				long sizePageList = IntPtr.Size * nPagesRequested;
				pageList = new byte[sizePageList];

				fixed (byte* pPageList = &pageList[0])
				{
					uint nPagesAllocated = nPagesRequested;
					bool bResult = AllocateUserPhysicalPages(System.Diagnostics.Process.GetCurrentProcess().Handle, ref nPagesAllocated, new IntPtr(pPageList));
					if (nPagesRequested != nPagesAllocated)
					{
						//abort! we're probably about to bomb the process, but just in case, we'll clean up
						FreeUserPhysicalPages(System.Diagnostics.Process.GetCurrentProcess().Handle, ref nPagesAllocated, new IntPtr(pPageList));
						pageList = null;
						return false;
					}
				}

				return true;
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			public bool Map(IntPtr targetWindow)
			{
				//note: unmapping previous mapping seems unnecessary

				if (pageList == null)
					return false;

				//map the desired physical pages 
				fixed (byte* pPageList = &pageList[0])
				{
					bool bResult = MapUserPhysicalPages(targetWindow, NumPages, new IntPtr(pPageList));
					return bResult;
				}
			}

			uint NumPages
			{
				get
				{
					return (uint)(pageList.Length / (uint)IntPtr.Size);
				}
			}

			protected virtual void Dispose(bool disposing)
			{
				if (pageList == null)
					return;

				fixed (byte* pPageList = &pageList[0])
				{
					uint nPagesRequested = NumPages;
					FreeUserPhysicalPages(System.Diagnostics.Process.GetCurrentProcess().Handle, ref nPagesRequested, new IntPtr(pPageList));
					pageList = null;
				}
			}

			~AWEMemoryBlock()
			{
				Dispose(false);
			}

		}
	}
}