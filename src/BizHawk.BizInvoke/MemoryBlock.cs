using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.BizInvoke
{
	public class MemoryBlock : IDisposable /*, IBinaryStateable */
	{
		/// <summary>allocate <paramref name="size"/> bytes starting at a particular address <paramref name="start"/></summary>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is not aligned or <paramref name="size"/> is <c>0</c></exception>
		public MemoryBlock(ulong start, ulong size)
		{
			if (!WaterboxUtils.Aligned(start))
				throw new ArgumentOutOfRangeException(nameof(start), start, "start address must be aligned");
			if (size == 0)
				throw new ArgumentOutOfRangeException(nameof(size), size, "cannot create 0-length block");
			if (start == 0)
				throw new NotImplementedException("Start == 0 doesn't work right now, not really");
			Start = start;
			Size = WaterboxUtils.AlignUp(size);
			EndExclusive = Start + Size;
			_pageData = (Protection[])(object)new byte[GetPage(EndExclusive - 1) + 1];

			_pal = OSTailoredCode.IsUnixHost
				? (IMemoryBlockPal)new MemoryBlockLinuxPal(Start, Size)
				: new MemoryBlockWindowsPal(Start, Size);
		}

		private IMemoryBlockPal _pal;

		/// <summary>
		/// Size that has been committed to actual underlying RAM.  Never shrinks.  Private because
		/// it should be transparent to the caller.  ALWAYS ALIGNED.
		/// </summary>
		private ulong CommittedSize;

		/// <summary>stores last set memory protection value for each page</summary>
		private Protection[] _pageData;

		/// <summary>
		/// end address of the memory block (not part of the block; class invariant: equal to <see cref="Start"/> + <see cref="Size"/>)
		/// </summary>
		public readonly ulong EndExclusive;

		/// <summary>total size of the memory block</summary>
		public readonly ulong Size;

		/// <summary>starting address of the memory block</summary>
		public readonly ulong Start;

		/// <summary>true if this is currently swapped in</summary>
		public bool Active { get; private set; }

		/// <summary>get a page index within the block</summary>
		private int GetPage(ulong addr)
		{
			if (addr < Start || addr >= EndExclusive)
				throw new ArgumentOutOfRangeException(nameof(addr), addr, "invalid address");
			return (int) ((addr - Start) >> WaterboxUtils.PageShift);
		}

		/// <summary>get a start address for a page index within the block</summary>
		private ulong GetStartAddr(int page) => ((ulong) page << WaterboxUtils.PageShift) + Start;

		private void EnsureActive()
		{
			if (!Active)
				throw new InvalidOperationException("MemoryBlock is not currently active");
		}

		/// <summary>
		/// Get a stream that can be used to read or write from part of the block. Does not check for or change <see cref="Protect"/>!
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="start"/> or end (= <paramref name="start"/> + <paramref name="length"/> - <c>1</c>)
		/// are outside [<see cref="Start"/>, <see cref="EndExclusive"/>), the range of the block
		/// </exception>
		public Stream GetStream(ulong start, ulong length, bool writer)
		{
			if (start < Start)
				throw new ArgumentOutOfRangeException(nameof(start), start, "invalid address");
			if (EndExclusive < start + length)
				throw new ArgumentOutOfRangeException(nameof(length), length, "requested length implies invalid end address");
			return new MemoryViewStream(!writer, writer, (long)start, (long)length);
		}

		/// <summary>activate the memory block, swapping it in at the pre-specified address</summary>
		/// <exception cref="InvalidOperationException"><see cref="MemoryBlock.Active"/> is <see langword="true"/> or failed to map file view</exception>
		public void Activate()
		{
			if (Active)
				throw new InvalidOperationException("Already active");
			_pal.Activate();
			ProtectAll();
			Active = true;
		}

		/// <summary>deactivate the memory block, removing it from RAM but leaving it immediately available to swap back in</summary>
		/// <exception cref="InvalidOperationException">
		/// <see cref="MemoryBlock.Active"/> is <see langword="false"/> or failed to unmap file view
		/// </exception>
		public void Deactivate()
		{
			EnsureActive();
			_pal.Deactivate();
			Active = false;
		}

		/// <summary>set r/w/x protection on a portion of memory. rounded to encompassing pages</summary>
		/// <exception cref="InvalidOperationException">failed to protect memory</exception>
		public void Protect(ulong start, ulong length, Protection prot)
		{
			EnsureActive();
			if (length == 0)
				return;

			// Note: asking for prot.none on memory that was not previously committed, commits it

			var computedStart = WaterboxUtils.AlignDown(start);
			var computedEnd = WaterboxUtils.AlignUp(start + length);
			var computedLength = computedEnd - computedStart;

			// potentially commit more memory
			var minNewCommittedSize = computedEnd - Start;
			if (minNewCommittedSize > CommittedSize)
			{
				CommittedSize = minNewCommittedSize;
				// Since Commit() was called, we have to do a full ProtectAll -- remember that when refactoring
				_pal.Commit(CommittedSize);
			}

			int pstart = GetPage(start);
			int pend = GetPage(start + length - 1);
			for (int i = pstart; i <= pend; i++)
			{
				_pageData[i] = prot;
			}

			// TODO: restore the previous behavior where we would only reprotect a partial range
			ProtectAll();
		}

		/// <summary>restore all recorded protections</summary>
		private void ProtectAll()
		{
			if (CommittedSize == 0)
				return;
			int ps = 0;
			int pageLimit = (int)(CommittedSize >> WaterboxUtils.PageShift);
			for (int i = 0; i < pageLimit; i++)
			{
				if (i == pageLimit - 1 || _pageData[i] != _pageData[i + 1])
				{
					ulong zstart = GetStartAddr(ps);
					ulong zend = GetStartAddr(i + 1);
					var prot = _pageData[i];
					_pal.Protect(zstart, zend - zstart, prot);
					ps = i + 1;
				}
			}
		}

		public void Dispose()
		{
			if (_pal != null)
			{
				_pal.Dispose();
				_pal = null;
			}
		}

		/// <summary>allocate <paramref name="size"/> bytes starting at a particular address <paramref name="start"/></summary>
		public static MemoryBlock Create(ulong start, ulong size) => new MemoryBlock(start, size);

		/// <summary>allocate <paramref name="size"/> bytes at any address</summary>
		public static MemoryBlock Create(ulong size) => Create(0, size);

		/// <summary>Memory protection constant</summary>
		public enum Protection : byte
		{
			None,
			R,
			RW,
			RX,
		}
	}
}
