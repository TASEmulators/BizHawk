using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

using BizHawk.Client.Common;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// implements IVideoWriter, outputting to format "JMD"
	/// this is the JPC-rr multidump format; there are no filesize limits, and resolution can switch dynamically
	/// so each dump is always one file
	/// they can be processed with JPC-rr streamtools or JMDSource (avisynth)
	/// </summary>
	[VideoWriter("jmd", "JMD writer", "Writes a JPC-rr multidump file (JMD).  These can be read and further processed with jpc-streamtools.  One JMD file contains all audio (uncompressed) and video (compressed).")]
	public class JmdWriter : IVideoWriter
	{
		// We formerly used a compressor that supported 0-9 values for compression level
		private const int NO_COMPRESSION = 0;
		private const int BEST_COMPRESSION = 9;
		private const int DEFAULT_COMPRESSION = -1;
		private const int BEST_SPEED = 1;

		private static CompressionLevel GetCompressionLevel(int v)
		{
			switch (v)
			{
				case NO_COMPRESSION:
					return CompressionLevel.NoCompression;
				case BEST_COMPRESSION:
					return CompressionLevel.Optimal;
				default:
					return CompressionLevel.Fastest;
			}
		}

		/// <summary>
		/// carries private compression information data
		/// </summary>
		private class CodecToken : IDisposable
		{
			public void Dispose()
			{
			}

			/// <summary>
			/// how hard the zlib compressor works
			/// </summary>
			public int CompressionLevel { get; set; }

			/// <summary>
			/// number of threads to be used for video compression (sort of)
			/// </summary>
			public int NumThreads { get; set; }

			/// <summary>
			/// instantiates a CodecToken with default parameters
			/// </summary>
			public CodecToken()
			{
				CompressionLevel = DEFAULT_COMPRESSION;
				NumThreads = 3;
			}
		}

		private readonly IDialogParent _dialogParent;

		// stores compression parameters
		private CodecToken _token;

		// fps numerator, constant
		private int _fpsNum;

		// fps denominator, constant
		private int _fpsDen;

		// audio samplerate, constant
		private int _audioSampleRate;

		/// <summary>
		/// audio number of channels, constant; 1 or 2 only
		/// </summary>
		private int _audioChannels;

		// actual disk file being written
		private JmdFile _jmdFile;

		/// <summary>
		/// metadata for a movie
		/// not needed if we aren't dumping something that's not a movie
		/// </summary>
		private class MovieMetaData
		{
			/// <summary>
			/// name of the game (rom)
			/// </summary>
			public string GameName { get; set; }

			/// <summary>
			/// author(s) names
			/// </summary>
			public string Authors { get; set; }

			/// <summary>
			/// total length of the movie: ms
			/// </summary>
			public ulong LengthMs { get; set; }
			
			/// <summary>
			/// number of rerecords
			/// </summary>
			public ulong Rerecords { get; set; }
		}

		// represents the metadata for the active movie (if applicable)
		private MovieMetaData _movieMetadata;

		/// <summary>
		/// represents a JMD file packet ready to be written except for sorting and timestamp offset
		/// </summary>
		private class JmdPacket
		{
			public ushort Stream { get; set; }
			public ulong Timestamp { get; set; } // final muxed timestamp will be relative to previous
			public byte Subtype { get; set; }
			public byte[] Data { get; set; }
		}

		/// <summary>
		/// writes JMD file packets to an underlying bytestream
		/// handles one video, one pcm audio, and one metadata track
		/// </summary>
		private class JmdFile
		{
			// current timestamp position
			private ulong _timestampOff;

			// total number of video frames written
			private ulong _totalFrames;

			// total number of sample pairs written
			private ulong _totalSamples;

			// fps of the video stream is fpsNum/fpsDen
			private readonly int _fpsNum;

			// fps of the video stream is fpsNum/fpsDen
			private readonly int _fpsDen;

			// audio samplerate in hz
			private readonly int _audioSamplerate;

			// true if input will be stereo; mono otherwise
			// output stream is always stereo
			private readonly bool _stereo;

			/// <summary>underlying bytestream that is being written to</summary>
			private readonly Stream _f;

			/// <exception cref="ArgumentException"><paramref name="f"/> cannot be written to</exception>
			public JmdFile(Stream f, int fpsNum, int fpsDen, int audioSamplerate, bool stereo)
			{
				if (!f.CanWrite)
				{
					throw new ArgumentException(message: $"{nameof(Stream)} must be writable!", paramName: nameof(f));
				}

				_f = f;
				_fpsNum = fpsNum;
				_fpsDen = fpsDen;
				_audioSamplerate = audioSamplerate;
				_stereo = stereo;

				_timestampOff = 0;
				_totalFrames = 0;
				_totalSamples = 0;

				_audioStorage = new Queue<JmdPacket>();
				_videoStorage = new Queue<JmdPacket>();

				WriteHeader();
			}

			// write header to the JPC file
			// assumes one video, one audio, and one metadata stream, with hardcoded IDs
			private void WriteHeader()
			{
				// write JPC MAGIC
				WriteBe16(0xffff);
				_f.Write(Encoding.ASCII.GetBytes("JPCRRMULTIDUMP"), 0, 14);

				// write channel table
				WriteBe16(3); // number of streams

				// for each stream
				WriteBe16(0); // channel 0
				WriteBe16(0); // video
				WriteBe16(0); // no name

				WriteBe16(1); // channel 1
				WriteBe16(1); // pcm audio
				WriteBe16(0); // no name

				WriteBe16(2); // channel 2
				WriteBe16(5); // metadata
				WriteBe16(0); // no name
			}

			/// <summary>
			/// write metadata for a movie file
			/// can be called at any time
			/// </summary>
			/// <param name="mmd">metadata to write</param>
			public void WriteMetadata(MovieMetaData mmd)
			{
				// write metadata
				WriteBe16(2); // data channel
				WriteBe32(0); // timestamp (same time as previous packet)
				_f.WriteByte(71); // GameName

				var temp = Encoding.UTF8.GetBytes(mmd.GameName);
				WriteVar(temp.Length);
				_f.Write(temp, 0, temp.Length);

				WriteBe16(2);
				WriteBe32(0);
				_f.WriteByte(65); // authors
				temp = Encoding.UTF8.GetBytes(mmd.Authors);
				WriteVar(temp.Length);
				_f.Write(temp, 0, temp.Length);

				WriteBe16(2);
				WriteBe32(0);
				_f.WriteByte(76); // length
				WriteVar(8);
				WriteBe64(mmd.LengthMs * 1000000);

				WriteBe16(2);
				WriteBe32(0);
				_f.WriteByte(82); // rerecords
				WriteVar(8);
				WriteBe64(mmd.Rerecords);
			}

			/// <summary>
			/// write big endian 16 bit unsigned
			/// </summary>
			private void WriteBe16(ushort v)
			{
				byte[] b = new byte[2];
				b[0] = (byte)(v >> 8);
				b[1] = (byte)(v & 255);
				_f.Write(b, 0, 2);
			}

			/// <summary>
			/// write big endian 32 bit unsigned
			/// </summary>
			private void WriteBe32(uint v)
			{
				byte[] b = new byte[4];
				b[0] = (byte)(v >> 24);
				b[1] = (byte)(v >> 16);
				b[2] = (byte)(v >> 8);
				b[3] = (byte)(v & 255);
				_f.Write(b, 0, 4);
			}

			/// <summary>
			/// write big endian 64 bit unsigned
			/// </summary>
			private void WriteBe64(ulong v)
			{
				byte[] b = new byte[8];
				for (int i = 7; i >= 0; i--)
				{
					b[i] = (byte)(v & 255);
					v >>= 8;
				}
				_f.Write(b, 0, 8);
			}

			/// <summary>
			/// write variable length value
			/// encoding is similar to MIDI
			/// </summary>
			private void WriteVar(ulong v)
			{
				byte[] b = new byte[10];
				int i = 0;
				while (v > 0)
				{
					if (i > 0)
						b[i++] = (byte)((v & 127) | 128);
					else
						b[i++] = (byte)(v & 127);
					v /= 128;
				}

				if (i == 0)
				{
					_f.WriteByte(0);
				}
				else
				{
					for (; i > 0; i--)
					{
						_f.WriteByte(b[i - 1]);
					}
				}
			}

			/// <summary>
			/// write variable length value
			/// encoding is similar to MIDI
			/// </summary>
			private void WriteVar(int v)
			{
				if (v < 0) throw new ArgumentException(message: "length cannot be less than 0!", paramName: nameof(v));
				WriteVar((ulong)v);
			}

			/// <summary>
			/// creates a timestamp out of fps value
			/// </summary>
			/// <param name="rate">fpsNum</param>
			/// <param name="scale">fpsDen</param>
			/// <param name="pos">frame position</param>
			/// <returns>timestamp in nanoseconds</returns>
			private static ulong TimestampCalc(int rate, int scale, ulong pos)
			{
				// rate/scale events per second
				// timestamp is in nanoseconds
				// round down, consistent with JPC-rr apparently?
				var b = new System.Numerics.BigInteger(pos) * scale * 1000000000 / rate;

				return (ulong)b;
			}

			/// <summary>
			/// actually write a packet to file
			/// timestamp sequence must be non-decreasing
			/// </summary>
			private void WriteActual(JmdPacket j)
			{
				if (j.Timestamp < _timestampOff)
				{
					throw new ArithmeticException("JMD Timestamp problem?");
				}

				var timeStampOut = j.Timestamp - _timestampOff;
				while (timeStampOut > 0xffffffff)
				{
					timeStampOut -= 0xffffffff;
					// write timestamp skipper
					for (int i = 0; i < 6; i++)
						_f.WriteByte(0xff);
				}
				_timestampOff = j.Timestamp;
				WriteBe16(j.Stream);
				WriteBe32((uint)timeStampOut);
				_f.WriteByte(j.Subtype);
				WriteVar((ulong)j.Data.LongLength);
				_f.Write(j.Data, 0, j.Data.Length);
			}

			/// <summary>
			/// assemble JMDPacket and send to PacketQueue
			/// </summary>
			/// <param name="source">zlibed frame with width and height prepended</param>
			public void AddVideo(byte[] source)
			{
				var j = new JmdPacket
				{
					Stream = 0,
					Subtype = 1,// zlib compressed, other possibility is 0 = uncompressed
					Data = source,
					Timestamp = TimestampCalc(_fpsNum, _fpsDen, _totalFrames)
				};
				
				_totalFrames++;
				WriteVideo(j);
			}

			/// <summary>
			/// assemble JMDPacket and send to PacketQueue
			/// one audio packet is split up into many many JMD packets, since JMD requires only 2 samples (1 left, 1 right) per packet
			/// </summary>
			public void AddSamples(short[] samples)
			{
				if (!_stereo)
				{
					for (int i = 0; i < samples.Length; i++)
					{
						DoAudioPacket(samples[i], samples[i]);
					}
				}
				else
				{
					for (int i = 0; i < samples.Length / 2; i++)
					{
						DoAudioPacket(samples[2 * i], samples[2 * i + 1]);
					}
				}
			}

			/// <summary>
			/// helper function makes a JMDPacket out of one sample pair and adds it to the order queue
			/// </summary>
			/// <param name="l">left sample</param>
			/// <param name="r">right sample</param>
			private void DoAudioPacket(short l, short r)
			{
				var j = new JmdPacket
				{
					Stream = 1,
					Subtype = 1, // raw PCM audio
					Data = new byte[4]
				};
				
				j.Data[0] = (byte)(l >> 8);
				j.Data[1] = (byte)(l & 255);
				j.Data[2] = (byte)(r >> 8);
				j.Data[3] = (byte)(r & 255);

				j.Timestamp = TimestampCalc(_audioSamplerate, 1, _totalSamples);
				_totalSamples++;
				WriteSound(j);
			}

			// ensure outputs are in order
			// JMD packets must be in non-decreasing timestamp order, but there's no obligation
			// for us to get handed that. This code is a bit overly complex to handle edge cases
			// that may not be a problem with the current system?

			// collection of JMD packets yet to be written (audio)
			private readonly Queue<JmdPacket> _audioStorage;

			// collection of JMD packets yet to be written (video)
			private readonly Queue<JmdPacket> _videoStorage;

			// add a sound packet to the file write queue
			// will be written when order-appropriate wrt video
			// the sound packets added must be internally ordered (but need not match video order)
			private void WriteSound(JmdPacket j)
			{
				while (_videoStorage.Count > 0)
				{
					var p = _videoStorage.Peek();
					if (p.Timestamp <= j.Timestamp)
						WriteActual(_videoStorage.Dequeue());
					else
						break;
				}

				_audioStorage.Enqueue(j);
			}

			// add a video packet to the file write queue
			// will be written when order-appropriate wrt audio
			// the video packets added must be internally ordered (but need not match audio order)
			private void WriteVideo(JmdPacket j)
			{
				while (_audioStorage.Count > 0)
				{
					var p = _audioStorage.Peek();
					if (p.Timestamp <= j.Timestamp)
						WriteActual(_audioStorage.Dequeue());
					else
						break;
				}
				_videoStorage.Enqueue(j);
			}

			// flush all remaining JMDPackets to file
			// call before closing the file
			private void FlushPackets()
			{
				while (_audioStorage.Count > 0 && _videoStorage.Count > 0)
				{
					var ap = _audioStorage.Peek();
					var av = _videoStorage.Peek();
					WriteActual(ap.Timestamp <= av.Timestamp
						? _audioStorage.Dequeue()
						: _videoStorage.Dequeue());
				}
				while (_audioStorage.Count > 0)
					WriteActual(_audioStorage.Dequeue());
				while (_videoStorage.Count > 0)
					WriteActual(_videoStorage.Dequeue());
			}

			/// <summary>
			/// flush any remaining packets and close underlying stream
			/// </summary>
			public void Close()
			{
				FlushPackets();
				_f.Close();
			}
		}

		/// <summary>
		/// sets default (probably wrong) parameters
		/// </summary>
		public JmdWriter(IDialogParent dialogParent)
		{
			_dialogParent = dialogParent;

			_fpsNum = 25;
			_fpsDen = 1;
			_audioSampleRate = 22050;
			_audioChannels = 1;
			_token = null;

			_movieMetadata = null;
		}

		public void Dispose()
		{
			// we have no unmanaged resources
		}

		/// <summary>sets the codec token to be used for video compression</summary>
		/// <exception cref="ArgumentException"><paramref name="token"/> does not inherit <see cref="JmdWriter.CodecToken"/></exception>
		public void SetVideoCodecToken(IDisposable token)
		{
			if (token is CodecToken codecToken)
			{
				_token = codecToken;
			}
			else
			{
				throw new ArgumentException(message: "codec token must be of right type", paramName: nameof(token));
			}
		}

		public IDisposable AcquireVideoCodecToken(Config config)
		{
			var ret = new CodecToken();

			// load from config and sanitize
			int t = Math.Min(Math.Max(config.JmdThreads, 1), 6);

			int c = Math.Min(Math.Max(config.JmdCompression, NO_COMPRESSION), BEST_COMPRESSION);

			if (!JmdForm.DoCompressionDlg(ref t, ref c, 1, 6, NO_COMPRESSION, BEST_COMPRESSION, _dialogParent.AsWinFormsHandle()))
				return null;

			config.JmdThreads = ret.NumThreads = t;
			config.JmdCompression = ret.CompressionLevel = c;

			return ret;
		}

		/// <summary>
		/// set framerate to fpsNum/fpsDen (assumed to be unchanging over the life of the stream)
		/// </summary>
		public void SetMovieParameters(int fpsNum, int fpsDen)
		{
			_fpsNum = fpsNum;
			_fpsDen = fpsDen;
		}

		/// <summary>
		/// set resolution parameters (width x height)
		/// must be set before file is opened
		/// can be changed in future
		/// should always match IVideoProvider
		/// </summary>
		public void SetVideoParameters(int width, int height)
		{
			// each frame is dumped independently with its own resolution tag, so we don't care to store this
		}

		/// <summary>set audio parameters, cannot change later</summary>
		/// <exception cref="ArgumentException"><paramref name="sampleRate"/> is outside range <c>8000..96000</c>, <paramref name="channels"/> is outside range <c>1..2</c>, or <paramref name="bits"/> is not <c>16</c></exception>
		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			// the sampleRate limits are arbitrary, just to catch things which are probably silly-wrong
			// if a larger range of sampling rates is needed, it should be supported
			const string ERR_MSG_INVALID_ARG = "Audio parameters out of range!";
			if (sampleRate is < 8000 or > 96000) throw new ArgumentOutOfRangeException(paramName: nameof(sampleRate), sampleRate, message: ERR_MSG_INVALID_ARG);
			if (channels is < 1 or > 2) throw new ArgumentOutOfRangeException(paramName: nameof(channels), channels, message: ERR_MSG_INVALID_ARG);
			if (bits is not 16) throw new ArgumentException(message: ERR_MSG_INVALID_ARG, paramName: nameof(bits));

			_audioSampleRate = sampleRate;
			_audioChannels = channels;
		}

		/// <summary>
		/// opens a recording stream
		/// set a video codec token first.
		/// </summary>
		public void OpenFile(string baseName)
		{
			string ext = Path.GetExtension(baseName);
			if (ext == null || ext.ToLowerInvariant() != ".jmd")
			{
				baseName += ".jmd";
			}

			_jmdFile = new JmdFile(File.Open(baseName, FileMode.Create), _fpsNum, _fpsDen, _audioSampleRate, _audioChannels == 2);

			if (_movieMetadata != null)
			{
				_jmdFile.WriteMetadata(_movieMetadata);
			}

			// start up thread
			// problem: since audio chunks and video frames both go through here, exactly how many zlib workers
			// gives is not known without knowing how the emulator will chunk audio packets
			// this shouldn't affect results though, just performance
			_threadQ = new BlockingCollection<object>(_token.NumThreads * 2);
			_workerT = new Thread(ThreadProc);
			_workerT.Start();
			_gzipFrameDelegate = GzipFrame;
		}

		// some of this code is copied from AviWriter... not sure how if at all it should be abstracted

		/// <summary>
		/// blocking thread safe queue, used for communication between main program and file writing thread
		/// </summary>
		private BlockingCollection<object> _threadQ;
		
		/// <summary>
		/// file writing thread; most of the work happens here
		/// </summary>
		private Thread _workerT;

		/// <summary>
		/// file writing thread's loop
		/// </summary>
		private void ThreadProc()
		{
			try
			{
				while (true)
				{
					object o = _threadQ.Take();
					if (o is IAsyncResult result)
					{
						_jmdFile.AddVideo(_gzipFrameDelegate.EndInvoke(result));
					}
					else if (o is short[] shorts)
					{
						_jmdFile.AddSamples(shorts);
					}
					else
					{
						// anything else is assumed to be quit time
						return;
					}
				}
			}
			catch (Exception e)
			{
				_dialogParent.DialogController.ShowMessageBox($"JMD Worker Thread died:\n\n{e}");
			}
		}

		/// <summary>
		/// close recording stream
		/// </summary>
		public void CloseFile()
		{
			_threadQ.Add(new object()); // acts as stop message
			_workerT.Join();
			_jmdFile.Close();
		}

		/// <summary>
		/// makes a copy of an IVideoProvider
		/// handles conversion to a byte array suitable for compression by zlib
		/// </summary>
		public class VideoCopy
		{
			public byte[] VideoBuffer { get; set; }

			public int BufferWidth { get; set; }
			public int BufferHeight { get; set; }
			public VideoCopy(IVideoProvider c)
			{
				int[] vb = c.GetVideoBuffer();
				VideoBuffer = new byte[vb.Length * sizeof(int)];

				// we have to switch RGB ordering here
				for (int i = 0; i < vb.Length; i++)
				{
					VideoBuffer[i * 4 + 0] = (byte)(vb[i] >> 16);
					VideoBuffer[i * 4 + 1] = (byte)(vb[i] >> 8);
					VideoBuffer[i * 4 + 2] = (byte)(vb[i] & 255);
					VideoBuffer[i * 4 + 3] = 0;
				}

				BufferWidth = c.BufferWidth;
				BufferHeight = c.BufferHeight;
			}
		}

		/// <summary>
		/// deflates (zlib) a VideoCopy, returning a byte array suitable for insertion into a JMD file
		/// the byte array includes width and height dimensions at the beginning
		/// this is run asynchronously for speedup, as compressing can be slow
		/// </summary>
		/// <param name="v">video frame to compress</param>
		/// <returns>zlib compressed frame, with width and height prepended</returns>
		private byte[] GzipFrame(VideoCopy v)
		{
			var m = new MemoryStream();

			// write frame height and width first
			m.WriteByte((byte)(v.BufferWidth >> 8));
			m.WriteByte((byte)(v.BufferWidth & 255));
			m.WriteByte((byte)(v.BufferHeight >> 8));
			m.WriteByte((byte)(v.BufferHeight & 255));

			var g = new GZipStream(m, GetCompressionLevel(_token.CompressionLevel), true); // leave memory stream open so we can pick its contents
			g.Write(v.VideoBuffer, 0, v.VideoBuffer.Length);
			g.Flush();
			g.Close();

			byte[] ret = m.GetBuffer();
			Array.Resize(ref ret, (int)m.Length);
			m.Close();
			return ret;
		}

		/// <summary>
		/// delegate for GzipFrame
		/// </summary>
		/// <param name="v">VideoCopy to compress</param>
		/// <returns>gzipped stream with width and height prepended</returns>
		private delegate byte[] GzipFrameD(VideoCopy v);
		
		// delegate for GzipFrame
		private GzipFrameD _gzipFrameDelegate;

		/// <summary>
		/// adds a frame to the stream
		/// </summary>
		public void AddFrame(IVideoProvider source)
		{
			if (!_workerT.IsAlive)
			{
				// signal some sort of error?
				return;
			}

			_threadQ.Add(_gzipFrameDelegate.BeginInvoke(new VideoCopy(source), null, null));
		}

		/// <summary>
		/// adds audio samples to the stream
		/// no attempt is made to sync this to the video
		/// </summary>
		public void AddSamples(short[] samples)
		{
			if (!_workerT.IsAlive)
			{
				// signal some sort of error?
				return;
			}

			_threadQ.Add((short[])samples.Clone());
		}

		/// <summary>
		/// set metadata parameters; should be called before opening file
		/// </summary>
		public void SetMetaData(string gameName, string authors, ulong lengthMs, ulong rerecords)
		{
			_movieMetadata = new MovieMetaData
			{
				GameName = gameName,
				Authors = authors,
				LengthMs = lengthMs,
				Rerecords = rerecords
			};
		}

		public string DesiredExtension() => "jmd";

		public void SetDefaultVideoCodecToken(Config config)
		{
			CodecToken ct = new CodecToken();

			// load from config and sanitize
			int t = Math.Min(Math.Max(config.JmdThreads, 1), 6);

			int c = Math.Min(Math.Max(config.JmdCompression, NO_COMPRESSION), BEST_COMPRESSION);

			ct.CompressionLevel = c;
			ct.NumThreads = t;

			_token = ct;
		}

		public void SetFrame(int frame) { }

		public bool UsesAudio => true;
		public bool UsesVideo => true;
	}


}
