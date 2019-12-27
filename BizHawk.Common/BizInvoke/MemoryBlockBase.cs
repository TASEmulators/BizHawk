using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BizHawk.Common.BizInvoke
{
	public abstract class MemoryBlockBase : IDisposable
	{
		protected MemoryBlockBase(ulong start, ulong size)
		{
			if (!WaterboxUtils.Aligned(start)) throw new ArgumentOutOfRangeException();
			if (size == 0) throw new ArgumentOutOfRangeException();
			Start = start;
			Size = WaterboxUtils.AlignUp(size);
			End = Start + Size;
			_pageData = new Protection[GetPage(End - 1) + 1];
		}

		/// <summary>stores last set memory protection value for each page</summary>
		protected readonly Protection[] _pageData;

		/// <summary>ending address of the memory block; equal to <see cref="Start"/> + <see cref="Size"/></summary>
		public readonly ulong End;

		/// <summary>total size of the memory block</summary>
		public readonly ulong Size;

		/// <summary>starting address of the memory block</summary>
		public readonly ulong Start;

		/// <summary>snapshot for XOR buffer</summary>
		protected byte[] _snapshot;

		/// <summary>true if this is currently swapped in</summary>
		public bool Active { get; protected set; }

		public byte[] XorHash { get; protected set; }

		/// <summary>get a page index within the block</summary>
		protected int GetPage(ulong addr)
		{
			if (addr < Start || End <= addr) throw new ArgumentOutOfRangeException();
			return (int) ((addr - Start) >> WaterboxUtils.PageShift);
		}

		/// <summary>get a start address for a page index within the block</summary>
		protected ulong GetStartAddr(int page) => ((ulong) page << WaterboxUtils.PageShift) + Start;

		/// <summary>Get a stream that can be used to read or write from part of the block. Does not check for or change <see cref="Protect"/>!</summary>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or end (= <paramref name="start"/> + <paramref name="length"/>) are outside bounds of memory block (<see cref="Start"/> and <see cref="End"/>)</exception>
		public Stream GetStream(ulong start, ulong length, bool writer)
		{
			if (start < Start) throw new ArgumentOutOfRangeException(nameof(start));
			if (End < start + length) throw new ArgumentOutOfRangeException(nameof(length));
			return new MemoryViewStream(!writer, writer, (long) start, (long) length, this);
		}

		/// <summary>get a stream that can be used to read or write from part of the block. both reads and writes will be XORed against an earlier recorded snapshot</summary>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or end (= <paramref name="start"/> + <paramref name="length"/>) are outside bounds of memory block (<see cref="Start"/> and <see cref="End"/>)</exception>
		/// <exception cref="InvalidOperationException">no snapshot taken (haven't called <see cref="SaveXorSnapshot"/>)</exception>
		public Stream GetXorStream(ulong start, ulong length, bool writer)
		{
			if (start < Start) throw new ArgumentOutOfRangeException(nameof(start));
			if (End < start + length) throw new ArgumentOutOfRangeException(nameof(length));
			if (_snapshot == null) throw new InvalidOperationException("No snapshot taken!");
			return new MemoryViewXorStream(!writer, writer, (long) start, (long) length, this, _snapshot, (long) (start - Start));
		}

		/// <summary>activate the memory block, swapping it in at the pre-specified address</summary>
		public abstract void Activate();

		/// <summary>deactivate the memory block, removing it from RAM but leaving it immediately available to swap back in</summary>
		public abstract void Deactivate();

		/// <summary>take a hash of the current full contents of the block, including unreadable areas</summary>
		public abstract byte[] FullHash();

		/// <summary>set r/w/x protection on a portion of memory. rounded to encompassing pages</summary>
		public abstract void Protect(ulong start, ulong length, Protection prot);

		/// <summary>restore all recorded protections</summary>
		protected abstract void ProtectAll();

		/// <summary>take a snapshot of the entire memory block's contents, for use in <see cref="GetXorStream"/></summary>
		public abstract void SaveXorSnapshot();

		public abstract void Dispose(bool disposing);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~MemoryBlockBase()
		{
			Dispose(false);
		}

		/// <summary>allocate <paramref name="size"/> bytes starting at a particular address <paramref name="start"/></summary>
		public static MemoryBlockBase CallPlatformCtor(ulong start, ulong size) => OSTailoredCode.IsUnixHost
			? (MemoryBlockBase) new MemoryBlockUnix(start, size)
			: new MemoryBlock(start, size);

		/// <summary>allocate <paramref name="size"/> bytes at any address</summary>
		public static MemoryBlockBase CallPlatformCtor(ulong size) => CallPlatformCtor(0, size);

		/// <summary>Memory protection constant</summary>
		public enum Protection : byte { None, R, RW, RX }

		private class MemoryViewStream : Stream
		{
			public MemoryViewStream(bool readable, bool writable, long ptr, long length, MemoryBlockBase owner)
			{
				_readable = readable;
				_writable = writable;
				_ptr = ptr;
				_length = length;
				_owner = owner;
				_pos = 0;
			}

			private readonly long _length;
			private readonly MemoryBlockBase _owner;
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
				get { return _pos; }
				set
				{
					if (value < 0 || _length < value) throw new ArgumentOutOfRangeException();
					_pos = value;
				}
			}

			private void EnsureNotDisposed()
			{
				if (_owner.Start == 0) throw new ObjectDisposedException("MemoryBlockBase");
			}

			/// <remarks>TODO should this call base.Flush()? --yoshi</remarks>
			public override void Flush() {}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (!_readable) throw new InvalidOperationException();
				if (count < 0 || buffer.Length < count + offset) throw new ArgumentOutOfRangeException();
				EnsureNotDisposed();

				count = (int) Math.Min(count, _length - _pos);
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
				if (!_writable) throw new InvalidOperationException();
				if (count < 0 || _length - _pos < count || buffer.Length < count + offset) throw new ArgumentOutOfRangeException();
				EnsureNotDisposed();

				Marshal.Copy(buffer, offset, Z.SS(_ptr + _pos), count);
				_pos += count;
			}
		}

		private class MemoryViewXorStream : MemoryViewStream
		{
			public MemoryViewXorStream(bool readable, bool writable, long ptr, long length, MemoryBlockBase owner, byte[] initial, long offset)
				: base(readable, writable, ptr, length, owner)
			{
				_initial = initial;
				_offset = (int) offset;
			}

			/// <summary>the initial data to XOR against for both reading and writing</summary>
			private readonly byte[] _initial;

			/// <summary>offset into the XOR data that this stream is representing</summary>
			private readonly int _offset;

			public override int Read(byte[] buffer, int offset, int count)
			{
				var pos = (int) Position;
				count = base.Read(buffer, offset, count);
				XorTransform(_initial, _offset + pos, buffer, offset, count);
				return count;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				var pos = (int) Position;
				if (count < 0 || Length - pos < count || buffer.Length < count + offset) throw new ArgumentOutOfRangeException();

				// is mutating the buffer passed to Stream.Write kosher?
				XorTransform(_initial, _offset + pos, buffer, offset, count);
				base.Write(buffer, offset, count);
			}

			/// <remarks>bounds check already done by calling method i.e. in <see cref="MemoryViewStream.Read">base.Read</see> (for <see cref="Read"/>) or in <see cref="Write"/></remarks>
			private static unsafe void XorTransform(byte[] source, int sourceOffset, byte[] dest, int destOffset, int length)
			{
				// TODO: C compilers can make this pretty snappy, but can the C# jitter? Or do we need intrinsics
				fixed (byte* _s = source, _d = dest)
				{
					byte* s = _s + sourceOffset;
					byte* d = _d + destOffset;
					byte* sEnd = s + length;
					while (s < sEnd) *d++ ^= *s++;
				}
			}
		}
	}
}
