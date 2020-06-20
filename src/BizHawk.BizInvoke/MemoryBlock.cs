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
			_dirtydata = (WriteDetectionStatus[])(object)new byte[GetPage(EndExclusive - 1) + 1];

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
		private WriteDetectionStatus[] _dirtydata;

		/// <summary>
		/// end address of the memory block (not part of the block; class invariant: equal to <see cref="Start"/> + <see cref="Size"/>)
		/// </summary>
		public readonly ulong EndExclusive;

		/// <summary>total size of the memory block</summary>
		public readonly ulong Size;

		/// <summary>starting address of the memory block</summary>
		public readonly ulong Start;

		/// <summary>snapshot containing a clean state for all committed pages</summary>
		private byte[] _snapshot;

		private byte[] _hash;

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
		private void EnsureSealed()
		{
			if (!_sealed)
				throw new InvalidOperationException("MemoryBlock is not currently sealed");
		}

		private bool _sealed;

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
			if (_sealed)
				_pal.SetWriteStatus(_dirtydata);
			Active = true;
		}

		/// <summary>deactivate the memory block, removing it from RAM but leaving it immediately available to swap back in</summary>
		/// <exception cref="InvalidOperationException">
		/// <see cref="MemoryBlock.Active"/> is <see langword="false"/> or failed to unmap file view
		/// </exception>
		public void Deactivate()
		{
			EnsureActive();
			if (_sealed)
				_pal.GetWriteStatus(_dirtydata, _pageData);
			_pal.Deactivate();
			Active = false;
		}

		/// <summary>
		/// read the of the current full contents of the block, including unreadable areas
		/// but not uncommitted areas
		/// </summary>
		public byte[] FullHash()
		{
			EnsureSealed();
			return _hash;
		}

		/// <summary>set r/w/x protection on a portion of memory. rounded to encompassing pages</summary>
		/// <exception cref="InvalidOperationException">failed to protect memory</exception>
		public void Protect(ulong start, ulong length, Protection prot)
		{
			EnsureActive();
			if (length == 0)
				return;
			if (_sealed)
				_pal.GetWriteStatus(_dirtydata, _pageData);
			
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
				// inform the low level code what addresses might fault on it
				if (prot == Protection.RW || prot == Protection.RW_Stack)
					_dirtydata[i] |= WriteDetectionStatus.CanChange;
				else
					_dirtydata[i] &= ~WriteDetectionStatus.CanChange;
			}

			// TODO: restore the previous behavior where we would only reprotect a partial range
			ProtectAll();
			if (_sealed)
				_pal.SetWriteStatus(_dirtydata);
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
				if (i == pageLimit - 1 || _pageData[i] != _pageData[i + 1] || _dirtydata[i] != _dirtydata[i + 1])
				{
					ulong zstart = GetStartAddr(ps);
					ulong zend = GetStartAddr(i + 1);
					var prot = _pageData[i];
					// adjust frontend notion of prot to the PAL layer's expectation
					if (prot == Protection.RW_Stack)
					{
						if (!_sealed)
						{
							// don't activate this protection yet
							prot = Protection.RW;
						}
						else
						{
							var didChange = (_dirtydata[i] & WriteDetectionStatus.DidChange) != 0;
							if (didChange)
								// don't needlessly retrigger
								prot = Protection.RW;
						}
					}
					else if (prot == Protection.RW_Invisible)
					{
						// this never matters to the backend
						prot = Protection.RW;
					}
					else if (_sealed && prot == Protection.RW)
					{
						var didChange = (_dirtydata[i] & WriteDetectionStatus.DidChange) != 0;
						if (!didChange)
							// set to trigger if we have not before
							prot = Protection.R;
					}

					_pal.Protect(zstart, zend - zstart, prot);
					ps = i + 1;
				}
			}
		}

		public void Seal()
		{
			EnsureActive();
			if (_sealed)
				throw new InvalidOperationException("Already sealed");
			_snapshot = new byte[CommittedSize];
			if (CommittedSize > 0)
			{
				// temporarily switch the committed parts to `R` so we can read them
				_pal.Protect(Start, CommittedSize, Protection.R);
				Marshal.Copy(Z.US(Start), _snapshot, 0, (int)CommittedSize);
			}
			_hash = WaterboxUtils.Hash(_snapshot);
			_sealed = true;
			ProtectAll();
			_pal.SetWriteStatus(_dirtydata);
		}

		const ulong MAGIC = 18123868458638683;

		public void SaveState(BinaryWriter w)
		{
			EnsureActive();
			EnsureSealed();
			_pal.GetWriteStatus(_dirtydata, _pageData);

			w.Write(MAGIC);
			w.Write(Start);
			w.Write(Size);
			w.Write(_hash);
			w.Write(CommittedSize);
			w.Write((byte[])(object)_pageData);
			w.Write((byte[])(object)_dirtydata);

			var buff = new byte[4096];
			int p;
			ulong addr;
			ulong endAddr = Start + CommittedSize;
			for (p = 0, addr = Start; addr < endAddr; p++, addr += 4096)
			{
				if (_pageData[p] != Protection.RW_Invisible && (_dirtydata[p] & WriteDetectionStatus.DidChange) != 0)
				{
					// TODO: It's slow to toggle individual pages like this.
					// Maybe that's OK because None is not used much?
					if (_pageData[p] == Protection.None)
						_pal.Protect(addr, 4096, Protection.R);
					Marshal.Copy(Z.US(addr), buff, 0, 4096);
					w.Write(buff);
					if (_pageData[p] == Protection.None)
						_pal.Protect(addr, 4096, Protection.None);
				}
			}
			// Console.WriteLine($"{Start:x16}");
			// var voom = _pageData.Take((int)(CommittedSize >> 12)).Select(p =>
			// {
			// 	switch (p)
			// 	{
			// 		case Protection.None: return ' ';
			// 		case Protection.R: return 'R';
			// 		case Protection.RW: return 'W';
			// 		case Protection.RX: return 'X';
			// 		case Protection.RW_Invisible: return '!';
			// 		case Protection.RW_Stack: return '+';
			// 		default: return '?';
			// 	}
			// }).Select((c, i) => new { c, i }).GroupBy(a => a.i / 60);
			// var zoom = _dirtydata.Take((int)(CommittedSize >> 12)).Select(p =>
			// {
			// 	switch (p)
			// 	{
			// 		case WriteDetectionStatus.CanChange: return '.';
			// 		case WriteDetectionStatus.CanChange | WriteDetectionStatus.DidChange: return '*';
			// 		case 0: return ' ';
			// 		case WriteDetectionStatus.DidChange: return '!';
			// 		default: return '?';
			// 	}
			// }).Select((c, i) => new { c, i }).GroupBy(a => a.i / 60);
			// foreach (var l in voom.Zip(zoom, (a, b) => new { a, b }))
			// {
			// 	Console.WriteLine("____" + new string(l.a.Select(a => a.c).ToArray()));
			// 	Console.WriteLine("____" + new string(l.b.Select(a => a.c).ToArray()));
			// }
		}

		public void LoadState(BinaryReader r)
		{
			EnsureActive();
			EnsureSealed();
			_pal.GetWriteStatus(_dirtydata, _pageData);

			if (r.ReadUInt64() != MAGIC || r.ReadUInt64() != Start || r.ReadUInt64() != Size)
				throw new InvalidOperationException("Savestate internal mismatch");
			if (!r.ReadBytes(_hash.Length).SequenceEqual(_hash))
			{
				// romhackurz need this not to throw on them.
				// anywhere where non-sync settings enter non-invisible ram, we need this not to throw
				Console.Error.WriteLine("WARNING: MEMORY BLOCK CONSISTENCY CHECK FAILED");
			}
			var newCommittedSize = r.ReadUInt64();
			if (newCommittedSize > CommittedSize)
			{
				_pal.Commit(newCommittedSize);
			}
			else if (newCommittedSize < CommittedSize)
			{
				// PAL layer won't let us shrink commits, but that's kind of OK
				var start = Start + newCommittedSize;
				var size = CommittedSize - newCommittedSize;
				_pal.Protect(start, size, Protection.RW);
				WaterboxUtils.ZeroMemory(Z.US(start), (long)size);
				_pal.Protect(start, size, Protection.None);
			}
			CommittedSize = newCommittedSize;
			var newPageData = (Protection[])(object)r.ReadBytes(_pageData.Length);
			var newDirtyData = (WriteDetectionStatus[])(object)r.ReadBytes(_dirtydata.Length);

			var buff = new byte[4096];
			int p;
			ulong addr;
			ulong endAddr = Start + CommittedSize;
			for (p = 0, addr = Start; addr < endAddr; p++, addr += 4096)
			{
				var dirty = (_dirtydata[p] & WriteDetectionStatus.DidChange) != 0;
				var newDirty = (newDirtyData[p] & WriteDetectionStatus.DidChange) != 0;
				var inState = newPageData[p] != Protection.RW_Invisible && newDirty;

				if (dirty || inState)
				{
					// must write out changed data

					// TODO: It's slow to toggle individual pages like this
					if (_pageData[p] != Protection.RW)
						_pal.Protect(addr, 4096, Protection.RW);

					// NB: There are some weird behaviors possible if a block transitions to or from RW_Invisible,
					// but nothing really "broken" (as far as I know); just reflecting the fact that you cannot track it.

					if (inState)
					{
						// changed data comes from the savestate
						r.Read(buff, 0, 4096);
						Marshal.Copy(buff, 0, Z.US(addr), 4096);
					}
					else
					{
						// data comes from the snapshot
						var offs = (int)(addr - Start);
						if (offs < _snapshot.Length)
						{
							Marshal.Copy(_snapshot, offs, Z.US(addr), 4096);
						}
						else
						{
							// or was not in the snapshot at all, so had never been changed by seal
							WaterboxUtils.ZeroMemory(Z.US(addr), 4096);
						}
					}
				}
			}
			_pageData = newPageData;
			_dirtydata = newDirtyData;
			ProtectAll();
			_pal.SetWriteStatus(_dirtydata);
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
			/// <summary>
			/// This area should not be tracked for changes, and should not be saved.
			/// </summary>
			RW_Invisible,
			/// <summary>
			/// This area may be used as a stack and should use a change detection that will work on stacks.
			/// (In windows, this is an inferior detection that triggers on reads.  In Linux, this flag has no effect.)
			/// </summary>
			RW_Stack,
		}
	}
}
