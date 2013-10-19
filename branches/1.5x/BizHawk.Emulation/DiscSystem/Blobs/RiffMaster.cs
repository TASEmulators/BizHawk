using System;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Parses a RIFF file into a live data structure. 
/// References to large blobs remain mostly on disk in the file which RiffMaster keeps a reference too. Dispose it to close the file.
/// You can modify blobs however you want and write the file back out to a new path, if youre careful (that was the original point of this)
/// Please be sure to test round-tripping when you make any changes. This architecture is a bit tricky to use, but it works if youre careful.
/// </summary>
class RiffMaster : IDisposable
{
	public RiffMaster() { }

	public void WriteFile(string fname)
	{
		using (FileStream fs = new FileStream(fname, FileMode.Create, FileAccess.Write, FileShare.Read))
			WriteStream(fs);
	}

	public Stream BaseStream;
	public void LoadFile(string fname)
	{
		var fs = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read);
		LoadStream(fs);
	}

	public void Dispose()
	{
		if (BaseStream != null) BaseStream.Dispose();
		BaseStream = null;
	}

	private static string ReadTag(BinaryReader br)
	{
		return "" + br.ReadChar() + br.ReadChar() + br.ReadChar() + br.ReadChar();
	}

	protected static void WriteTag(BinaryWriter bw, string tag)
	{
		for (int i = 0; i < 4; i++)
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

	static class Util
	{
		public static void CopyStream(Stream src, Stream dest, long len)
		{
			const int size = 0x2000;
			byte[] buffer = new byte[size];
			while (len > 0)
			{
				long todo = len;
				if (len > size) todo = size;
				int n = src.Read(buffer, 0, (int)todo);
				dest.Write(buffer, 0, n);
				len -= n;
			}
		}
	}

	public class RiffSubchunk : RiffChunk
	{
		public long Position;
		public uint Length;
		public Stream Source;
		public override void WriteStream(Stream s)
		{
			BinaryWriter bw = new BinaryWriter(s);
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
			int msSize = (int)Math.Min((long)int.MaxValue, Length);
			MemoryStream ms = new MemoryStream(msSize);
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
			WAVE_FORMAT_UNKNOWN = (0x0000),
			WAVE_FORMAT_PCM = (0x0001),
			WAVE_FORMAT_ADPCM = (0x0002),
			WAVE_FORMAT_ALAW = (0x0006),
			WAVE_FORMAT_MULAW = (0x0007),
			WAVE_FORMAT_OKI_ADPCM = (0x0010),
			WAVE_FORMAT_DIGISTD = (0x0015),
			WAVE_FORMAT_DIGIFIX = (0x0016),
			IBM_FORMAT_MULAW = (0x0101),
			IBM_FORMAT_ALAW = (0x0102),
			IBM_FORMAT_ADPCM = (0x0103),
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
			BinaryReader br = new BinaryReader(new MemoryStream(origin.ReadAll()));
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
		void Flush()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
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
			foreach (RiffChunk rc in subchunks)
				if (rc.tag == tag)
				{
					if (type == null) return rc;
					RiffContainer cont = rc as RiffContainer;
					if (cont != null && cont.type == type)
						return rc;
				}
			return null;
		}

		public RiffContainer()
		{
			tag = "LIST";
		}
		public string type;
		public List<RiffChunk> subchunks = new List<RiffChunk>();
		public override void WriteStream(Stream s)
		{
			BinaryWriter bw = new BinaryWriter(s);
			WriteTag(bw, tag);
			long size = GetVolume();
			if (size > uint.MaxValue) throw new FormatException("File too big to write out");
			bw.Write((uint)size);
			WriteTag(bw, type);
			bw.Flush();
			foreach (RiffChunk rc in subchunks)
				rc.WriteStream(s);
			if (size % 2 != 0)
				s.WriteByte(0);
		}
		public override long GetVolume()
		{
			long len = 4;
			foreach (RiffChunk rc in subchunks)
				len += rc.GetVolume() + 8;
			return len;
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
		public Dictionary<string, string> dictionary = new Dictionary<string, string>();
		public RiffContainer_INFO() { type = "INFO"; }
		public RiffContainer_INFO(RiffContainer rc)
		{
			subchunks = rc.subchunks;
			type = "INFO";
			foreach (RiffChunk chunk in subchunks)
			{
				RiffSubchunk rsc = chunk as RiffSubchunk;
				if (chunk == null)
					throw new FormatException("Invalid subchunk of INFO list");
				dictionary[rsc.tag] = System.Text.Encoding.ASCII.GetString(rsc.ReadAll());
			}
		}

		private void Flush()
		{
			subchunks.Clear();
			foreach (KeyValuePair<string, string> kvp in dictionary)
			{
				RiffSubchunk rs = new RiffSubchunk();
				rs.tag = kvp.Key;
				rs.Source = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(kvp.Value));
				rs.Position = 0;
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
		string tag = ReadTag(br); readCounter += 4;
		uint size = br.ReadUInt32(); readCounter += 4;
		if (size > int.MaxValue)
			throw new FormatException("chunk too big");
		if (tag == "RIFF" || tag == "LIST")
		{
			RiffContainer rc = new RiffContainer();
			rc.tag = tag;
			rc.type = ReadTag(br); readCounter += 4;
			long readEnd = readCounter - 4 + size;
			while (readEnd > readCounter)
				rc.subchunks.Add(ReadChunk(br));
			ret = rc.Morph();
		}
		else
		{
			RiffSubchunk rsc = new RiffSubchunk();
			rsc.tag = tag;
			rsc.Source = br.BaseStream;
			rsc.Position = br.BaseStream.Position;
			rsc.Length = size;
			readCounter += size;
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

	/// <summary>
	/// takes posession of the supplied stream
	/// </summary>
	public void LoadStream(Stream s)
	{
		Dispose();
		BaseStream = s;
		readCounter = 0;
		BinaryReader br = new BinaryReader(s);
		RiffChunk chunk = ReadChunk(br);
		if (chunk.tag != "RIFF") throw new FormatException("can't recognize riff chunk");
		riff = (RiffContainer)chunk;
	}


}