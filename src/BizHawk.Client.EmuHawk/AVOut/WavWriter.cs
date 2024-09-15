using System.Collections.Generic;
using System.Text;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
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
		private BinaryWriter _file;

		/// <summary>
		/// sequence of files to write to (split on 32 bit limit)
		/// </summary>
		private IEnumerator<Stream> _fileChain;

		/// <summary>
		/// samplerate in HZ
		/// </summary>
		private readonly int _sampleRate;

		/// <summary>
		/// number of audio channels
		/// </summary>
		private readonly int _numChannels;

		/// <summary>
		/// number of bytes of PCM data written to current file
		/// </summary>
		private ulong _numBytes;

		/// <summary>
		/// number of bytes after which a file split should be made
		/// </summary>
		private const ulong SplitPoint = 2 * 1000 * 1000 * 1000;

		/// <summary>
		/// write riff headers to current file
		/// </summary>
		private void WriteHeaders()
		{
			_file.Write(Encoding.ASCII.GetBytes("RIFF")); // ChunkID
			_file.Write(0U); // ChunkSize
			_file.Write(Encoding.ASCII.GetBytes("WAVE")); // Format

			_file.Write(Encoding.ASCII.GetBytes("fmt ")); // SubchunkID
			_file.Write(16U); // SubchunkSize
			_file.Write((ushort)1U); // AudioFormat (PCM)
			_file.Write((ushort)_numChannels); // NumChannels
			_file.Write((uint)_sampleRate); // SampleRate
			_file.Write((uint)(_sampleRate * _numChannels * 2)); // ByteRate
			_file.Write((ushort)(_numChannels * 2)); // BlockAlign
			_file.Write((ushort)16U); // BitsPerSample

			_file.Write(Encoding.ASCII.GetBytes("data")); // SubchunkID
			_file.Write(0U); // SubchunkSize
		}

		/// <summary>
		/// seek back to beginning of file and fix header sizes (if possible)
		/// </summary>
		private void FinalizeHeaders()
		{
			if (_numBytes + 36 >= 0x1_0000_0000)
			{
				// passed 4G limit, nothing to be done
				return;
			}

			try
			{
				_file.Seek(4, SeekOrigin.Begin);
				_file.Write((uint)(36 + _numBytes));
				_file.Seek(40, SeekOrigin.Begin);
				_file.Write((uint)(_numBytes));
			}
			catch (NotSupportedException)
			{
				// unseekable; oh well
			}
		}

		/// <summary>
		/// close current underlying stream
		/// </summary>
		private void CloseCurrent()
		{
			if (_file != null)
			{
				FinalizeHeaders();
				_file.Close();
				_file.Dispose();
			}

			_file = null;
		}

		/// <summary>
		/// open a new underlying stream
		/// </summary>
		private void OpenCurrent(Stream next)
		{
			_file = new BinaryWriter(next, Encoding.ASCII);
			_numBytes = 0;
			WriteHeaders();
		}

		/// <summary>
		/// write samples to file
		/// </summary>
		/// <param name="samples">samples to write; should contain one for each channel</param>
		public void WriteSamples(short[] samples)
		{
			_file.Write(samples);
			_numBytes += (ulong)(samples.Length * sizeof(short));

			// try splitting if we can
			if (_numBytes >= SplitPoint && _fileChain != null)
			{
				if (!_fileChain.MoveNext())
				{	// out of files, just keep on writing to this one
					_fileChain = null;
				}
				else
				{
					Stream next = _fileChain.Current;
					CloseCurrent();
					OpenCurrent(next);
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
			CloseCurrent();
		}

		/// <summary>
		/// checks sampling rate, number of channels for validity
		/// </summary>
		private void CheckArgs()
		{
			if (_sampleRate < 1 || _numChannels < 1)
			{
				throw new InvalidOperationException("Bad samplerate/numchannels");
			}
		}

		/// <summary>
		/// initializes WavWriter with a single output stream
		/// no attempt is made to split
		/// </summary>
		/// <param name="s">WavWriter now owns this stream</param>
		/// <param name="sampleRate">sampling rate in HZ</param>
		/// <param name="numChannels">number of audio channels</param>
		public WavWriter(Stream s, int sampleRate, int numChannels)
		{
			_sampleRate = sampleRate;
			_numChannels = numChannels;
			_fileChain = null;
			CheckArgs();
			OpenCurrent(s);
		}

		/// <summary>
		/// initializes WavWriter with an enumeration of Streams
		/// one is consumed every time 2G is hit
		/// if the enumerator runs out before the audio stream does, the last file could be >2G
		/// </summary>
		/// <param name="ss">WavWriter now owns any of these streams that it enumerates</param>
		/// <param name="sampleRate">sampling rate in HZ</param>
		/// <param name="numChannels">number of audio channels</param>
		/// <exception cref="ArgumentException"><paramref name="ss"/> cannot be progressed</exception>
		public WavWriter(IEnumerator<Stream> ss, int sampleRate, int numChannels)
		{
			_sampleRate = sampleRate;
			_numChannels = numChannels;
			CheckArgs();
			_fileChain = ss;

			// advance to first
			if (!_fileChain.MoveNext())
			{
				throw new ArgumentException(message: "Iterator was empty!", paramName: nameof(ss));
			}

			OpenCurrent(ss.Current);
		}
	}

	/// <summary>
	/// slim wrapper on WavWriter that implements IVideoWriter (discards all video!)
	/// </summary>
	[VideoWriter("wave", "WAV writer", "Writes a series of standard RIFF wav files containing uncompressed audio.  Does not write video.  Splits every 2G.")]
	public class WavWriterV : IVideoWriter
	{
		public void SetVideoCodecToken(IDisposable token) { }
		public void AddFrame(IVideoProvider source) { }
		public void SetMovieParameters(int fpsNum, int fpsDen) { }
		public void SetVideoParameters(int width, int height) { }
		public void SetFrame(int frame) { }

		public bool UsesAudio => true;

		public bool UsesVideo => false;

		private class WavWriterVToken : IDisposable
		{
			public void Dispose() { }
		}

		public IDisposable AcquireVideoCodecToken(Config config)
		{
			// don't care
			return new WavWriterVToken();
		}

		/// <exception cref="ArgumentException"><paramref name="bits"/> is not <c>16</c></exception>
		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			_sampleRate = sampleRate;
			_channels = channels;
			if (bits is not 16) throw new ArgumentException(message: "Only support 16bit audio!", paramName: nameof(bits));
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMs, ulong rerecords)
		{
			// not implemented
		}

		public void Dispose()
		{
			_wavWriter?.Dispose();
		}

		private WavWriter _wavWriter;
		private int _sampleRate;
		private int _channels;

		/// <summary>
		/// create a simple wav stream iterator
		/// </summary>
		private static IEnumerator<Stream> CreateStreamIterator(string template)
		{
			var (dir, baseName, ext) = template.SplitPathToDirFileAndExt();
			yield return new FileStream(template, FileMode.Create);
			int counter = 1;
			while (true)
			{
				yield return new FileStream($"{Path.Combine(dir ?? string.Empty, baseName)}_{counter}{ext}", FileMode.Create);
				counter++;
			}
		}

		public void OpenFile(string baseName)
		{
			_wavWriter = new WavWriter(CreateStreamIterator(baseName), _sampleRate, _channels);
		}

		public void CloseFile()
		{
			_wavWriter.Close();
			_wavWriter.Dispose();
			_wavWriter = null;
		}

		public void AddSamples(short[] samples)
		{
			_wavWriter.WriteSamples(samples);
		}

		public string DesiredExtension() => "wav";

		public void SetDefaultVideoCodecToken(Config config)
		{
			// don't use codec tokens, so don't care
		}
	}
}
