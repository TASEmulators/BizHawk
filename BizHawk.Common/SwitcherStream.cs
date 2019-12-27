using System;
using System.IO;

namespace BizHawk.Common
{
	/// <summary>
	/// This stream redirects all operations to another stream, specified by the user
	/// You might think you can do this just by changing out the stream instance you operate on, but this was built to facilitate some features which were never built:
	/// The ability to have the old stream automatically flushed, or for a derived class to manage two streams at a higher level and use these facilities to switch them
	/// without this subclass's clients knowing about the existence of two streams.
	/// Well, it could be useful, so here it is.
	/// </summary>
	public class SwitcherStream : Stream
	{
		// switchstream method? flush old stream?
		private Stream _currStream;

		/// <summary>
		/// if this is enabled, seeks to Begin,0 will get ignored; anything else will be an exception
		/// </summary>
		public bool DenySeekHack = false;

		public override bool CanRead => _currStream.CanRead;

		public override bool CanSeek => _currStream.CanSeek;

		public override bool CanWrite => _currStream.CanWrite;

		public override long Length => _currStream.Length;

		/// <exception cref="InvalidOperationException">(from setter) <see cref="DenySeekHack"/> is <see langword="true"/> and <paramref name="value"/> is not <c>0</c></exception>
		public override long Position
		{
			get => _currStream.Position;
			set
			{
				if (DenySeekHack)
				{
					if (value == 0)
					{
						return;
					}

					throw new InvalidOperationException($"Cannot set position to non-zero in a {nameof(SwitcherStream)} with {DenySeekHack}=true");
				}

				_currStream.Position = value;
			}
		}

		public override void Flush()
		{
			_currStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _currStream.Read(buffer, offset, count);
		}

		/// <exception cref="InvalidOperationException"><see cref="DenySeekHack"/> is <see langword="true"/> and either <paramref name="value"/> is not <c>0</c> or <paramref name="origin"/> is not <see cref="SeekOrigin.Begin"/></exception>
		public override long Seek(long offset, SeekOrigin origin)
		{
			if (DenySeekHack)
			{
				if (offset == 0 && origin == SeekOrigin.Begin)
				{
					return 0;
				}

				throw new InvalidOperationException($"Cannot call {nameof(Seek)} with non-zero offset or non-{nameof(SeekOrigin.Begin)} origin in a {nameof(SwitcherStream)} with {nameof(DenySeekHack)}=true");
			}

			return _currStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_currStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_currStream.Write(buffer, offset, count);
		}
	}
}