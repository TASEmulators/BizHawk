using System;
using System.IO;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.BizInvoke
{
	/// <summary>
	/// Create a stream that allows read/write over a set of unmanaged memory pointers
	/// The validity and lifetime of those pointers is YOUR responsibility
	/// </summary>
	public unsafe class MemoryViewStream : Stream, ISpanStream
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

		private byte* CurrentPointer() => (byte*)Z.SS(_ptr + _pos);

		public int Read(Span<byte> buffer)
		{
			if (!_readable)
				throw new IOException();
			EnsureNotDisposed();
			var count = (int)Math.Min(buffer.Length, _length - _pos);
			new ReadOnlySpan<byte>(CurrentPointer(), count).CopyTo(buffer);
			_pos += count;
			return count;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return Read(new Span<byte>(buffer, offset, count));
		}

		public override int ReadByte()
		{
			if (_pos < _length)
			{
				var ret = *CurrentPointer();
				_pos++;
				return ret;
			}
			else
			{
				return -1;
			}
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
			throw new IOException();
		}

		public void Write(ReadOnlySpan<byte> buffer)
		{
			if (!_writable)
				throw new IOException();
			EnsureNotDisposed();
			if (_pos + buffer.Length > _length)
				throw new IOException("End of non-resizable stream");
			buffer.CopyTo(new Span<byte>(CurrentPointer(), buffer.Length));
			_pos += buffer.Length;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Write(new ReadOnlySpan<byte>(buffer, offset, count));
		}

		public override void WriteByte(byte value)
		{
			if (_pos < _length)
			{
				*CurrentPointer() = value;
				_pos++;
			}
			else
			{
				throw new IOException("End of non-resizable stream");
			}
		}

		protected override void Dispose(bool disposing)
		{
			_closed = true;
			base.Dispose(disposing);
		}
	}
}
