using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// writes MS standard riff files containing uncompressed PCM wav data
	/// supports 16 bit signed data only
	/// </summary>
	public class WavWriter : IDisposable
	{
		/// <summary>
		/// underlying file being written to
		/// </summary>
		BinaryWriter file;
		/// <summary>
		/// sequence of files to write to (split on 32 bit limit)
		/// </summary>
		IEnumerator<Stream> filechain;

		/// <summary>
		/// samplerate in HZ
		/// </summary>
		int samplerate;
		/// <summary>
		/// number of audio channels
		/// </summary>
		int numchannels;

		/// <summary>
		/// number of bytes of PCM data written to current file
		/// </summary>
		UInt64 numbytes;

		/// <summary>
		/// number of bytes after which a file split should be made
		/// </summary>
		const UInt64 splitpoint = 2 * 1000 * 1000 * 1000;

		/// <summary>
		/// write riff headers to current file
		/// </summary>
		void writeheaders()
		{
			file.Write(Encoding.ASCII.GetBytes("RIFF")); // ChunkID
			file.Write((UInt32)0); // ChunkSize
			file.Write(Encoding.ASCII.GetBytes("WAVE")); // Format

			file.Write(Encoding.ASCII.GetBytes("fmt ")); // SubchunkID
			file.Write((UInt32)16); // SubchunkSize
			file.Write((UInt16)1); // AudioFormat (PCM)
			file.Write((UInt16)numchannels); // NumChannels
			file.Write((UInt32)samplerate); // SampleRate
			file.Write((UInt32)(samplerate * numchannels * 2)); // ByteRate
			file.Write((UInt16)(numchannels * 2)); // BlockAlign
			file.Write((UInt16)16); // BitsPerSample

			file.Write(Encoding.ASCII.GetBytes("data")); // SubchunkID
			file.Write((UInt32)0); // SubchunkSize
		}

		/// <summary>
		/// seek back to beginning of file and fix header sizes (if possible)
		/// </summary>
		void finalizeheaders()
		{
			if (numbytes + 36 >= 0x100000000)
				// passed 4G limit, nothing to be done
				return;
			try
			{
				file.Seek(4, SeekOrigin.Begin);
				file.Write((UInt32)(36 + numbytes));
				file.Seek(40, SeekOrigin.Begin);
				file.Write((UInt32)(numbytes));
			}
			catch (NotSupportedException)
			{	// unseekable; oh well
			}
		}

		/// <summary>
		/// close current underlying stream
		/// </summary>
		void closecurrent ()
		{
			if (file != null)
			{
				finalizeheaders();
				file.Close();
				file.Dispose();
			}
			file = null;
		}
		/// <summary>
		/// open a new underlying stream
		/// </summary>
		/// <param name="next"></param>
		void opencurrent (Stream next)
		{
			file = new BinaryWriter(next, Encoding.ASCII);
			numbytes = 0;
			writeheaders();
		}

		/// <summary>
		/// write samples to file
		/// </summary>
		/// <param name="samples">samples to write; should contain one for each channel</param>
		public void writesamples(short[] samples)
		{
			file.Write(samples);
			numbytes += (UInt64)(samples.Length * sizeof(short));

			// try splitting if we can
			if (numbytes >= splitpoint && filechain != null)
			{
				if (!filechain.MoveNext())
				{	// out of files, just keep on writing to this one
					filechain = null;
				}
				else
				{
					Stream next = filechain.Current;
					closecurrent();
					opencurrent(next);
				}
			}
		}

		public void Dispose()
		{
			Close();
		}

		/// <summary>
		/// finishes writing
		/// </summary>
		public void Close()
		{
			closecurrent();
		}

		/// <summary>
		/// checks sampling rate, number of channels for validity
		/// </summary>
		void checkargs()
		{
			if (samplerate < 1 || numchannels < 1)
				throw new ArgumentException("Bad samplerate/numchannels");
		}

		/// <summary>
		/// initializes WavWriter with a single output stream
		/// no attempt is made to split
		/// </summary>
		/// <param name="s">WavWriter now owns this stream</param>
		/// <param name="samplerate">sampling rate in HZ</param>
		/// <param name="numchannels">number of audio channels</param>
		public WavWriter(Stream s, int samplerate, int numchannels)
		{
			this.samplerate = samplerate;
			this.numchannels = numchannels;
			filechain = null;
			checkargs();
			opencurrent(s);
		}

		/// <summary>
		/// initializes WavWriter with an enumeration of Streams
		/// one is consumed every time 2G is hit
		/// if the enumerator runs out before the audio stream does, the last file could be >2G
		/// </summary>
		/// <param name="ss">WavWriter now owns any of these streams that it enumerates</param>
		/// <param name="samplerate">sampling rate in HZ</param>
		/// <param name="numchannels">number of audio channels</param>
		public WavWriter(IEnumerator<Stream> ss, int samplerate, int numchannels)
		{
			this.samplerate = samplerate;
			this.numchannels = numchannels;
			checkargs();
			filechain = ss;
			// advance to first
			if (!filechain.MoveNext())
				throw new ArgumentException("Iterator was empty!");
			opencurrent(ss.Current);
		}   
	}

	/// <summary>
	/// slim wrapper on WavWriter that implements IVideoWriter (discards all video!)
	/// </summary>
	public class WavWriterV : IVideoWriter
	{
		public void SetVideoCodecToken(IDisposable token) { }
		public void AddFrame(IVideoProvider source) { }
		public void SetMovieParameters(int fpsnum, int fpsden) { }
		public void SetVideoParameters(int width, int height) { }

		class WavWriterVToken : IDisposable
		{
			public void Dispose() { }
		}
		public IDisposable AcquireVideoCodecToken(System.Windows.Forms.IWin32Window hwnd)
		{
			// don't care
			return new WavWriterVToken();
		}

		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			this.sampleRate = sampleRate;
			this.channels = channels;
			if (bits != 16)
				throw new ArgumentException("Only support 16bit audio!");
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMS, ulong rerecords)
		{
			// not implemented
		}

		public void Dispose()
		{
			if (wavwriter != null)
				wavwriter.Dispose();
		}

		WavWriter wavwriter = null;
		int sampleRate = 0;
		int channels = 0;

		/// <summary>
		/// create a simple wav stream iterator
		/// </summary>
		/// <param name="template"></param>
		/// <returns></returns>
		static IEnumerator<Stream> CreateStreamIterator(string template)
		{
			string dir = Path.GetDirectoryName(template);
			string baseName = Path.GetFileNameWithoutExtension(template);
			string ext = Path.GetExtension(template);
			yield return new FileStream(template, FileMode.Create);
			int counter = 1;
			while (true)
			{
				yield return new FileStream (Path.Combine(dir, baseName) + "_" + counter + ext, FileMode.Create);
				counter++;
			}
		}

		public void OpenFile(string baseName)
		{
			wavwriter = new WavWriter(CreateStreamIterator(baseName), sampleRate, channels);
		}

		public void CloseFile()
		{
			wavwriter.Close();
			wavwriter.Dispose();
			wavwriter = null;
		}

		public void AddSamples(short[] samples)
		{
			wavwriter.writesamples(samples);
		}

		public override string ToString()
		{
			return ".wav writer";
		}

		public string WriterDescription()
		{
			return "Writes a series of standard RIFF wav files containing uncompressed audio.  Does not write video.  Splits every 2G.";
		}

		public string DesiredExtension()
		{
			return "wav";
		}


		public void SetDefaultVideoCodecToken()
		{
			// don't use codec tokens, so don't care
		}

		public string ShortName()
		{
			return "wave";
		}
	}
}
