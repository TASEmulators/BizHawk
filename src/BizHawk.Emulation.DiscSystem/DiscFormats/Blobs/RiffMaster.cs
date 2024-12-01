using System.IO;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Common;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Parses a RIFF file into a live data structure.
	/// References to large blobs remain mostly on disk in the file which RiffMaster keeps a reference too. Dispose it to close the file.
	/// You can modify blobs however you want and write the file back out to a new path, if you're careful (that was the original point of this)
	/// Please be sure to test round-tripping when you make any changes. This architecture is a bit tricky to use, but it works if you're careful.
	/// TODO - clarify stream disposing semantics
	/// </summary>
	internal class RiffMaster : IDisposable
	{
		public RiffMaster() { }

		public void WriteFile(string fname)
		{
			using FileStream fs = new(fname, FileMode.Create, FileAccess.Write, FileShare.Read);
			WriteStream(fs);
		}

		public Stream BaseStream;

		public void LoadFile(string fname)
		{
			LoadStream(
				new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read)
			);
		}

		public void Dispose()
		{
			BaseStream?.Dispose();
			BaseStream = null;
		}

		private static string ReadTag(BinaryReader br) =>
			string.Concat(br.ReadChar(), br.ReadChar(), br.ReadChar(), br.ReadChar());

		protected static void WriteTag(BinaryWriter bw, string tag)
		{
			for (var i = 0; i < 4; i++)
				bw.Write(tag[i]);
			bw.Flush();
		}

		public abstract class RiffChunk
		{
			public string tag;

			/// <summary>
			/// writes this chunk to the stream, including padding
			/// </summary>
			public abstract void WriteStream(Stream s);

			/// <summary>
			/// distinct from a size or a length, the `volume` is the volume of bytes occupied by the chunk on disk (accounting for padding).
			///
			/// </summary>
			public abstract long GetVolume();

			/// <summary>
			/// transforms into a derived class depending on tag
			/// </summary>
			public abstract RiffChunk Morph();
		}

		public class RiffSubchunk : RiffChunk
		{
			public long Position;
			public uint Length;
			public Stream Source;

			public override void WriteStream(Stream s)
			{
				BinaryWriter bw = new(s);
				WriteTag(bw, tag);
				bw.Write(Length);
				bw.Flush();

				Source.Position = Position;
				Util.CopyStream(Source, s, Length);

				//all chunks are supposed to be 16bit padded
				if (Length % 2 != 0)
					s.WriteByte(0);
			}

			public override long GetVolume()
			{
				long ret = Length;
				if (ret % 2 != 0) ret++;
				return ret;
			}

			public byte[] ReadAll()
			{
				var msSize = (int)Math.Min((long)int.MaxValue, Length);
				MemoryStream ms = new(msSize);
				Source.Position = Position;
				Util.CopyStream(Source, ms, Length);
				return ms.ToArray();
			}

			public override RiffChunk Morph()
			{
				switch (tag)
				{
					case "fmt ": return new RiffSubchunk_fmt(this);
				}
				return this;
			}
		}

		public class RiffSubchunk_fmt : RiffSubchunk
		{
			public enum FORMAT_TAG : ushort
			{
				WAVE_FORMAT_UNKNOWN = 0x0000,
				WAVE_FORMAT_PCM = 0x0001,
				WAVE_FORMAT_ADPCM = 0x0002,
				WAVE_FORMAT_ALAW = 0x0006,
				WAVE_FORMAT_MULAW = 0x0007,
				WAVE_FORMAT_OKI_ADPCM = 0x0010,
				WAVE_FORMAT_DIGISTD = 0x0015,
				WAVE_FORMAT_DIGIFIX = 0x0016,
				IBM_FORMAT_MULAW = 0x0101,
				IBM_FORMAT_ALAW = 0x0102,
				IBM_FORMAT_ADPCM = 0x0103,
			}

			public FORMAT_TAG format_tag;
			public ushort channels;
			public uint samplesPerSec;
			public uint avgBytesPerSec;
			public ushort blockAlign;
			public ushort bitsPerSample;

			public RiffSubchunk_fmt(RiffSubchunk origin)
			{
				tag = "fmt ";
				BinaryReader br = new(new MemoryStream(origin.ReadAll()));
				format_tag = (FORMAT_TAG)br.ReadUInt16();
				channels = br.ReadUInt16();
				samplesPerSec = br.ReadUInt32();
				avgBytesPerSec = br.ReadUInt32();
				blockAlign = br.ReadUInt16();
				bitsPerSample = br.ReadUInt16();
			}

			public override void WriteStream(Stream s)
			{
				Flush();
				base.WriteStream(s);
			}

			private void Flush()
			{
				MemoryStream ms = new();
				BinaryWriter bw = new(ms);
				bw.Write((ushort)format_tag);
				bw.Write(channels);
				bw.Write(samplesPerSec);
				bw.Write(avgBytesPerSec);
				bw.Write(blockAlign);
				bw.Write(bitsPerSample);
				bw.Flush();
				Source = ms;
				Position = 0;
				Length = (uint)ms.Length;
			}

			public override long GetVolume()
			{
				Flush();
				return base.GetVolume();
			}
		}

		public class RiffContainer : RiffChunk
		{
			public RiffChunk GetSubchunk(string tag, string type)
			{
				foreach (var rc in subchunks.Where(rc => rc.tag == tag))
				{
					if (type == null) return rc;
					if (rc is RiffContainer cont && cont.type == type) return cont;
				}

				return null;
			}

			public RiffContainer()
			{
				tag = "LIST";
			}

			public string type;
			public List<RiffChunk> subchunks = new();

			public override void WriteStream(Stream s)
			{
				BinaryWriter bw = new(s);
				WriteTag(bw, tag);
				var size = GetVolume();
				if (size > uint.MaxValue) throw new FormatException("File too big to write out");
				bw.Write((uint)size);
				WriteTag(bw, type);
				bw.Flush();
				foreach (var rc in subchunks)
					rc.WriteStream(s);
				if (size % 2 != 0)
					s.WriteByte(0);
			}

			public override long GetVolume()
			{
				return 4 + subchunks.Sum(rc => rc.GetVolume() + 8);
			}

			public override RiffChunk Morph()
			{
				switch (type)
				{
					case "INFO": return new RiffContainer_INFO(this);
				}
				return this;
			}
		}

		public class RiffContainer_INFO : RiffContainer
		{
			public readonly IDictionary<string, string> dictionary = new Dictionary<string, string>();
			public RiffContainer_INFO() { type = "INFO"; }

			/// <exception cref="FormatException"><paramref name="rc"/>.<see cref="RiffContainer.subchunks"/> contains a chunk that does not inherit <see cref="RiffSubchunk"/></exception>
			public RiffContainer_INFO(RiffContainer rc)
			{
				subchunks = rc.subchunks;
				type = "INFO";
				foreach (var chunk in subchunks)
				{
					if (chunk is not RiffSubchunk rsc) throw new FormatException("Invalid subchunk of INFO list");
					dictionary[rsc.tag] = System.Text.Encoding.ASCII.GetString(rsc.ReadAll());
				}
			}

			private void Flush()
			{
				subchunks.Clear();
				foreach (var (subchunkTag, s) in dictionary)
				{
					var rs = new RiffSubchunk
					{
						tag = subchunkTag,
						Source = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(s)),
						Position = 0
					};
					rs.Length = (uint)rs.Source.Length;
					subchunks.Add(rs);
				}
			}

			public override long GetVolume()
			{
				Flush();
				return base.GetVolume();
			}

			public override void WriteStream(Stream s)
			{
				Flush();
				base.WriteStream(s);
			}
		}

		public RiffContainer riff;
		private long readCounter;

		private RiffChunk ReadChunk(BinaryReader br)
		{
			RiffChunk ret;
			var tag = ReadTag(br); readCounter += 4;
			var size = br.ReadUInt32(); readCounter += 4;
			if (size > int.MaxValue)
				throw new FormatException("chunk too big");
			if (tag is "RIFF" or "LIST")
			{
				var rc = new RiffContainer
				{
					tag = tag,
					type = ReadTag(br)
				};

				readCounter += 4;
				var readEnd = readCounter - 4 + size;
				while (readEnd > readCounter)
					rc.subchunks.Add(ReadChunk(br));
				ret = rc.Morph();
			}
			else
			{
				var rsc = new RiffSubchunk
				{
					tag = tag,
					Source = br.BaseStream,
					Position = br.BaseStream.Position,
					Length = size
				};
				readCounter += size;
				br.BaseStream.Position += size;
				ret = rsc.Morph();
			}
			if (size % 2 != 0)
			{
				br.ReadByte();
				readCounter += 1;
			}

			return ret;
		}

		public void WriteStream(Stream s)
		{
			riff.WriteStream(s);
		}

		/// <summary>takes posession of the supplied stream</summary>
		/// <exception cref="FormatException"><paramref name="s"/> does not contain a riff chunk</exception>
		public void LoadStream(Stream s)
		{
			Dispose();
			BaseStream = s;
			readCounter = 0;
			BinaryReader br = new(s);
			var chunk = ReadChunk(br);
			if (chunk.tag != "RIFF") throw new FormatException("can't recognize riff chunk");
			riff = (RiffContainer)chunk;
		}
	}
}