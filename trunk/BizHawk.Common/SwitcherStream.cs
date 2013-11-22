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
		//switchstream method? flush old stream?
		Stream CurrStream = null;

		public void SetCurrStream(Stream str) { CurrStream = str; }

		public SwitcherStream()
		{
		}

		public override bool CanRead { get { return CurrStream.CanRead; } }
		public override bool CanSeek { get { return CurrStream.CanSeek; } }
		public override bool CanWrite { get { return CurrStream.CanWrite; } }
		public override void Flush()
		{
			CurrStream.Flush();
		}

		public override long Length { get { return CurrStream.Length; } }

		public override long Position
		{
			get
			{
				return CurrStream.Position;
			}
			set
			{
				CurrStream.Position = Position;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return CurrStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return CurrStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			CurrStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			CurrStream.Write(buffer, offset, count);
		}
	}

}