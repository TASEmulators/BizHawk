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

		public SwitcherStream()
		{
		}

		/// <summary>
		/// if this is enabled, seeks to Begin,0 will get ignored; anything else will be an exception
		/// </summary>
		public bool DenySeekHack = false;

		public override bool CanRead { get { return _currStream.CanRead; } }
		public override bool CanSeek { get { return _currStream.CanSeek; } }
		public override bool CanWrite { get { return _currStream.CanWrite; } }

		public override long Length { get { return _currStream.Length; } }

		public override long Position
		{
			get
			{
				return _currStream.Position;
			}

			set
			{
				if (DenySeekHack)
				{
					if (value == 0) return;
					else throw new InvalidOperationException("Cannot set position to non-zero in a SwitcherStream with DenySeekHack=true");
				}
				_currStream.Position = value;
			}
		}

		public void SetCurrStream(Stream str)
		{
			_currStream = str;
		}

		public override void Flush()
		{
			_currStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _currStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			if (DenySeekHack)
			{
				if (offset == 0 && origin == SeekOrigin.Begin) return 0;
				else throw new InvalidOperationException("Cannot call Seek with non-zero offset or non-Begin origin in a SwitcherStream with DenySeekHack=true");
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