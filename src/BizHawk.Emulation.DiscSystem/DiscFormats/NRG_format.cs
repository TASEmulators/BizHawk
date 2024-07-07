using System.Buffers.Binary;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.DiscSystem.CUE;

//Nero NRG images
//https://problemkaputt.de/psxspx-cdrom-disk-images-nrg-nero.htm
//https://github.com/cdemu/cdemu/blob/a3c1a20/libmirage/images/image-nrg/parser.c

namespace BizHawk.Emulation.DiscSystem
{
	public static class NRG_Format
	{
		/// <summary>
		/// Represents a NRG file, faithfully. Minimal interpretation of the data happens.
		/// May represent either a v1 or v2 NRG file
		/// </summary>
		public class NRGFile
		{
			/// <summary>
			/// File ID
			/// "NERO" for V1, "NER5" for V2
			/// </summary>
			public string FileID;

			/// <summary>
			/// Offset to first chunk size in bytes
			/// </summary>
			public long FileOffset;

			/// <summary>
			/// The CUES/CUEX chunks
			/// </summary>
			public readonly IList<NRGCue> Cues = new List<NRGCue>();

			/// <summary>
			/// The DAOI/DAOX chunks
			/// </summary>
			public readonly IList<NRGDAOTrackInfo> DAOTrackInfos = new List<NRGDAOTrackInfo>();

			/// <summary>
			/// The TINF/ETNF/ETN2 chunks
			/// </summary>
			public readonly IList<NRGTAOTrackInfo> TAOTrackInfos = new List<NRGTAOTrackInfo>();

			/// <summary>
			/// The RELO chunks
			/// </summary>
			public readonly IList<NRGRELO> RELOs = new List<NRGRELO>();

			/// <summary>
			/// The TOCT chunks
			/// </summary>
			public readonly IList<NRGTOCT> TOCTs = new List<NRGTOCT>();

			/// <summary>
			/// The SINF chunks
			/// </summary>
			public readonly IList<NRGSessionInfo> SessionInfos = new List<NRGSessionInfo>();

			/// <summary>
			/// The CDTX chunk
			/// </summary>
			public NRGCdText CdText;

			/// <summary>
			/// The MTYP chunk
			/// </summary>
			public NRGMediaType MediaType;

			/// <summary>
			/// The AFNM chunk
			/// </summary>
			public NRGFilenames Filenames;

			/// <summary>
			/// The VOLM chunk
			/// </summary>
			public NRGVolumeName VolumeName;

			/// <summary>
			/// The END! chunk
			/// </summary>
			public NRGEND End;
		}

		/// <summary>
		/// Represents a generic chunk from a NRG file
		/// </summary>
		public abstract class NRGChunk
		{
			/// <summary>
			/// The chunk ID
			/// </summary>
			public string ChunkID;

			/// <summary>
			/// The chunk size in bytes
			/// </summary>
			public int ChunkSize;
		}

		/// <summary>
		/// Represents a track index in CUES/CUEX chunk
		/// </summary>
		public class NRGTrackIndex
		{
			/// <summary>
			/// ADR/Control byte (LSBs = ADR, MSBs = Control)
			/// </summary>
			public byte ADRControl;

			/// <summary>
			/// Track number (00 = leadin, 01-99 = track n, AA = leadout)
			/// </summary>
			public BCD2 Track;

			/// <summary>
			/// Index (00 = pregap, 01+ = actual track)
			/// </summary>
			public BCD2 Index;

			/// <summary>
			/// LBA for the location of this track index, starts at -150
			/// </summary>
			public int LBA;
		}

		/// <summary>
		/// Represents a CUES/CUEX chunk from a NRG file
		/// </summary>
		public class NRGCue : NRGChunk
		{
			/// <summary>
			/// All of the track indices for this session
			/// Don't trust index0's LBA, it's probably wrong
			/// </summary>
			public readonly IList<NRGTrackIndex> TrackIndices = new List<NRGTrackIndex>();
		}

		/// <summary>
		/// Represents a track in a DAOI/DAOX chunk
		/// </summary>
		public class NRGDAOTrack
		{
			/// <summary>
			/// 12-letter/digit string (may be empty)
			/// </summary>
			public string Isrc;

			/// <summary>
			/// Sector size (depends on Mode)
			/// Note: some files will have all tracks use the same sector size
			/// So if you have different modes on tracks, this will be the largest mode size
			/// Of course, this means sectors on the file may just have padding
			/// </summary>
			public ushort SectorSize;

			/// <summary>
			/// 00 = Mode1 / 2048 byte sectors
			/// 02 = Mode2 Form1 / 2048 byte sectors
			/// 03 = Mode2 / 2336 byte sectors
			/// (nb: no$ reports this is Form1, libmirage reports this is Form2, doesn't matter with 2336 bytes anyways)
			/// 05 = Mode1 / 2352 byte sectors
			/// 06 = Mode2 / 2352 byte sectors
			/// 07 = Audio / 2352 byte sectors
			/// 0F = Mode1 / 2448 byte sectors
			/// 10 = Audio / 2448 byte sectors
			/// 11 = Mode2 / 2448 byte sectors
			/// </summary>
			public byte Mode;

			/// <summary>
			/// File offset to this track's pregap (index 0)
			/// </summary>
			public long PregapFileOffset;

			/// <summary>
			/// File offset to this track's actual data (index 1)
			/// </summary>
			public long TrackStartFileOffset;

			/// <summary>
			/// File offset to the end of this track (equal to next track pregap)
			/// </summary>
			public long TrackEndFileOffset;
		}

		/// <summary>
		/// Represents a DAOI/DAOX chunk from a NRG file
		/// </summary>
		public class NRGDAOTrackInfo : NRGChunk
		{
			/// <summary>
			/// 13-digit ASCII string (may be empty)
			/// </summary>
			public string Ean13CatalogNumber;

			/// <summary>
			/// Disk type (0x00 = Mode1 or Audio, 0x10 = CD-I (?), 0x20 = XA/Mode2)
			/// </summary>
			public byte DiskType;

			/// <summary>
			/// First track, non-BCD (1-99)
			/// </summary>
			public byte FirstTrack;

			/// <summary>
			/// Last track, non-BCD (1-99)
			/// </summary>
			public byte LastTrack;

			/// <summary>
			/// All of the tracks for this chunk
			/// </summary>
			public readonly IList<NRGDAOTrack> Tracks = new List<NRGDAOTrack>();
		}

		/// <summary>
		/// Represents a track in a TINF/ETNF/ETN2 chunk
		/// </summary>
		public class NRGTAOTrack
		{
			/// <summary>
			/// File offset to this track's data (presumably the start of the pregap)
			/// </summary>
			public long TrackFileOffset;

			/// <summary>
			/// Track length in bytes
			/// </summary>
			public ulong TrackLength;

			/// <summary>
			/// Same meaning as NRGDAOTrack's Mode
			/// </summary>
			public int Mode;

			/// <summary>
			/// Starting LBA for this track on the disc
			/// Not present for TINF chunks
			/// </summary>
			public int? StartLBA;
		}

		/// <summary>
		/// Represents a TINF/ETNF/ETN2 chunk
		/// </summary>
		public class NRGTAOTrackInfo : NRGChunk
		{
			/// <summary>
			/// All of the tracks for this chunk
			/// </summary>
			public readonly IList<NRGTAOTrack> Tracks = new List<NRGTAOTrack>();
		}

		/// <summary>
		/// Represents a RELO chunk
		/// </summary>
		public class NRGRELO : NRGChunk
		{
			// purpose is completely unknown
		}

		/// <summary>
		/// Represents a TOCT chunk
		/// </summary>
		public class NRGTOCT : NRGChunk
		{
			/// <summary>
			/// Disk type (0x00 = Mode1 or Audio, 0x10 = CD-I (?), 0x20 = XA/Mode2)
			/// </summary>
			public byte DiskType;
		}

		/// <summary>
		/// Represents a SINF chunk
		/// </summary>
		public class NRGSessionInfo : NRGChunk
		{
			/// <summary>
			/// Number of tracks in session
			/// </summary>
			public uint TrackCount;
		}

		/// <summary>
		/// Represents a CDTX chunk
		/// </summary>
		public class NRGCdText : NRGChunk
		{
			/// <summary>
			/// Raw 18-byte CD text packs
			/// </summary>
			public readonly IList<byte[]> CdTextPacks = new List<byte[]>();
		}

		/// <summary>
		/// Represents a MTYP chunk
		/// </summary>
		public class NRGMediaType : NRGChunk
		{
			/// <summary>
			/// Media Type
			/// </summary>
			public uint MediaType;
		}

		/// <summary>
		/// Represents a AFNM chunk
		/// </summary>
		public class NRGFilenames : NRGChunk
		{
			/// <summary>
			/// Filenames where the image originally came from
			/// </summary>
			public IList<string> Filenames = new List<string>();
		}

		/// <summary>
		/// Represents a VOLM chunk
		/// </summary>
		public class NRGVolumeName : NRGChunk
		{
			/// <summary>
			/// Volume Name
			/// </summary>
			public string VolumeName;
		}

		/// <summary>
		/// Represents a END! chunk
		/// </summary>
		public class NRGEND : NRGChunk
		{
			// Chunk size should always be 0
		}

		public class NRGParseException : Exception
		{
			public NRGParseException(string message) : base(message) { }
		}

		private static NRGCue ParseCueChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// CUES/CUEX is always a multiple of 8
			if (chunkSize % 8 != 0)
			{
				throw new NRGParseException("Malformed NRG format: CUE chunk was not a multiple of 8!");
			}

			// This shouldn't ever be 0
			if (chunkSize == 0)
			{
				throw new NRGParseException("Malformed NRG format: 0 sized CUE chunk!");
			}

			var v2 = chunkID == "CUEX";
			var ret = new NRGCue { ChunkID = chunkID, ChunkSize = chunkSize };
			for (var i = 0; i < chunkSize; i += 8)
			{
				var trackIndex = new NRGTrackIndex
				{
					ADRControl = chunkData[i + 0],
					Track = BCD2.FromBCD(chunkData[i + 1]),
					Index = BCD2.FromBCD(chunkData[i + 2]),
					// chunkData[i + 3] is probably padding
				};

				if (v2)
				{
					trackIndex.LBA = BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 4, sizeof(int)));
				}
				else
				{
					// chunkData[i + 4] is probably padding
					trackIndex.LBA = MSF.ToInt(chunkData[i + 5], chunkData[i + 6], chunkData[i + 7]) - 150;
				}

				ret.TrackIndices.Add(trackIndex);
			}

			return ret;
		}

		private static NRGDAOTrackInfo ParseDaoChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// base DAOI/DAOX is 22 bytes
			if (chunkSize < 22)
			{
				throw new NRGParseException("Malformed NRG format: DAO chunk is less than 22 bytes!");
			}

			var ret = new NRGDAOTrackInfo
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
				// chunkData[0..3] is usually a duplicate of chunkSize
				Ean13CatalogNumber = Encoding.ASCII.GetString(chunkData.Slice(4, 13)).TrimEnd('\0'),
				// chunkData[17] is probably padding
				DiskType = chunkData[18],
				// chunkData[19] is said to be "_num_sessions" by libmirage with a question mark as a comment
				// however, for a 2 session disc (DAOX), this seems to be 0 for the first session, then 1 for the next one
				// others report that this byte is "always 1" (presumably with single session discs)
				FirstTrack = chunkData[20],
				LastTrack = chunkData[21],
			};

			var v2 = chunkID == "DAOX";
			var ntracks = ret.LastTrack - ret.FirstTrack + 1;

			if (ntracks <= 0 || ret.FirstTrack is < 0 or > 99 || ret.LastTrack is < 0 or > 99)
			{
				throw new NRGParseException("Malformed NRG format: Corrupt track numbers in DAO chunk!");
			}

			// each track is 30 (DAOI) or 42 (DAOX) bytes
			if (chunkSize - 22 != ntracks * (v2 ? 42 : 30))
			{
				throw new NRGParseException("Malformed NRG format: DAO chunk size does not match number of tracks!");
			}

			for (var i = 22; i < chunkSize; i += v2 ? 42 : 30)
			{
				var track = new NRGDAOTrack
				{
					Isrc = Encoding.ASCII.GetString(chunkData.Slice(i, 12)).TrimEnd('\0'),
					SectorSize = BinaryPrimitives.ReadUInt16BigEndian(chunkData.Slice(i + 12, sizeof(ushort))),
					Mode = chunkData[i + 14]
				};

				if (v2)
				{
					track.PregapFileOffset = BinaryPrimitives.ReadInt64BigEndian(chunkData.Slice(i + 18, sizeof(long)));
					track.TrackStartFileOffset = BinaryPrimitives.ReadInt64BigEndian(chunkData.Slice(i + 26, sizeof(long)));
					track.TrackEndFileOffset = BinaryPrimitives.ReadInt64BigEndian(chunkData.Slice(i + 34, sizeof(long)));

					if (track.PregapFileOffset < 0 || track.TrackStartFileOffset < 0 || track.TrackEndFileOffset < 0)
					{
						throw new NRGParseException("Malformed NRG format: Negative file offsets in DAOX chunk!");
					}
				}
				else
				{
					track.PregapFileOffset = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 18, sizeof(uint)));
					track.TrackStartFileOffset = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 22, sizeof(uint)));
					track.TrackEndFileOffset = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 26, sizeof(uint)));
				}

				ret.Tracks.Add(track);
			}

			return ret;
		}

		private static NRGTAOTrackInfo ParseEtnChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// TINF is always a multiple of 12
			// ETNF is always a multiple of 20
			// ETN2 is always a multiple of 32

			var trackSize = chunkID switch
			{
				"TINF" => 12,
				"ETNF" => 20,
				"ETN2" => 32,
				_ => throw new InvalidOperationException()
			};

			if (chunkSize % trackSize != 0)
			{
				throw new NRGParseException($"Malformed NRG format: {chunkID} chunk was not a multiple of {trackSize}!");
			}

			var ret = new NRGTAOTrackInfo
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
			};

			for (var i = 0; i < chunkSize; i += trackSize)
			{
				var track = new NRGTAOTrack();

				if (chunkID == "ETN2")
				{
					track.TrackFileOffset = BinaryPrimitives.ReadInt64BigEndian(chunkData.Slice(i + 0, sizeof(long)));
					track.TrackLength = BinaryPrimitives.ReadUInt64BigEndian(chunkData.Slice(i + 8, sizeof(ulong)));
					track.Mode = BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 16, sizeof(int)));
					track.StartLBA = BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 20, sizeof(int)));
					// chunkData[24..31] is unknown

					if (track.TrackFileOffset < 0)
					{
						throw new NRGParseException("Malformed NRG format: Negative file offset in ETN2 chunk!");
					}
				}
				else
				{
					track.TrackFileOffset = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 0, sizeof(uint)));
					track.TrackLength = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Slice(i + 4, sizeof(uint)));
					track.Mode = BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 8, sizeof(int)));

					// not available in TINF chunks
					if (chunkID == "ETNF")
					{
						track.StartLBA = BinaryPrimitives.ReadInt32BigEndian(chunkData.Slice(i + 12, sizeof(int)));
						// chunkData[16..19] is unknown
					}
				}

				ret.Tracks.Add(track);
			}

			return ret;
		}

		private static NRGRELO ParseReloChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// RELO seems to be only 4 bytes large (although they're always all 0?)

			if (chunkSize != 4)
			{
				throw new NRGParseException("Malformed NRG format: RELO chunk was not 4 bytes!");
			}

			return new()
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
			};
		}

		private static NRGTOCT ParseToctChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// TOCT is always 2 bytes large

			if (chunkSize != 2)
			{
				throw new NRGParseException("Malformed NRG format: TOCT chunk was not 2 bytes!");
			}

			return new()
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
				DiskType = chunkData[0],
				// chunkData[1] is probably padding (always 0?)
			};
		}

		private static NRGSessionInfo ParseSinfChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// SINF is always 4 bytes large

			if (chunkSize != 4)
			{
				throw new NRGParseException("Malformed NRG format: SINF chunk was not 4 bytes!");
			}

			return new()
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
				TrackCount = BinaryPrimitives.ReadUInt32BigEndian(chunkData),
			};
		}

		private static NRGCdText ParseCdtxChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// CDTX is always a multiple of 18
			if (chunkSize % 18 != 0)
			{
				throw new NRGParseException("Malformed NRG format: CDTX chunk was not a multiple of 18!");
			}

			// might be legal to have a 0 sized CDTX chunk?

			var ret = new NRGCdText
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
			};

			for (var i = 0; i < chunkSize; i += 18)
			{
				ret.CdTextPacks.Add(chunkData.Slice(i, 18).ToArray());
			}

			return ret;
		}

		private static NRGMediaType ParseMtypChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// MTYP is always 4 bytes large

			if (chunkSize != 4)
			{
				throw new NRGParseException("Malformed NRG format: MTYP chunk was not 4 bytes!");
			}

			return new()
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
				MediaType = BinaryPrimitives.ReadUInt32BigEndian(chunkData),
			};
		}

		private static NRGFilenames ParseAfnmChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// AFNM just contains list of null terminated strings

			if (chunkSize == 0 || chunkData[chunkSize - 1] != 0)
			{
				throw new NRGParseException("Malformed NRG format: Missing null terminator in AFNM chunk!");
			}

			var ret = new NRGFilenames
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
			};

			for (var i = 0; i < chunkSize;)
			{
				var j = 0;
				while (chunkData[i + j] != 0)
				{
					j++;
				}

				ret.Filenames.Add(Encoding.ASCII.GetString(chunkData.Slice(i, j)));
				i += j + 1;
			}

			return ret;
		}

		private static NRGVolumeName ParseVolmChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// VOLM just contains a null terminated string

			if (chunkSize == 0 || chunkData[chunkSize - 1] != 0)
			{
				throw new NRGParseException("Malformed NRG format: Missing null terminator in VOLM chunk!");
			}

			return new()
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
				VolumeName = Encoding.ASCII.GetString(chunkData).TrimEnd('\0'),
			};
		}

		private static NRGEND ParseEndChunk(string chunkID, int chunkSize, ReadOnlySpan<byte> chunkData)
		{
			// END! is always 0 bytes large

			if (chunkSize != 0)
			{
				throw new NRGParseException("Malformed NRG format: END! chunk was not 0 bytes!");
			}

			return new()
			{
				ChunkID = chunkID,
				ChunkSize = chunkSize,
			};
		}

		/// <exception cref="NRGParseException">malformed nrg format</exception>
		public static NRGFile ParseFrom(Stream stream)
		{
			var nrgf = new NRGFile();
			using var br = new BinaryReader(stream);

			try
			{
				stream.Seek(-12, SeekOrigin.End);
				nrgf.FileID = br.ReadStringFixedUtf8(4);
				if (nrgf.FileID == "NER5")
				{
					nrgf.FileOffset = br.ReadInt64();
					if (BitConverter.IsLittleEndian)
					{
						nrgf.FileOffset = BinaryPrimitives.ReverseEndianness(nrgf.FileOffset);
					}

					// suppose technically you can interpret this as ulong
					// but streams seek with long, and a CD can't be millions of TB anyways
					if (nrgf.FileOffset < 0)
					{
						throw new NRGParseException("Malformed NRG format: Chunk file offset was negative!");
					}
				}
				else
				{
					nrgf.FileID = br.ReadStringFixedUtf8(4);
					if (nrgf.FileID != "NERO")
					{
						throw new NRGParseException("Malformed NRG format: Could not find NERO/NER5 signature!");
					}

					nrgf.FileOffset = br.ReadUInt32();
					if (BitConverter.IsLittleEndian)
					{
						nrgf.FileOffset = BinaryPrimitives.ReverseEndianness(nrgf.FileOffset);
					}
				}

				stream.Seek(nrgf.FileOffset, SeekOrigin.Begin);

				void AssertIsV1()
				{
					if (nrgf.FileID != "NERO")
					{
						throw new NRGParseException("Malformed NRG format: Found V1 chunk in a V2 file!");
					}
				}

				void AssertIsV2()
				{
					if (nrgf.FileID != "NER5")
					{
						throw new NRGParseException("Malformed NRG format: Found V2 chunk in a V1 file!");
					}
				}

				while (nrgf.End is null)
				{
					var chunkID = br.ReadStringFixedUtf8(4);
					var chunkSize = br.ReadInt32();
					if (BitConverter.IsLittleEndian)
					{
						chunkSize = BinaryPrimitives.ReverseEndianness(chunkSize);
					}

					// can interpret this as uint rather
					// but chunks should never reach 2 GB anyways
					if (chunkSize < 0)
					{
						throw new NRGParseException("Malformed NRG format: Chunk size was negative!");
					}

					var chunkData = br.ReadBytes(chunkSize);

					if (chunkData.Length != chunkSize)
					{
						throw new NRGParseException("Malformed NRG format: Unexpected stream end!");
					}

					switch (chunkID)
					{
						case "CUES":
							AssertIsV1();
							nrgf.Cues.Add(ParseCueChunk(chunkID, chunkSize, chunkData));
							break;
						case "CUEX":
							AssertIsV2();
							nrgf.Cues.Add(ParseCueChunk(chunkID, chunkSize, chunkData));
							break;
						case "DAOI":
							AssertIsV1();
							nrgf.DAOTrackInfos.Add(ParseDaoChunk(chunkID, chunkSize, chunkData));
							break;
						case "DAOX":
							AssertIsV2();
							nrgf.DAOTrackInfos.Add(ParseDaoChunk(chunkID, chunkSize, chunkData));
							break;
						case "TINF":
						case "ETNF":
							AssertIsV1();
							nrgf.TAOTrackInfos.Add(ParseEtnChunk(chunkID, chunkSize, chunkData));
							break;
						case "ETN2":
							AssertIsV2();
							nrgf.TAOTrackInfos.Add(ParseEtnChunk(chunkID, chunkSize, chunkData));
							break;
						case "RELO":
							AssertIsV2();
							nrgf.RELOs.Add(ParseReloChunk(chunkID, chunkSize, chunkData));
							break;
						case "TOCT":
							AssertIsV2();
							nrgf.TOCTs.Add(ParseToctChunk(chunkID, chunkSize, chunkData));
							break;
						case "SINF":
							nrgf.SessionInfos.Add(ParseSinfChunk(chunkID, chunkSize, chunkData));
							break;
						case "CDTX":
							AssertIsV2();
							if (nrgf.CdText is not null)
							{
								throw new NRGParseException("Malformed NRG format: Found multiple CD text chunks!");
							}
							nrgf.CdText = ParseCdtxChunk(chunkID, chunkSize, chunkData);
							break;
						case "MTYP":
							if (nrgf.MediaType is not null)
							{
								throw new NRGParseException("Malformed NRG format: Found multiple media type chunks!");
							}
							nrgf.MediaType = ParseMtypChunk(chunkID, chunkSize, chunkData);
							break;
						case "AFNM":
							if (nrgf.Filenames is not null)
							{
								throw new NRGParseException("Malformed NRG format: Found multiple filenames chunks!");
							}
							nrgf.Filenames = ParseAfnmChunk(chunkID, chunkSize, chunkData);
							break;
						case "VOLM":
							if (nrgf.VolumeName is not null)
							{
								throw new NRGParseException("Malformed NRG format: Found multiple volume name chunks!");
							}
							nrgf.VolumeName = ParseVolmChunk(chunkID, chunkSize, chunkData);
							break;
						case "END!":
							nrgf.End = ParseEndChunk(chunkID, chunkSize, chunkData);
							break;
						default:
							Console.WriteLine($"Unknown NRG chunk {chunkID} encountered");
							break;
					}
				}

				// sanity checks

				// SessionInfos will be empty if there is only 1 session
				var nsessions = Math.Max(nrgf.SessionInfos.Count, 1);

				if (nrgf.Cues.Count != nsessions)
				{
					throw new NRGParseException("Malformed NRG format: CUE chunk count does not match session count!");
				}

				if (nrgf.DAOTrackInfos.Count > 0)
				{
					if (nrgf.TAOTrackInfos.Count is not 0 || nrgf.RELOs.Count is not 0 || nrgf.TOCTs.Count is not 0)
					{
						throw new NRGParseException("Malformed NRG format: DAO and TAO chunks both present on file!");
					}

					if (nrgf.DAOTrackInfos.Count != nsessions)
					{
						throw new NRGParseException("Malformed NRG format: DAO chunk count does not match session count!");
					}
				}
				else
				{
					if (nrgf.TAOTrackInfos.Count != nsessions)
					{
						throw new NRGParseException("Malformed NRG format: TAO chunk count does not match session count!");
					}

					// don't know if RELOs are per session or only one should be present...

					if (nrgf.TOCTs.Count != nsessions)
					{
						throw new NRGParseException("Malformed NRG format: TOCT chunk count does not match session count!");
					}
				}

				return nrgf;
			}
			catch (EndOfStreamException)
			{
				throw new NRGParseException("Malformed NRG format: Unexpected stream end!");
			}
		}

		public class LoadResults
		{
			public NRGFile ParsedNRGFile;
			public bool Valid;
			public NRGParseException FailureException;
			public string NrgPath;
		}

		public static LoadResults LoadNRGPath(string path)
		{
			var ret = new LoadResults
			{
				NrgPath = path
			};
			try
			{
				if (!File.Exists(path)) throw new NRGParseException("Malformed NRG format: nonexistent NRG file!");

				NRGFile nrgf;
				using (var infNRG = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
					nrgf = ParseFrom(infNRG);

				ret.ParsedNRGFile = nrgf;
				ret.Valid = true;
			}
			catch (NRGParseException ex)
			{
				ret.FailureException = ex;
			}

			return ret;
		}

		/// <exception cref="NRGParseException">file <paramref name="nrgPath"/> not found, or other parsing error</exception>
		public static Disc LoadNRGToDisc(string nrgPath, DiscMountPolicy IN_DiscMountPolicy)
		{
			var loadResults = LoadNRGPath(nrgPath);
			if (!loadResults.Valid)
				throw loadResults.FailureException;

			var disc = new Disc();
			var nrgf = loadResults.ParsedNRGFile;

			IBlob nrgBlob = new Blob_RawFile { PhysicalPath = nrgPath };
			disc.DisposableResources.Add(nrgBlob);

			// SessionInfos will be empty if there is only 1 session
			var nsessions = Math.Max(nrgf.SessionInfos.Count, 1);
			var dao = nrgf.DAOTrackInfos.Count > 0; // tao otherwise

			for (var i = 0; i < nsessions; i++)
			{
				var session = new DiscSession { Number = i + 1 };

				int startTrack, endTrack;
				SessionFormat sessionFormat;
				if (dao)
				{
					startTrack = nrgf.DAOTrackInfos[i].FirstTrack;
					endTrack = nrgf.DAOTrackInfos[i].LastTrack;
					sessionFormat = (SessionFormat)nrgf.DAOTrackInfos[i].DiskType;
				}
				else
				{
					startTrack = 1 + nrgf.TAOTrackInfos.Take(i).Sum(t => t.Tracks.Count);
					endTrack = startTrack + nrgf.TAOTrackInfos[i].Tracks.Count - 1;
					sessionFormat = (SessionFormat)nrgf.TOCTs[i].DiskType;
				}

				var TOCMiscInfo = new Synthesize_A0A1A2_Job(
					firstRecordedTrackNumber: startTrack,
					lastRecordedTrackNumber: endTrack,
					sessionFormat: sessionFormat,
					leadoutTimestamp: nrgf.Cues[i].TrackIndices.First(t => t.Track.BCDValue == 0xAA).LBA + 150);
				TOCMiscInfo.Run(session.RawTOCEntries);

				foreach (var trackIndex in nrgf.Cues[i].TrackIndices)
				{
					if (trackIndex.Track.BCDValue is not (0 or 0xAA) && trackIndex.Index.BCDValue == 1)
					{
						var q = default(SubchannelQ);
						q.q_status = trackIndex.ADRControl;
						q.q_tno = BCD2.FromBCD(0);
						q.q_index = trackIndex.Track;
						q.Timestamp = 0;
						q.zero = 0;
						q.AP_Timestamp = trackIndex.LBA + 150;
						q.q_crc = 0;
						session.RawTOCEntries.Add(new() { QData = q });
					}
				}

				// leadin track
				var leadinSize = i == 0 ? 0 : 4500;
				var isData = (session.RawTOCEntries.First(t => t.QData.q_index.DecimalValue == startTrack).QData.ADR & 4) != 0;
				for (var j = 0; j < leadinSize; j++)
				{
					// this is most certainly wrong
					// nothing relies on the exact contents for now (only multisession core is VirtualJaguar which doesn't touch leadin)
					// but this will allow the correct TOC to be generated
					var cueTrackType = CueTrackType.Audio;
					if (isData)
					{
						cueTrackType = sessionFormat switch
						{
							SessionFormat.Type00_CDROM_CDDA => CueTrackType.Mode1_2352,
							SessionFormat.Type10_CDI => CueTrackType.CDI_2352,
							SessionFormat.Type20_CDXA => CueTrackType.Mode2_2352,
							_ => cueTrackType
						};
					}
					disc._Sectors.Add(new SS_Gap
					{
						Policy = IN_DiscMountPolicy,
						TrackType = cueTrackType,
					});
				}

				static SS_Base CreateSynth(int mode)
				{
					return mode switch
					{
						0x00 => new SS_Mode1_2048(),
						0x02 => new SS_Mode2_Form1_2048(),
						0x03 => new SS_Mode2_2336(),
						0x05 or 0x06 or 0x07 => new SS_2352(),
						0x0F or 0x10 or 0x11 => new SS_2448_Interleaved(),
						_ => throw new InvalidOperationException($"Invalid mode {mode}")
					};
				}

				if (dao)
				{
					var tracks = nrgf.DAOTrackInfos[i].Tracks;
					for (var j = 0; j < tracks.Count; j++)
					{
						var track = nrgf.DAOTrackInfos[i].Tracks[j];
						var relMSF = -(track.TrackStartFileOffset - track.PregapFileOffset) / track.SectorSize;
						var trackNumBcd = BCD2.FromDecimal(startTrack + j);
						var cueIndexes = nrgf.Cues[i].TrackIndices.Where(t => t.Track == trackNumBcd).ToArray();
	
						// do the pregap
						var pregapCueIndex = cueIndexes[0];
						for (var k = track.PregapFileOffset; k < track.TrackStartFileOffset; k += track.SectorSize)
						{
							var synth = CreateSynth(track.Mode);
							synth.Blob = nrgBlob;
							synth.BlobOffset = k;
							synth.Policy = IN_DiscMountPolicy;
							synth.sq.q_status = pregapCueIndex.ADRControl;
							synth.sq.q_tno = trackNumBcd;
							synth.sq.q_index = BCD2.FromBCD(0);
							synth.sq.Timestamp = !IN_DiscMountPolicy.CUE_PregapContradictionModeA
								? (int)relMSF + 1
								: (int)relMSF;
							synth.sq.zero = 0;
							synth.sq.AP_Timestamp = disc._Sectors.Count;
							synth.sq.q_crc = 0;
							synth.Pause = true;
							disc._Sectors.Add(synth);
							relMSF++;
						}

						// actual data
						var curIndex = 1;
						for (var k = track.TrackStartFileOffset; k < track.TrackEndFileOffset; k += track.SectorSize)
						{
							if (curIndex + 1 != cueIndexes.Length && disc._Sectors.Count == cueIndexes[curIndex + 1].LBA + 150)
							{
								curIndex++;
							}

							var synth = CreateSynth(track.Mode);
							synth.Blob = nrgBlob;
							synth.BlobOffset = k;
							synth.Policy = IN_DiscMountPolicy;
							synth.sq.q_status = cueIndexes[curIndex].ADRControl;
							synth.sq.q_tno = trackNumBcd;
							synth.sq.q_index = cueIndexes[curIndex].Index;
							synth.sq.Timestamp = (int)relMSF;
							synth.sq.zero = 0;
							synth.sq.AP_Timestamp = disc._Sectors.Count;
							synth.sq.q_crc = 0;
							synth.Pause = false;
							disc._Sectors.Add(synth);
							relMSF++;
						}
					}
				}
				else // TAO
				{
					// TODO
					throw new NotSupportedException("TAO not supported yet!");
				}

				// leadout track
				// first leadout is 6750 sectors, later ones are 2250 sectors
				var leadoutSize = i == 0 ? 6750 : 2250;
				for (var j = 0; j < leadoutSize; j++)
				{
					disc._Sectors.Add(new SS_Leadout
					{
						SessionNumber = session.Number,
						Policy = IN_DiscMountPolicy
					});
				}

				disc.Sessions.Add(session);
			}

			return disc;
		}
	}
}