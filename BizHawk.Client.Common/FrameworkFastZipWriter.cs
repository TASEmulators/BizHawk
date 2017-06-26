using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// this almost works, but it loses all of its speed advantages over FrameworkZipWriter from slow CRC calculation.
	/// </summary>
	public class FrameworkFastZipWriter : IZipWriter
	{
		private Stream _output;
		private readonly CompressionLevel _level;

		private byte[] _localHeader;
		private List<byte[]> _endBlobs = new List<byte[]>();
		private byte[] _fileHeaderTemplate;
		private int _numEntries;
		private bool _disposed;

		private class CRC32Stream : Stream
		{
			// Lookup table for speed.
			private static readonly uint[] Crc32Table;

			static CRC32Stream()
			{
				Crc32Table = new uint[256];
				for (uint i = 0; i < 256; ++i)
				{
					uint crc = i;
					for (int j = 8; j > 0; --j)
					{
						if ((crc & 1) == 1)
						{
							crc = (crc >> 1) ^ 0xEDB88320;
						}
						else
						{
							crc >>= 1;
						}
					}

					Crc32Table[i] = crc;
				}
			}

			private uint _crc = 0xffffffff;
			private int _count = 0;
			private Stream _baseStream;

			public int Size => _count;
			public uint Crc => ~_crc;

			public CRC32Stream(Stream baseStream)
			{
				_baseStream = baseStream;
			}

			private void CalculateByte(byte b)
			{
				_crc = (_crc >> 8) ^ Crc32Table[b ^ (_crc & 0xff)];
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				for (int i = offset; i < offset + count; i++)
				{
					CalculateByte(buffer[i]);
				}
				_count += count;
				_baseStream.Write(buffer, offset, count);
			}

			public override void WriteByte(byte value)
			{
				CalculateByte(value);
				_count++;
				_baseStream.WriteByte(value);
			}

			public override void Flush()
			{
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override bool CanRead => false;

			public override bool CanSeek => false;

			public override bool CanWrite => true;

			public override long Length
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public override long Position
			{
				get
				{
					throw new NotImplementedException();
				}

				set
				{
					throw new NotImplementedException();
				}
			}
		}

		public FrameworkFastZipWriter(string path, int compressionlevel)
		{
			_output = new FileStream(path, FileMode.Create, FileAccess.Write);
			if (compressionlevel == 0)
				throw new NotImplementedException();
			//_level = CompressionLevel.NoCompression;
			else if (compressionlevel < 5)
				_level = CompressionLevel.Fastest;
			else
				_level = CompressionLevel.Optimal;

			var dt = DateTime.Now;
			var mtime = dt.Second >> 1
				| dt.Minute << 5
				| dt.Hour << 11;
			var mdate = dt.Day
				| dt.Month << 5
				| (dt.Year - 1980) << 9;

			var modifiedDate = new byte[]
			{
				(byte)(mtime & 0xff),
				(byte)(mtime >> 8),
				(byte)(mdate & 0xff),
				(byte)(mdate >> 8)
			};

			_localHeader = new byte[]
			{
				0x50, 0x4b, 0x03, 0x04, // signature
				0x14, 0x00, // version
				0x08, 0x00, // flags: has data descriptor
				0x08, 0x00, // method: deflate
				modifiedDate[0], modifiedDate[1], // mod time
				modifiedDate[2], modifiedDate[3], // mod date
				0x00, 0x00, 0x00, 0x00, // crc32
				0x00, 0x00, 0x00, 0x00, // compressed size
				0x00, 0x00, 0x00, 0x00, // uncompressed size
				0x00, 0x00, // filename length
				0x00, 0x00, // extra field length
			};

			_fileHeaderTemplate = new byte[]
			{
				0x50, 0x4b, 0x01, 0x02, // signature
				0x17, 0x03, // ??
				0x14, 0x00, // version
				0x08, 0x00, // flags: has data descriptor
				0x08, 0x00, // method: deflate
				modifiedDate[0], modifiedDate[1], // mod time
				modifiedDate[2], modifiedDate[3], // mod date
				0x00, 0x00, 0x00, 0x00, // crc32
				0x00, 0x00, 0x00, 0x00, // compressed size
				0x00, 0x00, 0x00, 0x00, // uncompressed size
				0x00, 0x00, // filename length
				0x00, 0x00, // extra field length
				0x00, 0x00, // file comment length
				0x00, 0x00, // disk #,
				0x00, 0x00, // internal attributes
				0x00, 0x00, 0x00, 0x00, // external attributes
				0x00, 0x00, 0x00, 0x00, // local header offset
			};
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				WriteFooter();
				_output.Dispose();
				_output = null;
				_disposed = true;
			}
		}

		public void WriteItem(string name, Action<Stream> callback)
		{
			var nameb = Encoding.ASCII.GetBytes(name);
			_localHeader[26] = (byte)nameb.Length;
			_localHeader[27] = (byte)(nameb.Length >> 8);

			var localHeaderOffset = (int)(_output.Position);

			_output.Write(_localHeader, 0, _localHeader.Length);
			_output.Write(nameb, 0, nameb.Length);

			var fileStart = (int)(_output.Position);

			var s2 = new DeflateStream(_output, _level, true);
			var s3 = new CRC32Stream(s2);
			callback(s3);
			s2.Flush();

			var fileEnd = (int)(_output.Position);

			var crc = s3.Crc;
			var compressedSize = fileEnd - fileStart;
			var uncompressedSize = s3.Size;
			var descriptor = new byte[]
			{
				(byte)crc,
				(byte)(crc >> 8),
				(byte)(crc >> 16),
				(byte)(crc >> 24),
				(byte)compressedSize,
				(byte)(compressedSize >> 8),
				(byte)(compressedSize >> 16),
				(byte)(compressedSize >> 24),
				(byte)uncompressedSize,
				(byte)(uncompressedSize >> 8),
				(byte)(uncompressedSize >> 16),
				(byte)(uncompressedSize >> 24)
			};
			_output.Write(descriptor, 0, descriptor.Length);

			var fileHeader = (byte[])_fileHeaderTemplate.Clone();

			fileHeader[28] = (byte)nameb.Length;
			fileHeader[29] = (byte)(nameb.Length >> 8);
			Array.Copy(descriptor, 0, fileHeader, 16, 12);
			fileHeader[42] = (byte)localHeaderOffset;
			fileHeader[43] = (byte)(localHeaderOffset >> 8);
			fileHeader[44] = (byte)(localHeaderOffset >> 16);
			fileHeader[45] = (byte)(localHeaderOffset >> 24);

			_endBlobs.Add(fileHeader);
			_endBlobs.Add(nameb);
			_numEntries++;
		}

		private void WriteFooter()
		{
			var centralHeaderOffset = (int)(_output.Position);

			foreach (var blob in _endBlobs)
				_output.Write(blob, 0, blob.Length);

			var centralHeaderEnd = (int)(_output.Position);

			var centralHeaderSize = centralHeaderEnd - centralHeaderOffset;

			var footer = new byte[]
			{
				0x50, 0x4b, 0x05, 0x06, // signature
				0x00, 0x00, // disk number
				0x00, 0x00, // central record disk number
				(byte)_numEntries, (byte)(_numEntries >> 8), // number of entries on disk
				(byte)_numEntries, (byte)(_numEntries >> 8), // number of entries total
				(byte)centralHeaderSize,
				(byte)(centralHeaderSize >> 8),
				(byte)(centralHeaderSize >> 16),
				(byte)(centralHeaderSize >> 24), // central directory size
				(byte)centralHeaderOffset,
				(byte)(centralHeaderOffset >> 8),
				(byte)(centralHeaderOffset >> 16),
				(byte)(centralHeaderOffset >> 24), // central directory offset
				0x07, 0x00, // comment length
				0x42, 0x69, 0x7a, 0x48, 0x61, 0x77, 0x6b // comment
			};

			_output.Write(footer, 0, footer.Length);
		}
	}
}
