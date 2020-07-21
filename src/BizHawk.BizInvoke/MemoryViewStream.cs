using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BizHawk.BizInvoke
{
	/// <summary>
	/// Create a stream that allows read/write over a set of unmanaged memory pointers
	/// The validity and lifetime of those pointers is YOUR responsibility
	/// </summary>
	public class MemoryViewStream : Stream
	{
		public MemoryViewStream(bool readable, bool writable, long ptr, long length)
		{
			_readable = readable;
			_writable = writable;
			_ptr = ptr;
			_length = length;
			_pos = 0;
		}

		private readonly long _length;
		private readonly long _ptr;
		private readonly bool _readable;
		private readonly bool _writable;
		private bool _closed;

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
			if (_closed)
				throw new ObjectDisposedException(nameof(MemoryViewStream));
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

		protected override void Dispose(bool disposing)
		{
			_closed = true;
			base.Dispose(disposing);
		}
	}
}
