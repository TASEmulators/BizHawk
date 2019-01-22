using System;
using System.Runtime.InteropServices;
using System.IO;

namespace BizHawk.Common.BizInvoke
{
	public abstract class MemoryBlock : IDisposable
	{
		/// <summary>
		/// starting address of the memory block
		/// </summary>
		public ulong Start { get; protected set; }
		/// <summary>
		/// total size of the memory block
		/// </summary>
		public ulong Size { get; protected set; }
		/// <summary>
		/// ending address of the memory block; equal to start + size
		/// </summary>
		public ulong End { get; protected set; }

		/// <summary>
		/// true if this is currently swapped in
		/// </summary>
		public bool Active { get; protected set; }

		/// <summary>
		/// stores last set memory protection value for each page
		/// </summary>
		protected Protection[] _pageData;

		/// <summary>
		/// snapshot for XOR buffer
		/// </summary>
		protected byte[] _snapshot;

		public byte[] XorHash { get; protected set; }

		/// <summary>
		/// get a page index within the block
		/// </summary>
		protected int GetPage(ulong addr)
		{
			if (addr < Start || addr >= End) throw new ArgumentOutOfRangeException();
			return (int) ((addr - Start) >> WaterboxUtils.PageShift);
		}

		/// <summary>
		/// get a start address for a page index within the block
		/// </summary>
		protected ulong GetStartAddr(int page)
		{
			return ((ulong) page << WaterboxUtils.PageShift) + Start;
		}

		/// <summary>
		/// activate the memory block, swapping it in at the specified address
		/// </summary>
		public abstract void Activate();

		/// <summary>
		/// deactivate the memory block, removing it from RAM but leaving it immediately available to swap back in
		/// </summary>
		public abstract void Deactivate();

		/// <summary>
		/// Memory protection constant
		/// </summary>
		public enum Protection : byte
		{
			None, R, RW, RX
		}

		/// <summary>
		/// Get a stream that can be used to read or write from part of the block. Does not check for or change Protect()!
		/// </summary>
		public Stream GetStream(ulong start, ulong length, bool writer)
		{
			if (start < Start) throw new ArgumentOutOfRangeException(nameof(start));
			if (start + length > End) throw new ArgumentOutOfRangeException(nameof(length));
			return new MemoryViewStream(!writer, writer, (long) start, (long) length, this);
		}

		/// <summary>
		/// get a stream that can be used to read or write from part of the block.
		/// both reads and writes will be XORed against an earlier recorded snapshot
		/// </summary>
		public Stream GetXorStream(ulong start, ulong length, bool writer)
		{
			if (start < Start) throw new ArgumentOutOfRangeException(nameof(start));
			if (start + length > End) throw new ArgumentOutOfRangeException(nameof(length));
			if (_snapshot == null) throw new InvalidOperationException("No snapshot taken!");
			return new MemoryViewXorStream(!writer, writer, (long) start, (long) length, this, _snapshot, (long) (start - Start));
		}

		/// <summary>
		/// take a snapshot of the entire memory block's contents, for use in GetXorStream
		/// </summary>
		public abstract void SaveXorSnapshot();

		/// <summary>
		/// take a hash of the current full contents of the block, including unreadable areas
		/// </summary>
		/// <returns></returns>
		public abstract byte[] FullHash();

		/// <summary>
		/// restore all recorded protections
		/// </summary>
		protected abstract void ProtectAll();

		/// <summary>
		/// set r/w/x protection on a portion of memory. rounded to encompassing pages
		/// </summary>
		public abstract void Protect(ulong start, ulong length, Protection prot);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected abstract void Dispose(bool disposing);

		~MemoryBlock()
		{
			Dispose(false);
		}

		/// <summary>
		/// allocate size bytes at any address
		/// </summary>
		public static MemoryBlock PlatformConstructor(ulong size)
		{
			return PlatformConstructor(0, size);
		}

		/// <summary>
		/// allocate size bytes starting at a particular address
		/// </summary>
		public static MemoryBlock PlatformConstructor(ulong start, ulong size)
		{
			return PlatformLinkedLibSingleton.RunningOnUnix
//				? (MemoryBlock) new MemoryBlockUnix(start, size)
				? throw new InvalidOperationException("ctor of nonfunctional MemoryBlockUnix class")
				: (MemoryBlock) new MemoryBlockWin32(start, size);
		}

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

			private void EnsureNotDisposed()
			{
				if (_owner.Start == 0) throw new ObjectDisposedException("MemoryBlock");
			}

			private MemoryBlock _owner;

			private readonly bool _readable;
			private readonly bool _writable;

			private long _length;
			private long _pos;
			private readonly long _ptr;

			public override bool CanRead => _readable;
			public override bool CanSeek => true;
			public override bool CanWrite => _writable;
			public override void Flush() { }
			public override long Length => _length;

			public override long Position
			{
				get { return _pos; }
				set
				{
					if (value < 0 || value > _length) throw new ArgumentOutOfRangeException();
					_pos = value;
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (!_readable) throw new InvalidOperationException();
				if (count < 0 || count + offset > buffer.Length) throw new ArgumentOutOfRangeException();
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
				if (count < 0 || count + offset > buffer.Length || count > _length - _pos)
					throw new ArgumentOutOfRangeException();
				EnsureNotDisposed();
				Marshal.Copy(buffer, offset, Z.SS(_ptr + _pos), count);
				_pos += count;
			}
		}

		private class MemoryViewXorStream : MemoryViewStream
		{
			public MemoryViewXorStream(bool readable, bool writable, long ptr, long length, MemoryBlock owner,
				byte[] initial, long offset)
				: base(readable, writable, ptr, length, owner)
			{
				_initial = initial;
				_offset = (int) offset;
			}

			/// <summary>
			/// the initial data to XOR against for both reading and writing
			/// </summary>
			private readonly byte[] _initial;

			/// <summary>
			/// offset into the XOR data that this stream is representing
			/// </summary>
			private readonly int _offset;

			public override int Read(byte[] buffer, int offset, int count)
			{
				int pos = (int) Position;
				count = base.Read(buffer, offset, count);
				XorTransform(_initial, _offset + pos, buffer, offset, count);
				return count;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				int pos = (int) Position;
				if (count < 0 || count + offset > buffer.Length || count > Length - pos)
					throw new ArgumentOutOfRangeException();
				// is mutating the buffer passed to Stream.Write kosher?
				XorTransform(_initial, _offset + pos, buffer, offset, count);
				base.Write(buffer, offset, count);
			}

			private static unsafe void XorTransform(byte[] source, int sourceOffset, byte[] dest, int destOffset, int length)
			{
				// we don't do any bounds check because MemoryViewStream.Read and MemoryViewXorStream.Write already did it

				// TODO: C compilers can make this pretty snappy, but can the C# jitter? Or do we need intrinsics
				fixed (byte* _s = source, _d = dest)
				{
					byte* s = _s + sourceOffset;
					byte* d = _d + destOffset;
					byte* sEnd = s + length;
					while (s < sEnd)
					{
						*d++ ^= *s++;
					}
				}
			}
		}
	}
}
