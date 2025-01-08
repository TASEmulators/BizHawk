using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// implements a simple muxer for the NUT media format
	/// http://ffmpeg.org/~michael/nut.txt
	/// </summary>
	public class NutMuxer
	{
		// this code isn't really any good for general purpose nut creation

		public class ReusableBufferPool<T>
		{
			private readonly List<T[]> _available = new List<T[]>();
			private readonly ICollection<T[]> _inUse = new HashSet<T[]>();

			private readonly int _capacity;

			/// <param name="capacity">total number of buffers to keep around</param>
			public ReusableBufferPool(int capacity)
			{
				_capacity = capacity;
			}

			private T[] GetBufferInternal(int length, bool zerofill, Predicate<T[]> criteria)
			{
				if (_inUse.Count == _capacity)
				{
					throw new InvalidOperationException();
				}

				var candidate = _available.Find(criteria);
				if (candidate == null)
				{
					if (_available.Count + _inUse.Count == _capacity)
					{
						// out of space! should not happen often
						Console.WriteLine("Purging");
						_available.Clear();
					}
					candidate = new T[length];
				}
				else
				{
					if (zerofill)
					{
						Array.Clear(candidate, 0, candidate.Length);
					}

					_available.Remove(candidate);
				}

				_inUse.Add(candidate);
				return candidate;
			}

			public T[] GetBuffer(int length, bool zerofill = false)
			{
				return GetBufferInternal(length, zerofill, a => a.Length == length);
			}

			public T[] GetBufferAtLeast(int length, bool zerofill = false)
			{
				return GetBufferInternal(length, zerofill, a => a.Length >= length && a.Length / (float)length <= 2.0f);
			}

			/// <exception cref="ArgumentException"><paramref name="buffer"/> is not in use</exception>
			public void ReleaseBuffer(T[] buffer)
			{
				if (!_inUse.Remove(buffer)) throw new ArgumentException(message: "already released?", paramName: nameof(buffer));
				_available.Add(buffer);
			}
		}

		/// <summary>
		/// variable length value, unsigned
		/// </summary>
		private static void WriteVarU(ulong v, Stream stream)
		{
			byte[] b = new byte[10];
			int i = 0;
			do
			{
				if (i > 0)
					b[i++] = (byte)((v & 127) | 128);
				else
					b[i++] = (byte)(v & 127);
				v /= 128;
			} while (v > 0);

			for (; i > 0; i--)
			{
				stream.WriteByte(b[i - 1]);
			}
		}

		/// <summary>
		/// variable length value, unsigned
		/// </summary>
		private static void WriteVarU(int v, Stream stream)
		{
			if (v < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(v), "unsigned must be non-negative");
			}

			WriteVarU((ulong)v, stream);
		}

		/// <summary>
		/// variable length value, unsigned
		/// </summary>
		private static void WriteVarU(long v, Stream stream)
		{
			if (v < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(v), "unsigned must be non-negative");
			}

			WriteVarU((ulong)v, stream);
		}

		/// <summary>
		/// utf-8 string with length prepended
		/// </summary>
		private static void WriteString(string s, Stream stream)
		{
			WriteBytes(Encoding.UTF8.GetBytes(s), stream);
		}

		/// <summary>
		/// arbitrary sequence of bytes with length prepended
		/// </summary>
		private static void WriteBytes(byte[] b, Stream stream)
		{
			WriteVarU(b.Length, stream);
			stream.Write(b, 0, b.Length);
		}

		/// <summary>
		/// big endian 64 bit unsigned
		/// </summary>
		private static void WriteBe64(ulong v, Stream stream)
		{
			byte[] b = new byte[8];
			for (int i = 7; i >= 0; i--)
			{
				b[i] = (byte)(v & 255);
				v >>= 8;
			}

			stream.Write(b, 0, 8);
		}

		/// <summary>
		/// big endian 32 bit unsigned
		/// </summary>
		private static void WriteBe32(uint v, Stream stream)
		{
			byte[] b = new byte[4];
			for (int i = 3; i >= 0; i--)
			{
				b[i] = (byte)(v & 255);
				v >>= 8;
			}

			stream.Write(b, 0, 4);
		}

		private static readonly uint[] CrcTable =
		{
			0x00000000, 0x04C11DB7, 0x09823B6E, 0x0D4326D9,
			0x130476DC, 0x17C56B6B, 0x1A864DB2, 0x1E475005,
			0x2608EDB8, 0x22C9F00F, 0x2F8AD6D6, 0x2B4BCB61,
			0x350C9B64, 0x31CD86D3, 0x3C8EA00A, 0x384FBDBD
		};

		/// <summary>
		/// seems to be different than standard CRC32?????
		/// </summary>
		/// <returns>crc32, nut variant</returns>
		private static uint NutCRC32(byte[] buf)
		{
			uint crc = 0;
			foreach (var b in buf)
			{
				crc ^= (uint)b << 24;
				crc = (crc << 4) ^ CrcTable[crc >> 28];
				crc = (crc << 4) ^ CrcTable[crc >> 28];
			}
			return crc;
		}

		/// <summary>
		/// writes a single packet out, including CheckSums
		/// </summary>
		private class NutPacket : Stream
		{
			public enum StartCode : ulong
			{
				// where tf does this incantation come from --yoshi
				Main = 0x4E4D_7A56_1F5F_04AD,
				Stream = 0x4E53_1140_5BF2_F9DB,
				Syncpoint = 0x4E4B_E4AD_EECA_4569,
				Index = 0x4E58_DD67_2F23_E64E,
				Info = 0x4E49_AB68_B596_BA78,
			}

			private MemoryStream _data;
			private readonly StartCode _startCode;
			private readonly Stream _underlying;

			/// <summary>
			/// create a new NutPacket
			/// </summary>
			/// <param name="startCode">startCode for this packet</param>
			/// <param name="underlying">stream to write to</param>
			public NutPacket(StartCode startCode, Stream underlying)
			{
				_data = new MemoryStream();
				_startCode = startCode;
				_underlying = underlying;
			}

			public override bool CanRead => false;

			public override bool CanSeek => false;

			public override bool CanWrite => true;

			/// <summary>
			/// write data out to underlying stream, including header, footer, checksums
			/// this cannot be done more than once!
			/// </summary>
			public override void Flush()
			{
				// first, prep header
				var header = new MemoryStream();
				WriteBe64((ulong)_startCode, header);
				WriteVarU(_data.Length + 4, header); // +4 for checksum
				if (_data.Length > 4092)
				{
					WriteBe32(NutCRC32(header.ToArray()), header);
				}

				var tmp = header.ToArray();
				_underlying.Write(tmp, 0, tmp.Length);

				tmp = _data.ToArray();
				_underlying.Write(tmp, 0, tmp.Length);
				WriteBe32(NutCRC32(tmp), _underlying);

				_data = null;
			}
			
			public override long Length => throw new NotImplementedException();

			public override long Position
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}
			
			public override void Write(byte[] buffer, int offset, int count)
			{
				_data.Write(buffer, offset, count);
			}
		}

		/// <summary>
		/// stores basic AV parameters
		/// </summary>
		private class AVParams
		{
			public int Width { get; set; }
			public int Height { get; set; }
			public int Samplerate { get; set; }
			public int FpsNum { get; set; }
			public int FpsDen { get; set; }
			public int Channels { get; set; }

			/// <summary>
			/// puts fpsNum, fpsDen in lowest terms
			/// </summary>
			public void Reduce()
			{
				int gcd = (int)BigInteger.GreatestCommonDivisor(new BigInteger(FpsNum), new BigInteger(FpsDen));
				FpsNum /= gcd;
				FpsDen /= gcd;
			}
		}

		// stores basic AV parameters
		private readonly AVParams _avParams;

		// target output for nut stream
		private Stream _output;

		// PTS of video stream.  timebase is 1/framerate, so this is equal to number of frames
		private ulong _videoOpts;

		// PTS of audio stream.  timebase is 1/samplerate, so this is equal to number of samples
		private ulong _audioPts;

		// has EOR been written on this stream?
		private bool _videoDone;
		
		// has EOR been written on this stream?
		private bool _audioDone;

		// video packets waiting to be written
		private readonly Queue<NutFrame> _videoQueue;
		
		// audio packets waiting to be written
		private readonly Queue<NutFrame> _audioQueue;

		private readonly ReusableBufferPool<byte> _bufferPool = new ReusableBufferPool<byte>(12);

		/// <summary>
		/// write out the main header
		/// </summary>
		private void WriteMainHeader()
		{
			// note: this file starttag not actually part of main headers
			var tmp = Encoding.ASCII.GetBytes("nut/multimedia container\0");
			_output.Write(tmp, 0, tmp.Length);

			var header = new NutPacket(NutPacket.StartCode.Main, _output);

			WriteVarU(3, header); // version
			WriteVarU(2, header); // stream_count
			WriteVarU(65536, header); // max_distance

			WriteVarU(2, header); // time_base_count
			// timebase is length of single frame, so reversed num+den is intentional
			WriteVarU(_avParams.FpsDen, header); // time_base_num[0]
			WriteVarU(_avParams.FpsNum, header); // time_base_den[0]
			WriteVarU(1, header); // time_base_num[1]
			WriteVarU(_avParams.Samplerate, header); // time_base_den[1]

			// frame flag compression is ignored for simplicity
			for (int i = 0; i < 255; i++) // not 256 because entry 0x4e is skipped (as it would indicate a startcode)
			{
				WriteVarU(1 << 12, header); // tmp_flag = FLAG_CODED
				WriteVarU(0, header); // tmp_fields
			}

			// header compression ignored because it's not useful to us
			WriteVarU(0, header); // header_count_minus1

			// BROADCAST_MODE only useful for realtime transmission clock recovery
			WriteVarU(0, header); // main_flags

			header.Flush();
		}

		// write out the 0th stream header (video)
		private void WriteVideoHeader()
		{
			var header = new NutPacket(NutPacket.StartCode.Stream, _output);
			WriteVarU(0, header); // stream_id
			WriteVarU(0, header); // stream_class = video
			WriteString("BGRA", header); // fourcc = "BGRA"
			WriteVarU(0, header); // time_base_id = 0
			WriteVarU(8, header); // msb_pts_shift
			WriteVarU(1, header); // max_pts_distance
			WriteVarU(0, header); // decode_delay
			WriteVarU(1, header); // stream_flags = FLAG_FIXED_FPS
			WriteBytes(Array.Empty<byte>(), header); // codec_specific_data

			// stream_class = video
			WriteVarU(_avParams.Width, header); // width
			WriteVarU(_avParams.Height, header); // height
			WriteVarU(1, header); // sample_width
			WriteVarU(1, header); // sample_height
			WriteVarU(18, header); // colorspace_type = full range rec709 (avisynth's "PC.709")

			header.Flush();
		}

		// write out the 1st stream header (audio)
		private void WriteAudioHeader()
		{
			var header = new NutPacket(NutPacket.StartCode.Stream, _output);
			WriteVarU(1, header); // stream_id
			WriteVarU(1, header); // stream_class = audio
			WriteString("\x01\x00\x00\x00", header); // fourcc = 01 00 00 00
			WriteVarU(1, header); // time_base_id = 1
			WriteVarU(8, header); // msb_pts_shift
			WriteVarU(_avParams.Samplerate, header); // max_pts_distance
			WriteVarU(0, header); // decode_delay
			WriteVarU(0, header); // stream_flags = none; no FIXED_FPS because we aren't guaranteeing same-size audio chunks
			WriteBytes(Array.Empty<byte>(), header); // codec_specific_data

			// stream_class = audio
			WriteVarU(_avParams.Samplerate, header); // samplerate_num
			WriteVarU(1, header); // samplerate_den
			WriteVarU(_avParams.Channels, header); // channel_count

			header.Flush();
		}

		/// <summary>
		/// stores a single frame with syncpoint, in mux-ready form
		/// used because reordering of audio and video can be needed for proper interleave
		/// </summary>
		private class NutFrame
		{
			/// <summary>
			/// data ready to be written to stream/disk
			/// </summary>
			private readonly byte[] _data;

			/// <summary>
			/// valid length of the data
			/// </summary>
			private readonly int _actualLength;

			/// <summary>
			/// presentation timestamp
			/// </summary>
			private readonly ulong _pts;

			/// <summary>
			/// fraction of the specified timebase
			/// </summary>
			private readonly ulong _ptsNum;

			/// <summary>
			/// fraction of the specified timebase
			/// </summary>
			private readonly ulong _ptsDen;

			private readonly ReusableBufferPool<byte> _pool;

			/// <param name="payload">frame data</param>
			/// <param name="payLoadLen">actual length of frame data</param>
			/// <param name="pts">presentation timestamp</param>
			/// <param name="ptsNum">numerator of timebase</param>
			/// <param name="ptsDen">denominator of timebase</param>
			/// <param name="ptsIndex">which timestamp base is used, assumed to be also stream number</param>
			public NutFrame(byte[] payload, int payLoadLen, ulong pts, ulong ptsNum, ulong ptsDen, int ptsIndex, ReusableBufferPool<byte> pool)
			{
				_pts = pts;
				_ptsNum = ptsNum;
				_ptsDen = ptsDen;

				_pool = pool;
				_data = pool.GetBufferAtLeast(payLoadLen + 2048);
				var frame = new MemoryStream(_data);

				// create syncpoint
				var sync = new NutPacket(NutPacket.StartCode.Syncpoint, frame);
				WriteVarU(pts * 2 + (ulong)ptsIndex, sync); // global_key_pts
				WriteVarU(1, sync); // back_ptr_div_16, this is wrong
				sync.Flush();


				var frameHeader = new MemoryStream();
				frameHeader.WriteByte(0); // frame_code

				// frame_flags = FLAG_CODED, so:
				int flags = 0;
				flags |= 1 << 0; // FLAG_KEY
				if (payLoadLen == 0)
				{
					flags |= 1 << 1; // FLAG_EOR
				}

				flags |= 1 << 3; // FLAG_CODED_PTS
				flags |= 1 << 4; // FLAG_STREAM_ID
				flags |= 1 << 5; // FLAG_SIZE_MSB
				flags |= 1 << 6; // FLAG_CHECKSUM
				WriteVarU(flags, frameHeader);
				WriteVarU(ptsIndex, frameHeader); // stream_id
				WriteVarU(pts + 256, frameHeader); // coded_pts = pts + 1 << msb_pts_shift
				WriteVarU(payLoadLen, frameHeader); // data_size_msb

				var frameHeaderArr = frameHeader.ToArray();
				frame.Write(frameHeaderArr, 0, frameHeaderArr.Length);
				WriteBe32(NutCRC32(frameHeaderArr), frame); // checksum
				frame.Write(payload, 0, payLoadLen);

				_actualLength = (int)frame.Position;
			}

			/// <summary>
			/// compare two NutFrames by pts
			/// </summary>
			public static bool operator <=(NutFrame lhs, NutFrame rhs)
			{
				BigInteger left = new BigInteger(lhs._pts);
				left = left * lhs._ptsNum * rhs._ptsDen;
				BigInteger right = new BigInteger(rhs._pts);
				right = right * rhs._ptsNum * lhs._ptsDen;

				return left <= right;
			}
			public static bool operator >=(NutFrame lhs, NutFrame rhs)
			{
				BigInteger left = new BigInteger(lhs._pts);
				left = left * lhs._ptsNum * rhs._ptsDen;
				BigInteger right = new BigInteger(rhs._pts);
				right = right * rhs._ptsNum * lhs._ptsDen;

				return left >= right;
			}

			/// <summary>
			/// write out frame, with syncpoint and all headers
			/// </summary>
			public void WriteData(Stream dest)
			{
				dest.Write(_data, 0, _actualLength);
				_pool.ReleaseBuffer(_data);
			}
		}

		/// <summary>write a video frame to the stream</summary>
		/// <param name="video">raw video data; if length 0, write EOR</param>
		/// <exception cref="Exception">internal error, possible A/V desync</exception>
		/// <exception cref="InvalidOperationException">already written EOR</exception>
		public void WriteVideoFrame(ReadOnlySpan<int> video)
		{
			if (_videoDone)
				throw new InvalidOperationException("Can't write data after end of relevance!");
			if (_audioQueue.Count > 5)
				throw new Exception("A/V Desync?");
			var dataLen = video.Length * sizeof(int);
			var data = _bufferPool.GetBufferAtLeast(dataLen);
			MemoryMarshal.AsBytes(video).CopyTo(data.AsSpan(0, dataLen));
			if (dataLen == 0)
			{
				_videoDone = true;
			}

			var f = new NutFrame(data, dataLen, _videoOpts, (ulong) _avParams.FpsDen, (ulong) _avParams.FpsNum, 0, _bufferPool);
			_bufferPool.ReleaseBuffer(data);
			_videoOpts++;
			_videoQueue.Enqueue(f);
			while (_audioQueue.Count > 0 && f >= _audioQueue.Peek())
			{
				_audioQueue.Dequeue().WriteData(_output);
			}
		}

		/// <summary>write an audio frame to the stream</summary>
		/// <param name="samples">raw audio data; if length 0, write EOR</param>
		/// <exception cref="Exception">internal error, possible A/V desync</exception>
		/// <exception cref="InvalidOperationException">already written EOR</exception>
		public void WriteAudioFrame(short[] samples)
		{
			if (_audioDone)
			{
				throw new Exception("Can't write audio after end of relevance!");
			}

			if (_videoQueue.Count > 5)
			{
				throw new Exception("A/V Desync?");
			}

			int dataLen = samples.Length * sizeof(short);
			byte[] data = _bufferPool.GetBufferAtLeast(dataLen);
			Buffer.BlockCopy(samples, 0, data, 0, dataLen);
			if (dataLen == 0)
			{
				_audioDone = true;
			}

			var f = new NutFrame(data, dataLen, _audioPts, 1, (ulong)_avParams.Samplerate, 1, _bufferPool);
			_bufferPool.ReleaseBuffer(data);
			_audioPts += (ulong)samples.Length / (ulong)_avParams.Channels;
			_audioQueue.Enqueue(f);
			while (_videoQueue.Count > 0 && f >= _videoQueue.Peek())
			{
				_videoQueue.Dequeue().WriteData(_output);
			}
		}

		/// <summary>
		/// create a new NutMuxer
		/// </summary>
		/// <param name="width">video width</param>
		/// <param name="height">video height</param>
		/// <param name="fpsNum">fps numerator</param>
		/// <param name="fpsDen">fps denominator</param>
		/// <param name="samplerate">audio samplerate</param>
		/// <param name="channels">audio number of channels</param>
		/// <param name="underlying">Stream to write to</param>
		public NutMuxer(int width, int height, int fpsNum, int fpsDen, int samplerate, int channels, Stream underlying)
		{
			_avParams = new AVParams
			{
				Width = width,
				Height = height,
				FpsNum = fpsNum,
				FpsDen = fpsDen
			};

			_avParams.Reduce(); // TimeBases in nut MUST be relatively prime
			_avParams.Samplerate = samplerate;
			_avParams.Channels = channels;
			_output = underlying;

			_audioPts = 0;
			_videoOpts = 0;

			_audioQueue = new Queue<NutFrame>();
			_videoQueue = new Queue<NutFrame>();

			WriteMainHeader();
			WriteVideoHeader();
			WriteAudioHeader();

			_videoDone = false;
			_audioDone = false;
		}

		/// <summary>
		/// finish and flush everything
		/// closes underlying stream!!
		/// </summary>
		public void Finish()
		{
			if (!_videoDone)
			{
				WriteVideoFrame([ ]);
			}

			if (!_audioDone)
			{
				WriteAudioFrame([ ]);
			}

			// flush any remaining queued packets
			while (_audioQueue.Count > 0 && _videoQueue.Count > 0)
			{
				if (_audioQueue.Peek() <= _videoQueue.Peek())
				{
					_audioQueue.Dequeue().WriteData(_output);
				}
				else
				{
					_videoQueue.Dequeue().WriteData(_output);
				}
			}

			while (_audioQueue.Count > 0)
			{
				_audioQueue.Dequeue().WriteData(_output);
			}

			while (_videoQueue.Count > 0)
			{
				_videoQueue.Dequeue().WriteData(_output);
			}

			_output.Close();
			_output = null;
		}
	}
}
