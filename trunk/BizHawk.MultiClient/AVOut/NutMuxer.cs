using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Numerics;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// implements a simple muxer for the NUT media format
	/// http://ffmpeg.org/~michael/nut.txt
	/// </summary>
	class NutMuxer
	{
		// this code isn't really any good for general purpose nut creation


		/// <summary>
		/// variable length value, unsigned
		/// </summary>
		static void WriteVarU(ulong v, Stream stream)
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
				stream.WriteByte(b[i - 1]);
		}

		/// <summary>
		/// variable length value, unsigned
		/// </summary>
		static void WriteVarU(int v, Stream stream)
		{
			if (v < 0)
				throw new ArgumentOutOfRangeException("unsigned must be non-negative");
			WriteVarU((ulong)v, stream);
		}

		/// <summary>
		/// variable length value, unsigned
		/// </summary>
		static void WriteVarU(long v, Stream stream)
		{
			if (v < 0)
				throw new ArgumentOutOfRangeException("unsigned must be non-negative");
			WriteVarU((ulong)v, stream);
		}

		/// <summary>
		/// variable length value, signed
		/// </summary>
		static void WriteVarS(long v, Stream stream)
		{
			ulong temp;
			if (v < 0)
				temp = 1 + 2 * (ulong)(-v);
			else
				temp = 2 * (ulong)(v);
			WriteVarU(temp - 1, stream);
		}

		/// <summary>
		/// utf-8 string with length prepended
		/// </summary>
		static void WriteString(string s, Stream stream)
		{
			WriteBytes(Encoding.UTF8.GetBytes(s), stream);
		}

		/// <summary>
		/// arbitrary sequence of bytes with length prepended
		/// </summary>
		static void WriteBytes(byte[] b, Stream stream)
		{
			WriteVarU(b.Length, stream);
			stream.Write(b, 0, b.Length);
		}

		/// <summary>
		/// big endian 64 bit unsigned
		/// </summary>
		/// <param name="v"></param>
		/// <param name="stream"></param>
		static void WriteBE64(ulong v, Stream stream)
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
		/// <param name="v"></param>
		/// <param name="stream"></param>
		static void WriteBE32(uint v, Stream stream)
		{
			byte[] b = new byte[4];
			for (int i = 3; i >= 0; i--)
			{
				b[i] = (byte)(v & 255);
				v >>= 8;
			}
			stream.Write(b, 0, 4);
		}
		/// <summary>
		/// big endian 32 bit unsigned
		/// </summary>
		/// <param name="v"></param>
		/// <param name="stream"></param>
		static void WriteBE32(int v, Stream stream)
		{
			byte[] b = new byte[4];
			for (int i = 3; i >= 0; i--)
			{
				b[i] = (byte)(v & 255);
				v >>= 8;
			}
			stream.Write(b, 0, 4);
		}

		static readonly uint[] CRCtable = new uint[]
		{
			0x00000000, 0x04C11DB7, 0x09823B6E, 0x0D4326D9,
			0x130476DC, 0x17C56B6B, 0x1A864DB2, 0x1E475005,
			0x2608EDB8, 0x22C9F00F, 0x2F8AD6D6, 0x2B4BCB61,
			0x350C9B64, 0x31CD86D3, 0x3C8EA00A, 0x384FBDBD,
		};

		/// <summary>
		/// seems to be different than standard CRC32?????
		/// </summary>
		/// <param name="buf"></param>
		/// <returns>crc32, nut variant</returns>
		static uint NutCRC32(byte[] buf)
		{
			uint crc = 0;
			for (int i = 0; i < buf.Length; i++)
			{
				crc ^= (uint)buf[i] << 24;
				crc = (crc << 4) ^ CRCtable[crc >> 28];
				crc = (crc << 4) ^ CRCtable[crc >> 28];
			}
			return crc;
		}



		/// <summary>
		/// writes a single packet out, including checksums
		/// </summary>
		class NutPacket : Stream
		{
			public enum StartCode : ulong
			{
				Main = 0x4e4d7a561f5f04ad,
				Stream = 0x4e5311405bf2f9db,
				Syncpoint = 0x4e4be4adeeca4569,
				Index = 0x4e58dd672f23e64e,
				Info = 0x4e49ab68b596ba78
			};

			MemoryStream data;
			StartCode startcode;
			Stream underlying;

			/// <summary>
			/// create a new NutPacket
			/// </summary>
			/// <param name="startcode">startcode for this packet</param>
			/// <param name="underlying">stream to write to</param>
			public NutPacket(StartCode startcode, Stream underlying)
			{
				data = new MemoryStream();
				this.startcode = startcode;
				this.underlying = underlying;
			}


			public override bool CanRead
			{
				get { return false; }
			}

			public override bool CanSeek
			{
				get { return false; }
			}

			public override bool CanWrite
			{
				get { return true; }
			}

			/// <summary>
			/// write data out to underlying stream, including header, footer, checksums
			/// this cannot be done more than once!
			/// </summary>
			public override void Flush()
			{
				// first, prep header
				var header = new MemoryStream();
				WriteBE64((ulong)startcode, header);
				WriteVarU(data.Length + 4, header); // +4 for checksum
				if (data.Length > 4092)
					WriteBE32(NutCRC32(header.ToArray()), header);
				var tmp = header.ToArray();
				underlying.Write(tmp, 0, tmp.Length);

				tmp = data.ToArray();
				underlying.Write(tmp, 0, tmp.Length);
				WriteBE32(NutCRC32(tmp), underlying);

				data = null;
			}
			
			public override long Length
			{
				get { throw new NotImplementedException(); }
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
				data.Write(buffer, offset, count);
			}
		}


		/// <summary>
		/// stores basic AV parameters
		/// </summary>
		class AVParams
		{
			public int width, height, samplerate, fpsnum, fpsden, channels;
			/// <summary>
			/// puts fpsnum, fpsden in lowest terms
			/// </summary>
			public void Reduce()
			{
				int gcd = (int) BigInteger.GreatestCommonDivisor(new BigInteger(fpsnum), new BigInteger(fpsden));
				fpsnum /= gcd;
				fpsden /= gcd;
			}
		}

		/// <summary>
		/// stores basic AV parameters
		/// </summary>
		AVParams avparams;

		/// <summary>
		/// target output for nut stream
		/// </summary>
		Stream output;

		/// <summary>
		/// PTS of video stream.  timebase is 1/framerate, so this is equal to number of frames
		/// </summary>
		ulong videopts;

		/// <summary>
		/// PTS of audio stream.  timebase is 1/samplerate, so this is equal to number of samples
		/// </summary>
		ulong audiopts;

		/// <summary>
		/// has EOR been writen on this stream?
		/// </summary>
		bool videodone;
		/// <summary>
		/// has EOR been written on this stream?
		/// </summary>
		bool audiodone;

		/// <summary>
		/// video packets waiting to be written
		/// </summary>
		Queue<NutFrame> videoqueue;
		/// <summary>
		/// audio packets waiting to be written
		/// </summary>
		Queue<NutFrame> audioqueue;


		/// <summary>
		/// write out the main header
		/// </summary>
		void writemainheader()
		{
			// note: this file starttag not actually part of main headers
			var tmp = Encoding.ASCII.GetBytes("nut/multimedia container\0");
			output.Write(tmp, 0, tmp.Length);

			var header = new NutPacket(NutPacket.StartCode.Main, output);

			WriteVarU(3, header); // version
			WriteVarU(2, header); // stream_count
			WriteVarU(65536, header); // max_distance

			WriteVarU(2, header); // time_base_count
			// timebase is length of single frame, so reversed num+den is intentional
			WriteVarU(avparams.fpsden, header); // time_base_num[0]
			WriteVarU(avparams.fpsnum, header); // time_base_den[0]
			WriteVarU(1, header); // time_base_num[1]
			WriteVarU(avparams.samplerate, header); // time_base_den[1]

			// frame flag compression is ignored for simplicity
			for (int i = 0; i < 255; i++) // not 256 because entry 0x4e is skipped (as it would indicate a startcode)
			{
				WriteVarU((1 << 12), header); // tmp_flag = FLAG_CODED
				WriteVarU(0, header); // tmp_fields
			}

			// header compression ignored because it's not useful to us
			WriteVarU(0, header); // header_count_minus1

			// BROADCAST_MODE only useful for realtime transmission clock recovery
			WriteVarU(0, header); // main_flags

			header.Flush();
		}

		/// <summary>
		/// write out the 0th stream header (video)
		/// </summary>
		void writevideoheader()
		{
			var header = new NutPacket(NutPacket.StartCode.Stream, output);
			WriteVarU(0, header); // stream_id
			WriteVarU(0, header); // stream_class = video
			WriteString("BGRA", header); // fourcc = "BGRA"
			WriteVarU(0, header); // time_base_id = 0
			WriteVarU(8, header); // msb_pts_shift
			WriteVarU(1, header); // max_pts_distance
			WriteVarU(0, header); // decode_delay
			WriteVarU(1, header); // stream_flags = FLAG_FIXED_FPS
			WriteBytes(new byte[0], header); // codec_specific_data

			// stream_class = video
			WriteVarU(avparams.width, header); // width
			WriteVarU(avparams.height, header); // height
			WriteVarU(1, header); // sample_width
			WriteVarU(1, header); // sample_height
			WriteVarU(18, header); // colorspace_type = full range rec709 (avisynth's "PC.709")

			header.Flush();
		}

		/// <summary>
		/// write out the 1st stream header (audio)
		/// </summary>
		void writeaudioheader()
		{
			var header = new NutPacket(NutPacket.StartCode.Stream, output);
			WriteVarU(1, header); // stream_id
			WriteVarU(1, header); // stream_class = audio
			WriteString("\x01\x00\x00\x00", header); // fourcc = 01 00 00 00
			WriteVarU(1, header); // time_base_id = 1
			WriteVarU(8, header); // msb_pts_shift
			WriteVarU(avparams.samplerate, header); // max_pts_distance
			WriteVarU(0, header); // decode_delay
			WriteVarU(0, header); // stream_flags = none; no FIXED_FPS because we aren't guaranteeing same-size audio chunks
			WriteBytes(new byte[0], header); // codec_specific_data

			// stream_class = audio
			WriteVarU(avparams.samplerate, header); // samplerate_num
			WriteVarU(1, header); // samplerate_den
			WriteVarU(avparams.channels, header); // channel_count

			header.Flush();
		}

		/// <summary>
		/// stores a single frame with syncpoint, in mux-ready form
		/// used because reordering of audio and video can be needed for proper interleave
		/// </summary>
		class NutFrame
		{
			/// <summary>
			/// data ready to be written to stream/disk
			/// </summary>
			byte[] data;

			/// <summary>
			/// presentation timestamp
			/// </summary>
			ulong pts;

			/// <summary>
			/// fraction of the specified timebase
			/// </summary>
			ulong ptsnum, ptsden;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="payload">frame data</param>
			/// <param name="pts">presentation timestamp</param>
			/// <param name="ptsnum">numerator of timebase</param>
			/// <param name="ptsden">denominator of timebase</param>
			/// <param name="ptsindex">which timestamp base is used, assumed to be also stream number</param>
			public NutFrame(byte[] payload, ulong pts, ulong ptsnum, ulong ptsden, int ptsindex)
			{
				this.pts = pts;
				this.ptsnum = ptsnum;
				this.ptsden = ptsden;

				var frame = new MemoryStream();

				// create syncpoint
				var sync = new NutPacket(NutPacket.StartCode.Syncpoint, frame);
				WriteVarU(pts * 2 + (ulong)ptsindex, sync); // global_key_pts
				WriteVarU(1, sync); // back_ptr_div_16, this is wrong
				sync.Flush();


				var frameheader = new MemoryStream();
				frameheader.WriteByte(0); // frame_code
				// frame_flags = FLAG_CODED, so:
				int flags = 0;
				flags |= 1 << 0; // FLAG_KEY
				if (payload.Length == 0)
					flags |= 1 << 1; // FLAG_EOR
				flags |= 1 << 3; // FLAG_CODED_PTS
				flags |= 1 << 4; // FLAG_STREAM_ID
				flags |= 1 << 5; // FLAG_SIZE_MSB
				flags |= 1 << 6; // FLAG_CHECKSUM
				WriteVarU(flags, frameheader);
				WriteVarU(ptsindex, frameheader); // stream_id
				WriteVarU(pts + 256, frameheader); // coded_pts = pts + 1 << msb_pts_shift
				WriteVarU(payload.Length, frameheader); // data_size_msb

				var frameheaderarr = frameheader.ToArray();
				frame.Write(frameheaderarr, 0, frameheaderarr.Length);
				WriteBE32(NutCRC32(frameheaderarr), frame); // checksum
				frame.Write(payload, 0, payload.Length);

				data = frame.ToArray();
			}

			/// <summary>
			/// compare two NutFrames by pts
			/// </summary>
			/// <param name="lhs"></param>
			/// <param name="rhs"></param>
			/// <returns></returns>
			public static bool operator <=(NutFrame lhs, NutFrame rhs)
			{
				BigInteger left = new BigInteger(lhs.pts);
				left = left * lhs.ptsnum * rhs.ptsden;
				BigInteger right = new BigInteger(rhs.pts);
				right = right * rhs.ptsnum * lhs.ptsden;

				return left <= right;
			}
			public static bool operator >=(NutFrame lhs, NutFrame rhs)
			{
				BigInteger left = new BigInteger(lhs.pts);
				left = left * lhs.ptsnum * rhs.ptsden;
				BigInteger right = new BigInteger(rhs.pts);
				right = right * rhs.ptsnum * lhs.ptsden;

				return left >= right;
			}


			static NutFrame()
			{
				//dbg = new StreamWriter(".\\nutframe.txt", false);
			}

			//static StreamWriter dbg;

			/// <summary>
			/// write out frame, with syncpoint and all headers
			/// </summary>
			/// <param name="dest"></param>
			public void WriteData(Stream dest)
			{
				dest.Write(data, 0, data.Length);
				//dbg.WriteLine(string.Format("{0},{1},{2}", pts, ptsnum, ptsden));
			}

		}


		/// <summary>
		/// write a video frame to the stream
		/// </summary>
		/// <param name="data">raw video data; if length 0, write EOR</param>
		public void writevideoframe(byte[] data)
		{
			if (videodone)
				throw new Exception("Can't write data after end of relevance!");
			if (data.Length == 0)
				videodone = true;
			var f = new NutFrame(data, videopts, (ulong) avparams.fpsden, (ulong) avparams.fpsnum, 0);
			videopts++;
			videoqueue.Enqueue(f);
			while (audioqueue.Count > 0 && f >= audioqueue.Peek())
				audioqueue.Dequeue().WriteData(output);
		}




		/// <summary>
		/// write an audio frame to the stream
		/// </summary>
		/// <param name="data">raw audio data; if length 0, write EOR</param>
		public void writeaudioframe(short[] samples)
		{
			if (audiodone)
				throw new Exception("Can't write audio after end of relevance!");
			byte[] data = new byte[samples.Length * sizeof (short)];
			Buffer.BlockCopy(samples, 0, data, 0, data.Length);
			if (data.Length == 0)
				audiodone = true;

			var f = new NutFrame(data, audiopts, 1, (ulong)avparams.samplerate, 1);
			audiopts += (ulong)samples.Length / (ulong)avparams.channels;
			audioqueue.Enqueue(f);
			while (videoqueue.Count > 0 && f >= videoqueue.Peek())
				videoqueue.Dequeue().WriteData(output);
		}

		/// <summary>
		/// create a new NutMuxer
		/// </summary>
		/// <param name="width">video width</param>
		/// <param name="height">video height</param>
		/// <param name="fpsnum">fps numerator</param>
		/// <param name="fpsden">fps denominator</param>
		/// <param name="samplerate">audio samplerate</param>
		/// <param name="channels">audio number of channels</param>
		/// <param name="underlying">Stream to write to</param>
		public NutMuxer(int width, int height, int fpsnum, int fpsden, int samplerate, int channels, Stream underlying)
		{
			avparams = new AVParams();
			avparams.width = width;
			avparams.height = height;
			avparams.fpsnum = fpsnum;
			avparams.fpsden = fpsden;
			avparams.Reduce(); // timebases in nut MUST be relatively prime
			avparams.samplerate = samplerate;
			avparams.channels = channels;
			output = underlying;

			audiopts = 0;
			videopts = 0;

			audioqueue = new Queue<NutFrame>();
			videoqueue = new Queue<NutFrame>();

			writemainheader();
			writevideoheader();
			writeaudioheader();

			videodone = false;
			audiodone = false;
		}

		/// <summary>
		/// finish and flush everything
		/// closes underlying stream!!
		/// </summary>
		public void Finish()
		{
			if (!videodone)
				writevideoframe(new byte[0]);
			if (!audiodone)
				writeaudioframe(new short[0]);

			// flush any remaining queued packets

			while (audioqueue.Count > 0 && videoqueue.Count > 0)
			{
				if (audioqueue.Peek() <= videoqueue.Peek())
					audioqueue.Dequeue().WriteData(output);
				else
					videoqueue.Dequeue().WriteData(output);
			}
			while (audioqueue.Count > 0)
				audioqueue.Dequeue().WriteData(output);
			while (videoqueue.Count > 0)
				videoqueue.Dequeue().WriteData(output);

			output.Close();
			output = null;
		}
	}
}

