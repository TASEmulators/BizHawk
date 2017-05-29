using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace BizHawk.Client.Common
{
	public class SevenZipWriter : IZipWriter
	{
		private class RangBuffer
		{
			private const int Len = 4096;
			private const int Mask = 4095;

			private readonly byte[] _buff = new byte[Len];

			private readonly object _sharedlock = new object();
			private readonly ManualResetEvent _full = new ManualResetEvent(true);
			private readonly ManualResetEvent _empty = new ManualResetEvent(false);

			private int _wpos;
			private int _rpos;

			private bool _writeclosed;
			private bool _readclosed;

			public Stream W { get; }
			public Stream R { get; }

			public RangBuffer()
			{
				W = new WStream(this);
				R = new RStream(this);
			}

			public int ReadByte()
			{
				// slow, but faster than using the other overload with byte[1]
				while (true)
				{
					_empty.WaitOne();
					lock (_sharedlock)
					{
						if (_rpos != _wpos)
						{
							byte ret = _buff[_rpos++];
							_rpos &= Mask;
							_full.Set();
							return ret;
						}
						else if (_writeclosed)
						{
							return -1;
						}
						else
						{
							_empty.Reset();
						}
					}
				}
			}

			public int Read(byte[] buffer, int offset, int count)
			{
				int ret = 0;
				while (count > 0)
				{
					_empty.WaitOne();
					lock (_sharedlock)
					{
						int start = _rpos;
						int end = _wpos;
						if (end < start) // wrap
						{
							end = Len;
						}

						if (end - start > count)
						{
							end = start + count;
						}

						int c = end - start;
						if (c > 0)
						{
							Buffer.BlockCopy(_buff, start, buffer, offset, c);
							count -= c;
							ret += c;
							offset += c;
							_rpos = end & Mask;
							_full.Set();
						}
						else if (_writeclosed)
						{
							break;
						}
						else
						{
							_empty.Reset();
						}
					}
				}

				return ret;
			}

			public void CloseRead()
			{
				lock (_sharedlock)
				{
					_readclosed = true;
					_full.Set();
				}
			}

			public bool WriteByte(byte value)
			{
				while (true)
				{
					_full.WaitOne();
					lock (_sharedlock)
					{
						int next = (_wpos + 1) & Mask;
						if (next != _rpos)
						{
							_buff[_wpos] = value;
							_wpos = next;
							_empty.Set();
							return true;
						}

						if (_readclosed)
						{
							return false;
						}

						_full.Reset();
					}
				}
			}

			public int Write(byte[] buffer, int offset, int count)
			{
				int ret = 0;
				while (count > 0)
				{
					_full.WaitOne();
					lock (_sharedlock)
					{
						int start = _wpos;
						int end = (_rpos - 1) & Mask;
						if (end < start) // wrap
						{
							end = Len;
						}

						if (end - start > count)
						{
							end = start + count;
						}

						int c = end - start;
						if (c > 0)
						{
							Buffer.BlockCopy(buffer, offset, _buff, start, c);
							count -= c;
							ret += c;
							offset += c;
							_wpos = end & Mask;
							_empty.Set();
						}
						else if (_readclosed)
						{
							break;
						}
						else
						{
							_full.Reset();
						}
					}
				}

				return ret;
			}

			public void CloseWrite()
			{
				lock (_sharedlock)
				{
					_writeclosed = true;
					_empty.Set();
				}
			}

			private class WStream : Stream
			{
				public override bool CanRead => false;
				public override bool CanSeek => false;
				public override bool CanWrite => true;

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
					_r = r;
				}

				public override int Read(byte[] buffer, int offset, int count)
				{
					throw new NotSupportedException();
				}

				public override void Write(byte[] buffer, int offset, int count)
				{
#if true
					int cnt = _r.Write(buffer, offset, count);
					_total += cnt;
					if (cnt < count)
					{
						throw new IOException("broken pipe");
					}
#else
					int end = offset + count;
					while (offset < end)
					{
						WriteByte(buffer[offset++]);
						_total++;
					}
#endif
				}

				public override void WriteByte(byte value)
				{
					if (!_r.WriteByte(value))
					{
						throw new IOException("broken pipe");
					}
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
				public override bool CanRead => true;
				public override bool CanSeek => false;
				public override bool CanWrite => false;

				public override void Flush() { }
				public override long Length => 1; // { get { throw new NotSupportedException(); } }
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
					_r = r;
				}

				public override int Read(byte[] buffer, int offset, int count)
				{
#if true
					int cnt = _r.Read(buffer, offset, count);
					_total += cnt; 
					return cnt;
#else
					int ret = 0;
					int end = offset + count;
					while (offset < end)
					{
						int val = ReadByte();
						if (val == -1)
							break;
						buffer[offset] = (byte)val;
						offset++;
						ret++;
						_total++;
					}
					return ret;
#endif
				}

				public override int ReadByte()
				{
					return _r.ReadByte();
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

		private readonly SevenZip.SevenZipCompressor _svc;
		private readonly string _path;

		private bool _first = true;
		private int _compressionlevel;

		public SevenZipWriter(string path, int compressionlevel)
		{
			_path = path;
			_compressionlevel = compressionlevel;

			_svc = new SevenZip.SevenZipCompressor { ArchiveFormat = SevenZip.OutArchiveFormat.Zip };

			switch (compressionlevel)
			{
				default:
				case 0:
					_svc.CompressionLevel = SevenZip.CompressionLevel.None;
					break;
				case 1:
				case 2:
					_svc.CompressionLevel = SevenZip.CompressionLevel.Fast;
					break;
				case 3:
				case 4:
					_svc.CompressionLevel = SevenZip.CompressionLevel.Low;
					break;
				case 5:
				case 6:
					_svc.CompressionLevel = SevenZip.CompressionLevel.Normal;
					break;
				case 7:
				case 8:
					_svc.CompressionLevel = SevenZip.CompressionLevel.High;
					break;
				case 9:
					_svc.CompressionLevel = SevenZip.CompressionLevel.Ultra;
					break;
			}
		}

		public void WriteItem(string name, Action<Stream> callback)
		{
			var dict = new Dictionary<string, Stream>();
			var r = new RangBuffer();
			dict[name] = r.R;
			if (_first)
			{
				_first = false;
				_svc.CompressionMode = SevenZip.CompressionMode.Create;
			}
			else
			{
				_svc.CompressionMode = SevenZip.CompressionMode.Append;
			}

			var task = Task.Factory.StartNew(() =>
				{
					_svc.CompressStreamDictionary(dict, _path);
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
