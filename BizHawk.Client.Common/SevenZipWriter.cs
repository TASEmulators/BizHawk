using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace BizHawk.Client.Common
{
	public class SevenZipWriter : IZipWriter
	{
		private class RangBuffer
		{
			const int LEN = 4096;
			const int MASK = 4095;
			byte[] buff = new byte[LEN];

			int wpos = 0;
			int rpos = 0;

			bool writeclosed;
			bool readclosed;

			object sharedlock = new object();

			public Stream W { get; private set; }
			public Stream R { get; private set; }

			public RangBuffer()
			{
				W = new WStream(this);
				R = new RStream(this);
			}

			public int Read(byte[] buffer, int offset, int count)
			{
				int ret = 0;
				while (count > 0)
				{
					lock (sharedlock)
					{
						int start = rpos;
						int end = wpos;
						if (end < start) // wrap
							end = LEN;
						if (end - start > count)
							end = start + count;

						int c = end - start;
						if (c > 0)
						{
							Buffer.BlockCopy(buff, start, buffer, offset, c);
							count -= c;
							ret += c;
							offset += c;
							rpos = end & MASK;
						}
						else if (writeclosed)
						{
							break;
						}
						else
						{
							// todo: don't spin here
						}
					}
				}
				return ret;
			}

			public void CloseRead()
			{
				lock (sharedlock)
					readclosed = true;
			}

			public int Write(byte[] buffer, int offset, int count)
			{
				int ret = 0;
				while (count > 0)
				{
					lock (sharedlock)
					{
						int start = wpos;
						int end = (rpos - 1) & MASK;
						if (end < start) // wrap
							end = LEN;
						if (end - start > count)
							end = start + count;

						int c = end - start;
						if (c > 0)
						{
							Buffer.BlockCopy(buffer, offset, buff, start, c);
							count -= c;
							ret += c;
							offset += c;
							wpos = end & MASK;
						}
						else if (readclosed)
						{
							break;
						}
						else
						{
							// todo: don't spin here
						}
					}
				}
				return ret;
			}

			public void CloseWrite()
			{
				lock (sharedlock)
					writeclosed = true;
			}

			private class WStream : Stream
			{
				public override bool CanRead { get { return false; } }
				public override bool CanSeek { get { return false; } }
				public override bool CanWrite { get { return true; } }
				public override void Flush() { }
				public override long Length { get { throw new NotSupportedException(); } }
				public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
				public override void SetLength(long value) { throw new NotSupportedException(); }
				public override long Position
				{
					get { throw new NotSupportedException(); }
					set { throw new NotSupportedException(); }
				}

				private RangBuffer _r;
				private long _total; // bytes written so far
				public WStream(RangBuffer r)
				{
					this._r = r;
				}

				public override int Read(byte[] buffer, int offset, int count)
				{
					throw new NotSupportedException();
				}

				public override void Write(byte[] buffer, int offset, int count)
				{
					int cnt = _r.Write(buffer, offset, count);
					_total += cnt;
					if (cnt < count)
						throw new IOException("broken pipe");
				}

				protected override void Dispose(bool disposing)
				{
					if (disposing && _r != null)
					{
						_r.CloseWrite();
						_r = null;
					}
					base.Dispose(disposing);
				}
			}
			private class RStream : Stream
			{
				public override bool CanRead { get { return true; } }
				public override bool CanSeek { get { return false; } }
				public override bool CanWrite { get { return false; } }
				public override void Flush() { }
				public override long Length { get { return 1; } } // { get { throw new NotSupportedException(); } }
				public override long Seek(long offset, SeekOrigin origin) { return 0; } // { throw new NotSupportedException(); }
				public override void SetLength(long value) { throw new NotSupportedException(); }
				public override long Position
				{
					get { throw new NotSupportedException(); }
					set { throw new NotSupportedException(); }
				}

				private RangBuffer _r;
				private long _total; // bytes read so far
				public RStream(RangBuffer r)
				{
					this._r = r;
				}

				public override int Read(byte[] buffer, int offset, int count)
				{
					int cnt = _r.Read(buffer, offset, count);
					_total += cnt; 
					return cnt;
				}

				public override void Write(byte[] buffer, int offset, int count)
				{
					throw new NotSupportedException();
				}

				protected override void Dispose(bool disposing)
				{
					if (disposing && _r != null)
					{
						_r.CloseRead();
						_r = null;
					}
					base.Dispose(disposing);
				}
			}
		}

		SevenZip.SevenZipCompressor svc;

		bool first = true;
		string path;
		int compressionlevel;

		public SevenZipWriter(string path, int compressionlevel)
		{
			this.path = path;
			this.compressionlevel = compressionlevel;

			svc = new SevenZip.SevenZipCompressor();
			svc.ArchiveFormat = SevenZip.OutArchiveFormat.Zip;

			switch (compressionlevel)
			{
				default:
				case 0: svc.CompressionLevel = SevenZip.CompressionLevel.None; break;
				case 1:
				case 2: svc.CompressionLevel = SevenZip.CompressionLevel.Fast; break;
				case 3:
				case 4: svc.CompressionLevel = SevenZip.CompressionLevel.Low; break;
				case 5:
				case 6: svc.CompressionLevel = SevenZip.CompressionLevel.Normal; break;
				case 7:
				case 8: svc.CompressionLevel = SevenZip.CompressionLevel.High; break;
				case 9: svc.CompressionLevel = SevenZip.CompressionLevel.Ultra; break;
			}
		}

		public void WriteItem(string name, Action<Stream> callback)
		{
			var dict = new Dictionary<string, Stream>();
			var r = new RangBuffer();
			dict[name] = r.R;
			if (first)
			{
				first = false;
				svc.CompressionMode = SevenZip.CompressionMode.Create;
			}
			else
			{
				svc.CompressionMode = SevenZip.CompressionMode.Append;
			}

			var task = Task.Factory.StartNew(() =>
				{
					svc.CompressStreamDictionary(dict, path);
				});
			try
			{
				callback(r.W);
			}
			finally
			{
				r.W.Dispose();
			}
			task.Wait();
		}

		public void Dispose()
		{
			// nothing to do
		}
	}
}
