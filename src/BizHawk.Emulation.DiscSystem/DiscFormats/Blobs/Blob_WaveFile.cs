using System.Linq;
using System.IO;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// TODO - double-check that riffmaster is not filling memory at load-time but reading through to the disk
	/// TODO - clarify stream disposing semantics
	/// </summary>
	internal class Blob_WaveFile : IBlob
	{
		[Serializable]
		public class Blob_WaveFile_Exception : Exception
		{
			public Blob_WaveFile_Exception(string message)
				: base(message)
			{
			}
		}

		public Blob_WaveFile()
		{
		}

		public void Load(byte[] waveData)
		{
		}

		public void Load(string wavePath)
		{
			var stream = new FileStream(wavePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			Load(stream);
		}

		/// <exception cref="Blob_WaveFile_Exception">not a valid RIFF WAVE file with exactly one data chunk containing two 16-bit PCM channels at 44.1 kHz</exception>
		public void Load(Stream stream)
		{
			try
			{
				RiffSource = null;
				var rm = new RiffMaster();
				rm.LoadStream(stream);
				RiffSource = rm;

				//analyze the file to make sure its an OK wave file

				if (rm.riff.type != "WAVE")
				{
					throw new Blob_WaveFile_Exception("Not a RIFF WAVE file");
				}

				if (rm.riff.subchunks.Find(static chunk => chunk.tag == "fmt ") is not RiffMaster.RiffSubchunk_fmt fmt)
				{
					throw new Blob_WaveFile_Exception("Not a valid RIFF WAVE file (missing fmt chunk");
				}

				var dataChunks = rm.riff.subchunks.Where(chunk => chunk.tag == "data").ToList();
				if (dataChunks.Count != 1)
				{
					//later, we could make a Stream which would make an index of data chunks and walk around them
					throw new Blob_WaveFile_Exception("Multi-data-chunk WAVE files not supported");
				}

				if (fmt.format_tag != RiffMaster.RiffSubchunk_fmt.FORMAT_TAG.WAVE_FORMAT_PCM)
				{
					throw new Blob_WaveFile_Exception("Not a valid PCM WAVE file (only PCM is supported)");
				}

				if (fmt.channels != 2 || fmt.bitsPerSample != 16 || fmt.samplesPerSec != 44100)
				{
					throw new Blob_WaveFile_Exception("Not a CDA format WAVE file (conversion not yet supported)");
				}

				//acquire the start of the data chunk
				var dataChunk = (RiffMaster.RiffSubchunk) dataChunks[0];
				waveDataStreamPos = dataChunk.Position;
				Length = dataChunk.Length;
			}
			catch(Exception)
			{
				Dispose();
				throw;
			}
		}

		public int Read(long byte_pos, byte[] buffer, int offset, int count)
		{
			RiffSource.BaseStream.Position = byte_pos + waveDataStreamPos;
			return RiffSource.BaseStream.Read(buffer, offset, count);
		}

		private RiffMaster RiffSource;
		private long waveDataStreamPos;
		public long Length { get; private set; }

		public void Dispose()
		{
			RiffSource?.Dispose();
			RiffSource = null;
		}
	}
}
