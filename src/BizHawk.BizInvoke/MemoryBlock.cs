using System;
using System.IO;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.BizInvoke
{
	public class MemoryBlock : IDisposable
	{
		/// <summary>allocate <paramref name="size"/> bytes starting at a particular address <paramref name="start"/></summary>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is not aligned or <paramref name="size"/> is <c>0</c></exception>
		public MemoryBlock(ulong start, ulong size)
		{
			if (!WaterboxUtils.Aligned(start))
				throw new ArgumentOutOfRangeException(nameof(start), start, "start address must be aligned");
			if (size == 0)
				throw new ArgumentOutOfRangeException(nameof(size), size, "cannot create 0-length block");
			Start = start;
			Size = WaterboxUtils.AlignUp(size);
			EndExclusive = Start + Size;
			_pageData = new Protection[GetPage(EndExclusive - 1) + 1];

			_pal = OSTailoredCode.IsUnixHost
				? (IMemoryBlockPal)new MemoryBlockUnixPal(Start, Size)
				: new MemoryBlockWindowsPal(Start, Size);
		}

		private IMemoryBlockPal _pal;

		/// <summary>
		/// Size that has been committed to actual underlying RAM.  Never shrinks.  Private because
		/// it should be transparent to the caller.  ALWAYS ALIGNED.
		/// </summary>
		private ulong CommittedSize;

		/// <summary>
		/// The last CommittedSize that was sent to the PAL layer when we were active.
		/// TODO: Do we really need the ability to change protections while inactive?
		/// </summary>
		private ulong LastActiveCommittedSize;

		/// <summary>stores last set memory protection value for each page</summary>
		private readonly Protection[] _pageData;

		/// <summary>
		/// end address of the memory block (not part of the block; class invariant: equal to <see cref="Start"/> + <see cref="Size"/>)
		/// </summary>
		public readonly ulong EndExclusive;

		/// <summary>total size of the memory block</summary>
		public readonly ulong Size;

		/// <summary>starting address of the memory block</summary>
		public readonly ulong Start;

		/// <summary>snapshot for XOR buffer</summary>
		private byte[] _snapshot;

		/// <summary>true if this is currently swapped in</summary>
		public bool Active { get; private set; }

		public byte[] XorHash { get; private set; }

		/// <summary>get a page index within the block</summary>
		private int GetPage(ulong addr)
		{
			if (addr < Start || addr >= EndExclusive)
				throw new ArgumentOutOfRangeException(nameof(addr), addr, "invalid address");
			return (int) ((addr - Start) >> WaterboxUtils.PageShift);
		}

		/// <summary>get a start address for a page index within the block</summary>
		private ulong GetStartAddr(int page) => ((ulong) page << WaterboxUtils.PageShift) + Start;

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
			return new MemoryViewStream(!writer, writer, (long)start, (long)length, this);
		}

		/// <summary>
		/// get a stream that can be used to read or write from part of the block.
		/// both reads and writes will be XORed against an earlier recorded snapshot
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="start"/> or end (= <paramref name="start"/> + <paramref name="length"/> - <c>1</c>) are outside
		/// [<see cref="Start"/>, <see cref="EndExclusive"/>), the range of the block
		/// </exception>
		/// <exception cref="InvalidOperationException">no snapshot taken (haven't called <see cref="SaveXorSnapshot"/>)</exception>
		public Stream GetXorStream(ulong start, ulong length, bool writer)
		{
			if (start < Start)
				throw new ArgumentOutOfRangeException(nameof(start), start, "invalid address");
			if (EndExclusive < start + length)
				throw new ArgumentOutOfRangeException(nameof(length), length, "requested length implies invalid end address");
			if (_snapshot == null)
				throw new InvalidOperationException("No snapshot taken!");
			return new MemoryViewXorStream(!writer, writer, (long)start, (long)length, this, _snapshot, (long)(start - Start));
		}

		/// <summary>activate the memory block, swapping it in at the pre-specified address</summary>
		/// <exception cref="InvalidOperationException"><see cref="MemoryBlock.Active"/> is <see langword="true"/> or failed to map file view</exception>
		public void Activate()
		{
			if (Active)
				throw new InvalidOperationException("Already active");
			_pal.Activate();
			if (CommittedSize > LastActiveCommittedSize)
			{
				_pal.Commit(CommittedSize);
				LastActiveCommittedSize = CommittedSize;
			}
			ProtectAll();
			Active = true;
		}

		/// <summary>deactivate the memory block, removing it from RAM but leaving it immediately available to swap back in</summary>
		/// <exception cref="InvalidOperationException">
		/// <see cref="MemoryBlock.Active"/> is <see langword="false"/> or failed to unmap file view
		/// </exception>
		public void Deactivate()
		{
			if (!Active)
				throw new InvalidOperationException("Not active");
			_pal.Deactivate();
			Active = false;
		}

		/// <summary>
		/// take a hash of the current full contents of the block, including unreadable areas
		/// but not uncommitted areas
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// <see cref="MemoryBlock.Active"/> is <see langword="false"/> or failed to make memory read-only
		/// </exception>
		public byte[] FullHash()
		{
			if (!Active)
				throw new InvalidOperationException("Not active");
			// temporarily switch the committed parts to `R` so we can read them
			if (CommittedSize > 0)
				_pal.Protect(Start, CommittedSize, Protection.R);
			var ret = WaterboxUtils.Hash(GetStream(Start, CommittedSize, false));
			ProtectAll();
			return ret;
		}


		/// <summary>set r/w/x protection on a portion of memory. rounded to encompassing pages</summary
		/// <exception cref="InvalidOperationException">failed to protect memory</exception>
		public void Protect(ulong start, ulong length, Protection prot)
		{
			if (length == 0)
				return;
			
			// Note: asking for prot.none on memory that was not previously committed, commits it

			var computedStart = WaterboxUtils.AlignDown(start);
			var computedEnd = WaterboxUtils.AlignUp(start + length);
			var computedLength = computedEnd - computedStart;

			int pstart = GetPage(start);
			int pend = GetPage(start + length - 1);

			for (int i = pstart; i <= pend; i++)
				_pageData[i] = prot; // also store the value for later use

			// potentially commit more memory
			var minNewCommittedSize = computedEnd - Start;
			if (minNewCommittedSize > CommittedSize)
			{
				CommittedSize = minNewCommittedSize;
			}

			if (Active) // it's legal to Protect() if we're not active; the information is just saved for the next activation
			{
				if (CommittedSize > LastActiveCommittedSize)
				{
					_pal.Commit(CommittedSize);
					LastActiveCommittedSize = CommittedSize;
					ProtectAll();
				}
				else
				{
					_pal.Protect(computedStart, computedLength, prot);
				}
			}
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
					_pal.Protect(zstart, zend - zstart, _pageData[i]);
					ps = i + 1;
				}
			}
		}

		/// <summary>take a snapshot of the entire memory block's contents, for use in <see cref="GetXorStream"/></summary>
		/// <exception cref="InvalidOperationException">
		/// snapshot already taken, <see cref="MemoryBlock.Active"/> is <see langword="false"/>, or failed to make memory read-only
		/// </exception>
		public void SaveXorSnapshot()
		{
			// note: The snapshot only holds up to the current committed size.  We compensate for that in xorstream
			if (_snapshot != null)
				throw new InvalidOperationException("Snapshot already taken");
			if (!Active)
				throw new InvalidOperationException("Not active");

			// temporarily switch the entire committed area to `R`: in case some areas are unreadable, we don't want
			// that to complicate things
			if (CommittedSize > 0)
				_pal.Protect(Start, CommittedSize, Protection.R);

			_snapshot = new byte[CommittedSize];
			var ds = new MemoryStream(_snapshot, true);
			var ss = GetStream(Start, CommittedSize, false);
			ss.CopyTo(ds);
			XorHash = WaterboxUtils.Hash(_snapshot);

			ProtectAll();
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
		public enum Protection : byte { None, R, RW, RX }

		private class MemoryViewStream : Stream
		{
			public MemoryViewStream(bool readable, bool writable, long ptr, long length, MemoryBlock owner)
			{
				_readable = readable;
				_writable = writable;
				_ptr = ptr;
				_length = length;
				_owner = owner;
				_pos = 0;
			}

			private readonly long _length;
			private readonly MemoryBlock _owner;
			private readonly long _ptr;
			private readonly bool _readable;
			private readonly bool _writable;

			private long _pos;

			public override bool CanRead => _readable;
			public override bool CanSeek => true;
			public override bool CanWrite => _writable;
			public override long Length => _length;
			public override long Position
			{
				get => _pos;
				set
				{
					if (value < 0 || value > _length)
						throw new ArgumentOutOfRangeException();
					_pos = value;
				}
			}

			private void EnsureNotDisposed()
			{
				if (_owner.Start == 0)
					throw new ObjectDisposedException(nameof(MemoryBlock));
			}

			public override void Flush() {}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (!_readable)
					throw new InvalidOperationException();
				if (count < 0 || offset + count > buffer.Length)
					throw new ArgumentOutOfRangeException();
				EnsureNotDisposed();

				count = (int)Math.Min(count, _length - _pos);
				Marshal.Copy(Z.SS(_ptr + _pos), buffer, offset, count);
				_pos += count;
				return count;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				long newpos;
				switch (origin)
				{
					default:
					case SeekOrigin.Begin:
						newpos = offset;
						break;
					case SeekOrigin.Current:
						newpos = _pos + offset;
						break;
					case SeekOrigin.End:
						newpos = _length + offset;
						break;
				}
				Position = newpos;
				return newpos;
			}

			public override void SetLength(long value)
			{
				throw new InvalidOperationException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (!_writable)
					throw new InvalidOperationException();
				if (count < 0 || _pos + count > _length || offset + count > buffer.Length)
					throw new ArgumentOutOfRangeException();
				EnsureNotDisposed();

				Marshal.Copy(buffer, offset, Z.SS(_ptr + _pos), count);
				_pos += count;
			}
		}

		private class MemoryViewXorStream : MemoryViewStream
		{
			public MemoryViewXorStream(bool readable, bool writable, long ptr, long length, MemoryBlock owner, byte[] initial, long offset)
				: base(readable, writable, ptr, length, owner)
			{
				_initial = initial;
				_offset = (int)offset;
			}

			/// <summary>the initial data to XOR against for both reading and writing</summary>
			private readonly byte[] _initial;

			/// <summary>offset into the XOR data that this stream is representing</summary>
			private readonly int _offset;

			public override int Read(byte[] buffer, int offset, int count)
			{
				var pos = (int)Position;
				count = base.Read(buffer, offset, count);
				XorTransform(_initial, _offset + pos, buffer, offset, count);
				return count;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				var pos = (int)Position;
				if (count < 0 || pos + count > Length || offset + count > buffer.Length)
					throw new ArgumentOutOfRangeException();

				// is mutating the buffer passed to Stream.Write kosher?
				XorTransform(_initial, _offset + pos, buffer, offset, count);
				base.Write(buffer, offset, count);
			}

			private static unsafe void XorTransform(byte[] source, int sourceOffset, byte[] dest, int destOffset, int length)
			{
				// bounds checks on dest have been done already, but not on source
				// If a xorsnapshot was created and then the heap was later expanded, those other bytes are effectively 0

				// TODO: C compilers can make this pretty snappy, but can the C# jitter? Or do we need intrinsics
				fixed (byte* _s = source, _d = dest)
				{
					byte* s = _s + sourceOffset;
					byte* d = _d + destOffset;
					byte* sEnd = _s + Math.Min(sourceOffset + length, source.Length);
					while (s < sEnd)
						*d++ ^= *s++;
					// for anything left in dest past source.Length, we need not transform at all
				}
			}
		}
	}
}
