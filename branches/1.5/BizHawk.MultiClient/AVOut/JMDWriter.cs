using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// implements IVideoWriter, outputting to format "JMD"
	/// this is the JPC-rr multidump format; there are no filesize limits, and resolution can switch dynamically
	/// so each dump is always one file
	/// they can be processed with JPC-rr streamtools or JMDSource (avisynth)
	/// </summary>
	class JMDWriter : IVideoWriter
	{
		/// <summary>
		/// carries private compression information data
		/// </summary>
		class CodecToken : IDisposable
		{
			public void Dispose()
			{
			}

			/// <summary>
			/// how hard the zlib compressor works
			/// </summary>
			public int compressionlevel
			{
				get;
				set;
			}

			/// <summary>
			/// number of threads to be used for video compression (sort of)
			/// </summary>
			public int numthreads
			{
				get;
				set;
			}

			/// <summary>
			/// instantiates a CodecToken with default parameters
			/// </summary>
			public CodecToken()
			{
				compressionlevel = Deflater.DEFAULT_COMPRESSION;
				numthreads = 3;
			}
		}

		/// <summary>
		/// stores compression parameters
		/// </summary>
		CodecToken token;
		/// <summary>
		/// fps numerator, constant
		/// </summary>
		int fpsnum;
		/// <summary>
		/// fps denominator, constant
		/// </summary>
		int fpsden;

		/// <summary>
		/// audio samplerate, constant
		/// </summary>
		int audiosamplerate;
		/// <summary>
		/// audio number of channels, constant; 1 or 2 only
		/// </summary>
		int audiochannels;
		/// <summary>
		/// audio bits per sample, constant; only 16 supported
		/// </summary>
		int audiobits;

		/// <summary>
		/// actual disk file being written
		/// </summary>
		JMDfile jmdfile;

		/// <summary>
		/// metadata for a movie
		/// not needed if we aren't dumping something that's not a movie
		/// </summary>
		class MovieMetaData
		{
			/// <summary>
			/// name of the game (rom)
			/// </summary>
			public string gamename;
			/// <summary>
			/// author(s) names
			/// </summary>
			public string authors;
			/// <summary>
			/// total length of the movie: ms
			/// </summary>
			public UInt64 lengthms;
			/// <summary>
			/// number of rerecords
			/// </summary>
			public UInt64 rerecords;
		}
		/// <summary>
		/// represents the metadata for the active movie (if applicable)
		/// </summary>
		MovieMetaData moviemetadata;

		/// <summary>
		/// represents a JMD file packet ready to be written except for sorting and timestamp offset
		/// </summary>
		class JMDPacket
		{
			public UInt16 stream;
			public UInt64 timestamp; // final muxed timestamp will be relative to previous
			public byte subtype;
			public byte[] data;
		}

		/// <summary>
		/// writes JMDfile packets to an underlying bytestream
		/// handles one video, one pcm audio, and one metadata track
		/// </summary>
		class JMDfile
		{
			/// <summary>
			/// current timestamp position
			/// </summary>
			UInt64 timestampoff;
			/// <summary>
			/// total number of video frames written
			/// </summary>
			UInt64 totalframes;
			/// <summary>
			/// total number of sample pairs written
			/// </summary>
			UInt64 totalsamples;

			/// <summary>
			/// fps of the video stream is fpsnum/fpsden
			/// </summary>
			int fpsnum;
			/// <summary>
			/// fps of the video stream is fpsnum/fpsden
			/// </summary>
			int fpsden;
			/// <summary>
			/// audio samplerate in hz
			/// </summary>
			int audiosamplerate;
			/// <summary>
			/// true if input will be stereo; mono otherwise
			/// output stream is always stereo
			/// </summary>
			bool stereo;

			/// <summary>
			/// underlying bytestream that is being written to
			/// </summary>
			Stream f;
			public JMDfile(Stream f, int fpsnum, int fpsden, int audiosamplerate, bool stereo)
			{
				if (!f.CanWrite)
					throw new ArgumentException("Stream must be writable!");

				this.f = f;
				this.fpsnum = fpsnum;
				this.fpsden = fpsden;
				this.audiosamplerate = audiosamplerate;
				this.stereo = stereo;

				timestampoff = 0;
				totalframes = 0;
				totalsamples = 0;

				astorage = new Queue<JMDPacket>();
				vstorage = new Queue<JMDPacket>();

				writeheader();
			}

			/// <summary>
			/// write header to the JPC file
			/// assumes one video, one audio, and one metadata stream, with hardcoded IDs
			/// </summary>
			void writeheader()
			{
				// write JPC MAGIC
				writeBE16(0xffff);
				f.Write(Encoding.ASCII.GetBytes("JPCRRMULTIDUMP"), 0, 14);

				// write channel table
				writeBE16(3); // number of streams

				// for each stream
				writeBE16(0); // channel 0
				writeBE16(0); // video
				writeBE16(0); // no name

				writeBE16(1); // channel 1
				writeBE16(1); // pcm audio
				writeBE16(0); // no name

				writeBE16(2); // channel 2
				writeBE16(5); // metadata
				writeBE16(0); // no name
			}

			/// <summary>
			/// write metadata for a movie file
			/// can be called at any time
			/// </summary>
			/// <param name="mmd">metadata to write</param>
			public void writemetadata(MovieMetaData mmd)
			{
				byte[] temp;
				// write metadatas
				writeBE16(2); // data channel
				writeBE32(0); // timestamp (same time as previous packet)
				f.WriteByte(71); // gamename
				temp = System.Text.Encoding.UTF8.GetBytes(mmd.gamename);
				writeVar(temp.Length);
				f.Write(temp, 0, temp.Length);

				writeBE16(2);
				writeBE32(0);
				f.WriteByte(65); // authors
				temp = System.Text.Encoding.UTF8.GetBytes(mmd.authors);
				writeVar(temp.Length);
				f.Write(temp, 0, temp.Length);

				writeBE16(2);
				writeBE32(0);
				f.WriteByte(76); // length
				writeVar(8);
				writeBE64(mmd.lengthms * 1000000);

				writeBE16(2);
				writeBE32(0);
				f.WriteByte(82); // rerecords
				writeVar(8);
				writeBE64(mmd.rerecords);
			}

			/// <summary>
			/// write big endian 16 bit unsigned
			/// </summary>
			/// <param name="v"></param>
			void writeBE16(UInt16 v)
			{
				byte[] b = new byte[2];
				b[0] = (byte)(v >> 8);
				b[1] = (byte)(v & 255);
				f.Write(b, 0, 2);
			}

			/// <summary>
			/// write big endian 32 bit unsigned
			/// </summary>
			/// <param name="v"></param>
			void writeBE32(UInt32 v)
			{
				byte[] b = new byte[4];
				b[0] = (byte)(v >> 24);
				b[1] = (byte)(v >> 16);
				b[2] = (byte)(v >> 8);
				b[3] = (byte)(v & 255);
				f.Write(b, 0, 4);
			}

			/// <summary>
			/// write big endian 64 bit unsigned
			/// </summary>
			/// <param name="v"></param>
			void writeBE64(UInt64 v)
			{
				byte[] b = new byte[8];
				for (int i = 7; i >= 0; i--)
				{
					b[i] = (byte)(v & 255);
					v >>= 8;
				}
				f.Write(b, 0, 8);
			}

			/// <summary>
			/// write variable length value
			/// encoding is similar to MIDI
			/// </summary>
			/// <param name="v"></param>
			void writeVar(UInt64 v)
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
					f.WriteByte(0);
				else
					for (; i > 0; i--)
						f.WriteByte(b[i - 1]);
			}

			/// <summary>
			/// write variable length value
			/// encoding is similar to MIDI
			/// </summary>
			/// <param name="v"></param>
			void writeVar(int v)
			{
				if (v < 0)
					throw new ArgumentException("length cannot be less than 0!");
				writeVar((UInt64)v);
			}

			/// <summary>
			/// creates a timestamp out of fps value
			/// </summary>
			/// <param name="rate">fpsnum</param>
			/// <param name="scale">fpsden</param>
			/// <param name="pos">frame position</param>
			/// <returns>timestamp in nanoseconds</returns>
			static UInt64 timestampcalc(int rate, int scale, UInt64 pos)
			{
				// rate/scale events per second
				// timestamp is in nanoseconds
				// round down, consistent with JPC-rr apparently?
				var b = new System.Numerics.BigInteger(pos) * scale * 1000000000 / rate;

				return (UInt64)b;
			}

			/// <summary>
			/// actually write a packet to file
			/// timestamp sequence must be nondecreasing
			/// </summary>
			/// <param name="j"></param>
			void writeActual(JMDPacket j)
			{
				if (j.timestamp < timestampoff)
					throw new ArithmeticException("JMD Timestamp problem?");
				UInt64 timestampout = j.timestamp - timestampoff;
				while (timestampout > 0xffffffff)
				{
					timestampout -= 0xffffffff;
					// write timestamp skipper
					for (int i = 0; i < 6; i++)
						f.WriteByte(0xff);
				}
				timestampoff = j.timestamp;
				writeBE16(j.stream);
				writeBE32((UInt32)timestampout);
				f.WriteByte(j.subtype);
				writeVar((UInt64)j.data.LongLength);
				f.Write(j.data, 0, j.data.Length);
			}

			/// <summary>
			/// assemble JMDPacket and send to packetqueue
			/// </summary>
			/// <param name="source">zlibed frame with width and height prepended</param>
			public void AddVideo(byte[] source)
			{
				var j = new JMDPacket();
				j.stream = 0;
				j.subtype = 1; // zlib compressed, other possibility is 0 = uncompressed
				j.data = source;
				j.timestamp = timestampcalc(fpsnum, fpsden, (UInt64)totalframes);
				totalframes++;
				writevideo(j);
			}

			/// <summary>
			/// assemble JMDPacket and send to packetqueue
			/// one audio packet is split up into many many JMD packets, since JMD requires only 2 samples (1 left, 1 right) per packet
			/// </summary>
			/// <param name="samples"></param>
			public void AddSamples(short[] samples)
			{
				if (!stereo)
					for (int i = 0; i < samples.Length; i++)
						doaudiopacket(samples[i], samples[i]);
				else
					for (int i = 0; i < samples.Length / 2; i++)
						doaudiopacket(samples[2 * i], samples[2 * i + 1]);
			}

			/// <summary>
			/// helper function makes a JMDPacket out of one sample pair and adds it to the order queue
			/// </summary>
			/// <param name="l">left sample</param>
			/// <param name="r">right sample</param>
			void doaudiopacket(short l, short r)
			{
				var j = new JMDPacket();
				j.stream = 1;
				j.subtype = 1; // raw PCM audio
				j.data = new byte[4];
				j.data[0] = (byte)(l >> 8);
				j.data[1] = (byte)(l & 255);
				j.data[2] = (byte)(r >> 8);
				j.data[3] = (byte)(r & 255);

				j.timestamp = timestampcalc(audiosamplerate, 1, totalsamples);
				totalsamples++;
				writesound(j);
			}

			// ensure outputs are in order
			// JMD packets must be in nondecreasing timestamp order, but there's no obligation
			// for us to get handed that.  this code is a bit overcomplex to handle edge cases
			// that may not be a problem with the current system?

			/// <summary>
			/// collection of JMDpackets yet to be written (audio)
			/// </summary>
			Queue<JMDPacket> astorage;
			/// <summary>
			/// collection of JMDpackets yet to be written (video)
			/// </summary>
			Queue<JMDPacket> vstorage;

			/// <summary>
			/// add a sound packet to the file write queue
			/// will be written when order-appropriate wrt video
			/// the sound packets added must be internally ordered (but need not match video order)
			/// </summary>
			/// <param name="j"></param>
			void writesound(JMDPacket j)
			{
				while (vstorage.Count > 0)
				{
					var p = vstorage.Peek();
					if (p.timestamp <= j.timestamp)
						writeActual(vstorage.Dequeue());
					else
						break;
				}
				astorage.Enqueue(j);
			}

			/// <summary>
			/// add a video packet to the file write queue
			/// will be written when order-appropriate wrt audio
			/// the video packets added must be internally ordered (but need not match audio order)
			/// </summary>
			/// <param name="j"></param>
			void writevideo(JMDPacket j)
			{
				while (astorage.Count > 0)
				{
					var p = astorage.Peek();
					if (p.timestamp <= j.timestamp)
						writeActual(astorage.Dequeue());
					else
						break;
				}
				vstorage.Enqueue(j);
			}

			/// <summary>
			/// flush all remaining JMDPackets to file
			/// call before closing the file
			/// </summary>
			void flushpackets()
			{
				while (astorage.Count > 0 && vstorage.Count > 0)
				{
					var ap = astorage.Peek();
					var av = vstorage.Peek();
					if (ap.timestamp <= av.timestamp)
						writeActual(astorage.Dequeue());
					else
						writeActual(vstorage.Dequeue());
				}
				while (astorage.Count > 0)
					writeActual(astorage.Dequeue());
				while (vstorage.Count > 0)
					writeActual(vstorage.Dequeue());
			}

			/// <summary>
			/// flush any remaining packets and close underlying stream
			/// </summary>
			public void Close()
			{
				flushpackets();
				f.Close();
			}
		}

		/// <summary>
		/// sets default (probably wrong) parameters
		/// </summary>
		public JMDWriter()
		{
			fpsnum = 25;
			fpsden = 1;
			audiosamplerate = 22050;
			audiochannels = 1;
			audiobits = 8;
			token = null;

			moviemetadata = null;
		}

		public void Dispose()
		{
			// we have no unmanaged resources
		}

		/// <summary>
		/// sets the codec token to be used for video compression
		/// </summary>
		public void SetVideoCodecToken(IDisposable token)
		{
			if (token is CodecToken)
				this.token = (CodecToken)token;
			else
				throw new ArgumentException("codec token must be of right type");
		}

		/// <summary>
		/// obtain a set of recording compression parameters
		/// </summary>
		/// <param name="hwnd">hwnd to attach to if the user is shown config dialog</param>
		/// <returns>codec token, dispose of it when you're done with it</returns>
		public IDisposable AcquireVideoCodecToken(System.Windows.Forms.IWin32Window hwnd)
		{
			CodecToken ret = new CodecToken();

			// load from config and sanitize
			int t = Math.Min(Math.Max(Global.Config.JMDThreads, 1), 6);

			int c = Math.Min(Math.Max(Global.Config.JMDCompression, Deflater.NO_COMPRESSION), Deflater.BEST_COMPRESSION);

			if (!JMDForm.DoCompressionDlg(ref t, ref c, 1, 6, Deflater.NO_COMPRESSION, Deflater.BEST_COMPRESSION, hwnd))
				return null;

			Global.Config.JMDThreads = ret.numthreads = t;
			Global.Config.JMDCompression = ret.compressionlevel = c;

			return ret;
		}

		/// <summary>
		/// set framerate to fpsnum/fpsden (assumed to be unchanging over the life of the stream)
		/// </summary>
		public void SetMovieParameters(int fpsnum, int fpsden)
		{
			this.fpsnum = fpsnum;
			this.fpsden = fpsden;
		}

		/// <summary>
		/// set resolution parameters (width x height)
		/// must be set before file is opened
		/// can be changed in future
		/// should always match IVideoProvider
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void SetVideoParameters(int width, int height)
		{
			// each frame is dumped independently with its own resolution tag, so we don't care to store this
		}

		/// <summary>
		/// set audio parameters.  cannot change later
		/// </summary>
		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			// the sampleRate limits are arbitrary, just to catch things which are probably silly-wrong
			// if a larger range of sampling rates is needed, it should be supported
			if (sampleRate < 8000 || sampleRate > 96000 || channels < 1 || channels > 2 || bits != 16)
				throw new ArgumentException("Audio parameters out of range!");
			audiosamplerate = sampleRate;
			audiochannels = channels;
			audiobits = bits;
		}

		/// <summary>
		/// opens a recording stream
		/// set a video codec token first.
		/// </summary>
		public void OpenFile(string baseName)
		{
			string ext = Path.GetExtension(baseName);
			if (ext == null || ext.ToLower() != ".jmd")
				baseName = baseName + ".jmd";

			jmdfile = new JMDfile(File.Open(baseName, FileMode.Create), fpsnum, fpsden, audiosamplerate, audiochannels == 2);


			if (moviemetadata != null)
				jmdfile.writemetadata(moviemetadata);

			// start up thread
			// problem: since audio chunks and video frames both go through here, exactly how many zlib workers
			// gives is not known without knowing how the emulator will chunk audio packets
			// this shouldn't affect results though, just performance
			threadQ = new System.Collections.Concurrent.BlockingCollection<Object>(token.numthreads * 2);
			workerT = new System.Threading.Thread(new System.Threading.ThreadStart(threadproc));
			workerT.Start();
			GzipFrameDelegate = new GzipFrameD(GzipFrame);
		}

		// some of this code is copied from AviWriter... not sure how if at all it should be abstracted
		/// <summary>
		/// blocking threadsafe queue, used for communication between main program and file writing thread
		/// </summary>
		System.Collections.Concurrent.BlockingCollection<Object> threadQ;
		/// <summary>
		/// file writing thread; most of the work happens here
		/// </summary>
		System.Threading.Thread workerT;

		/// <summary>
		/// filewriting thread's loop
		/// </summary>
		void threadproc()
		{
			try
			{
				while (true)
				{
					Object o = threadQ.Take();
					if (o is IAsyncResult)
						jmdfile.AddVideo(GzipFrameDelegate.EndInvoke((IAsyncResult)o));
					else if (o is short[])
						jmdfile.AddSamples((short[])o);
					else
						// anything else is assumed to be quit time
						return;
				}
			}
			catch (Exception e)
			{
				System.Windows.Forms.MessageBox.Show("JMD Worker Thread died:\n\n" + e.ToString());
				return;
			}
		}

		/// <summary>
		/// close recording stream
		/// </summary>
		public void CloseFile()
		{
			threadQ.Add(new Object()); // acts as stop message
			workerT.Join();

			jmdfile.Close();
		}

		/// <summary>
		/// makes a copy of an IVideoProvider
		/// handles conversion to a byte array suitable for compression by zlib
		/// </summary>
		class VideoCopy
		{
			public byte[] VideoBuffer;

			public int BufferWidth;
			public int BufferHeight;
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
				//Buffer.BlockCopy(vb, 0, VideoBuffer, 0, VideoBuffer.Length);
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
		byte[] GzipFrame(VideoCopy v)
		{
			MemoryStream m = new MemoryStream();
			// write frame height and width first
			m.WriteByte((byte)(v.BufferWidth >> 8));
			m.WriteByte((byte)(v.BufferWidth & 255));
			m.WriteByte((byte)(v.BufferHeight >> 8));
			m.WriteByte((byte)(v.BufferHeight & 255));
			var g = new DeflaterOutputStream(m, new ICSharpCode.SharpZipLib.Zip.Compression.Deflater(token.compressionlevel));
			g.IsStreamOwner = false; // leave memory stream open so we can pick its contents
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
		delegate byte[] GzipFrameD(VideoCopy v);
		/// <summary>
		/// delegate for GzipFrame
		/// </summary>
		GzipFrameD GzipFrameDelegate;

		/// <summary>
		/// adds a frame to the stream
		/// </summary>
		public void AddFrame(IVideoProvider source)
		{
			if (!workerT.IsAlive)
				// signal some sort of error?
				return;
			threadQ.Add(GzipFrameDelegate.BeginInvoke(new VideoCopy(source), null, null));
		}

		/// <summary>
		/// adds audio samples to the stream
		/// no attempt is made to sync this to the video
		/// </summary>
		public void AddSamples(short[] samples)
		{
			if (!workerT.IsAlive)
				// signal some sort of error?
				return;
			threadQ.Add((short[])samples.Clone());
		}

		/// <summary>
		/// set metadata parameters; should be called before opening file
		/// </summary>
		public void SetMetaData(string gameName, string authors, UInt64 lengthMS, UInt64 rerecords)
		{
			moviemetadata = new MovieMetaData();
			moviemetadata.gamename = gameName;
			moviemetadata.authors = authors;
			moviemetadata.lengthms = lengthMS;
			moviemetadata.rerecords = rerecords;
		}


		public override string ToString()
		{
			return "JMD writer";
		}

		public string WriterDescription()
		{
			return "Writes a JPC-rr multidump file (JMD).  These can be read and further processed with jpc-streamtools.  One JMD file contains all audio (uncompressed) and video (compressed).";
		}

		public string DesiredExtension()
		{
			return "jmd";
		}


		public void SetDefaultVideoCodecToken()
		{
			CodecToken ct = new CodecToken();

			// load from config and sanitize
			int t = Math.Min(Math.Max(Global.Config.JMDThreads, 1), 6);

			int c = Math.Min(Math.Max(Global.Config.JMDCompression, Deflater.NO_COMPRESSION), Deflater.BEST_COMPRESSION);

			ct.compressionlevel = c;
			ct.numthreads = t;

			token = ct;
		}

		public string ShortName()
		{
			return "jmd";
		}
	}
}
