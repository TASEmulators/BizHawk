using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Kinds of IPF data-area stream element (dataType, 5 LSB of the dataHead byte).
	/// </summary>
	public enum IpfDataType
	{
		End = 0,
		Sync = 1,
		Data = 2,
		Gap = 3,
		Raw = 4,
		Fuzzy = 5,
	}

	/// <summary>
	/// One IPF data-stream element, already tokenized (Fuzzy carries no sample - generate random).
	/// </summary>
	public sealed class IpfDataElement
	{
		public IpfDataType Type;
		public int Size;          // sample size, in bytes or bits per the block's DataInBit flag
		public bool SizeInBits;   // interpretation of Size
		public byte[] Sample = System.Array.Empty<byte>(); // empty for Fuzzy
	}

	/// <summary>
	/// One IPF gap-stream element (GapLength = repeat count, or SampleLength = a bit sample).
	/// </summary>
	public sealed class IpfGapElement
	{
		public int ElemType;      // 1 = GapLength (repeat count), 2 = SampleLength (bit sample)
		public int Size;          // repeat count (type 1) or sample size in bits (type 2)
		public byte[] Sample = System.Array.Empty<byte>(); // only for type 2
	}

	/// <summary>
	/// A DATA-record block descriptor (32 bytes) plus its decoded data-stream elements.
	/// </summary>
	public sealed class IpfBlockDescriptor
	{
		public int DataBits;
		public int GapBits;
		public int DataBytesOrGapOffset; // CAPS: dataBytes; SPS: gapOffset
		public int GapBytesOrCellType;   // CAPS: gapBytes; SPS: cellType
		public int EncoderType;          // 0 unknown, 1 MFM, 2 raw
		public int BlockFlags;           // bit0 ForwardGap, bit1 BackwardGap, bit2 DataInBit
		public int GapDefault;
		public int DataOffset;

		public bool DataInBit => (BlockFlags & 0x04) != 0;

		public List<IpfDataElement> DataElements { get; } = new List<IpfDataElement>();
	}

	/// <summary>
	/// IPF INFO record.
	/// </summary>
	public sealed class IpfInfo
	{
		public int MediaType, EncoderType, EncoderRev, FileKey, FileRev, Origin;
		public int MinTrack, MaxTrack, MinSide, MaxSide;
		public int[] Platforms = new int[4];
	}

	/// <summary>
	/// IPF IMGE record: describes one track/side and points at a DATA record by DataKey.
	/// </summary>
	public sealed class IpfImage
	{
		public int Track, Side, Density, SignalType;
		public int TrackBytes, StartBytePos, StartBitPos;
		public int DataBits, GapBits, TrackBits, BlockCount;
		public int TrackFlags, DataKey;

		public bool Fuzzy => (TrackFlags & 0x01) != 0;
	}

	/// <summary>
	/// IPF DATA record: the block descriptors + stream data for the matching IMGE (by DataKey).
	/// </summary>
	public sealed class IpfDataRecord
	{
		public int DataKey;
		public bool ExtraCrcOk;
		public List<IpfBlockDescriptor> Blocks { get; } = new List<IpfBlockDescriptor>();
	}

	/// <summary>
	/// Parsed IPF file: the INFO block plus IMGE records and DATA records keyed by DataKey.
	/// </summary>
	public sealed class IpfDisk
	{
		public IpfInfo Info;
		public List<IpfImage> Images { get; } = new List<IpfImage>();
		public Dictionary<int, IpfDataRecord> Data { get; } = new Dictionary<int, IpfDataRecord>();
		public bool AllCrcOk = true;
	}

	/// <summary>
	/// IPF container parser (record layer). Walks the CAPS/INFO/IMGE/DATA records, validates the
	/// record and data-block CRC32s, and tokenizes each block's data-stream elements. Rolling the decoded
	/// stream into flux cells is a separate step. Big-endian throughout.
	/// </summary>
	public static class IpfConverter
	{
		public static bool IsIpf(byte[] d)
			=> d != null && d.Length >= 12 && d[0] == (byte)'C' && d[1] == (byte)'A' && d[2] == (byte)'P' && d[3] == (byte)'S';

		public static IpfDisk Parse(byte[] d)
		{
			if (!IsIpf(d)) throw new System.ArgumentException("not an IPF file (no CAPS record)", nameof(d));

			var disk = new IpfDisk();
			int pos = 0;
			while (pos + 12 <= d.Length)
			{
				string type = System.Text.Encoding.ASCII.GetString(d, pos, 4);
				int length = ReadBe(d, pos + 4);
				int crc = ReadBe(d, pos + 8);
				if (length < 12 || pos + length > d.Length) break;

				uint calc = Crc32Iso.ComputeWithZeroedField(d, pos, length, pos + 8);
				bool headerCrcOk = calc == (uint)crc;
				if (!headerCrcOk) disk.AllCrcOk = false;

				switch (type)
				{
					case "CAPS":
						break;
					case "INFO":
						disk.Info = ParseInfo(d, pos + 12);
						break;
					case "IMGE":
						disk.Images.Add(ParseImage(d, pos + 12));
						break;
					case "DATA":
						pos = ParseData(d, pos, disk);
						continue; // ParseData returns the next record position (past the extra data block)
					default:
						break; // unknown record - skip by length
				}
				pos += length;
			}
			return disk;
		}

		private static IpfInfo ParseInfo(byte[] d, int o) => new()
		{
			MediaType = ReadBe(d, o),
			EncoderType = ReadBe(d, o + 4),
			EncoderRev = ReadBe(d, o + 8),
			FileKey = ReadBe(d, o + 12),
			FileRev = ReadBe(d, o + 16),
			Origin = ReadBe(d, o + 20),
			MinTrack = ReadBe(d, o + 24),
			MaxTrack = ReadBe(d, o + 28),
			MinSide = ReadBe(d, o + 32),
			MaxSide = ReadBe(d, o + 36),
			Platforms = new[] { ReadBe(d, o + 48), ReadBe(d, o + 52), ReadBe(d, o + 56), ReadBe(d, o + 60) },
		};

		private static IpfImage ParseImage(byte[] d, int o) => new()
		{
			Track = ReadBe(d, o),
			Side = ReadBe(d, o + 4),
			Density = ReadBe(d, o + 8),
			SignalType = ReadBe(d, o + 12),
			TrackBytes = ReadBe(d, o + 16),
			StartBytePos = ReadBe(d, o + 20),
			StartBitPos = ReadBe(d, o + 24),
			DataBits = ReadBe(d, o + 28),
			GapBits = ReadBe(d, o + 32),
			TrackBits = ReadBe(d, o + 36),
			BlockCount = ReadBe(d, o + 40),
			TrackFlags = ReadBe(d, o + 48),
			DataKey = ReadBe(d, o + 52),
		};

		// Returns the position of the next record (past the extra data block).
		private static int ParseData(byte[] d, int recordStart, IpfDisk disk)
		{
			int headerLen = ReadBe(d, recordStart + 4);            // 28 (record header + data block)
			int o = recordStart + 12;                              // start of the data block
			int extraLen = ReadBe(d, o);                           // Extra Data Block size
			int extraCrc = ReadBe(d, o + 8);
			int dataKey = ReadBe(d, o + 12);
			int extraStart = recordStart + headerLen;              // Extra Data Block begins after the 28-byte header
			int next = extraStart + extraLen;
			if (extraLen <= 0 || next > d.Length) return recordStart + headerLen;

			var rec = new IpfDataRecord { DataKey = dataKey };
			rec.ExtraCrcOk = Crc32Iso.Compute(d, extraStart, extraLen) == (uint)extraCrc;
			if (!rec.ExtraCrcOk) disk.AllCrcOk = false;

			// block count comes from the matching IMGE record
			int blockCount = 0;
			foreach (var img in disk.Images) if (img.DataKey == dataKey) { blockCount = img.BlockCount; break; }

			for (int b = 0; b < blockCount; b++)
			{
				int bd = extraStart + b * 32;
				if (bd + 32 > d.Length) break;
				var block = new IpfBlockDescriptor
				{
					DataBits = ReadBe(d, bd),
					GapBits = ReadBe(d, bd + 4),
					DataBytesOrGapOffset = ReadBe(d, bd + 8),
					GapBytesOrCellType = ReadBe(d, bd + 12),
					EncoderType = ReadBe(d, bd + 16),
					BlockFlags = ReadBe(d, bd + 20),
					GapDefault = ReadBe(d, bd + 24),
					DataOffset = ReadBe(d, bd + 28),
				};
				if (block.DataBits > 0 && block.DataOffset > 0)
					ReadDataElements(d, extraStart + block.DataOffset, next, block);
				rec.Blocks.Add(block);
			}

			disk.Data[dataKey] = rec;
			return next;
		}

		// Tokenize a block's data-stream elements until the terminating null dataHead (or the block end).
		private static void ReadDataElements(byte[] d, int start, int limit, IpfBlockDescriptor block)
		{
			int p = start;
			while (p < limit)
			{
				byte head = d[p++];
				if (head == 0) break; // null dataHead terminates the list
				int sizeWidth = (head >> 5) & 0x07;
				var type = (IpfDataType)(head & 0x1F);
				int size = 0;
				for (int i = 0; i < sizeWidth && p < limit; i++) size = (size << 8) | d[p++];

				var elem = new IpfDataElement { Type = type, Size = size, SizeInBits = block.DataInBit };
				if (type != IpfDataType.Fuzzy)
				{
					int sampleBytes = block.DataInBit ? (size + 7) / 8 : size;
					if (sampleBytes < 0 || p + sampleBytes > limit) break;
					elem.Sample = new byte[sampleBytes];
					System.Array.Copy(d, p, elem.Sample, 0, sampleBytes);
					p += sampleBytes;
				}
				block.DataElements.Add(elem);
			}
		}

		private static int ReadBe(byte[] d, int o)
			=> (d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3];

		// ---- flux generation ----

		/// <summary>
		/// Roll one decoded IMGE/DATA track into MFM flux cells. Within an MFM block, Sync/Raw stream samples
		/// are raw cells (8 cells per sample byte), Data/Gap samples are MFM-encoded (16 cells per byte), and
		/// Fuzzy elements become weak cells. Blocks are laid down in order, each followed by its inter-block
		/// gap filled with 0x4E to the recorded gap length. Returns null for an empty/unformatted track.
		/// </summary>
		public static MfmTrack BuildFluxTrack(IpfImage img, IpfDataRecord data)
		{
			if (img == null || data == null || img.BlockCount == 0 || img.DataBits == 0) return null;

			var w = new MfmTrackWriter();
			foreach (var block in data.Blocks)
			{
				foreach (var e in block.DataElements)
				{
					switch (e.Type)
					{
						case IpfDataType.Sync:
						case IpfDataType.Raw:
							w.WriteRawCells(e.Sample, e.SizeInBits ? e.Size : e.Size * 8);
							break;
						case IpfDataType.Data:
						case IpfDataType.Gap:
							if (e.SizeInBits) w.WriteRawCells(e.Sample, e.Size);
							else foreach (var b in e.Sample) w.WriteByte(b);
							break;
						case IpfDataType.Fuzzy:
							w.WriteWeakCells(e.SizeInBits ? e.Size : e.Size * 16);
							break;
					}
				}
				EmitGap(w, block.GapBits);
			}
			return w.Build();
		}

		// Fill an inter-block gap of the given cell length with MFM-encoded 0x4E (the IBM gap byte).
		private static void EmitGap(MfmTrackWriter w, int gapCells)
		{
			int fullBytes = gapCells / 16;
			for (int i = 0; i < fullBytes; i++) w.WriteByte(0x4E);
			int rem = gapCells - fullBytes * 16;
			if (rem > 0) w.WriteRawCells(System.Array.Empty<byte>(), rem);
		}
	}
}
