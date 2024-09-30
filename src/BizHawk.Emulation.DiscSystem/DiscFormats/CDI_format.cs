using System.IO;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.DiscSystem.CUE;

//DiscJuggler CDI images
//https://problemkaputt.de/psxspx-cdrom-disk-images-cdi-discjuggler.htm
//https://github.com/cdemu/cdemu/blob/1f90f74/libmirage/images/image-cdi/parser.c

namespace BizHawk.Emulation.DiscSystem
{
	public static class CDI_Format
	{
		/// <summary>
		/// Represents a CDI file, faithfully. Minimal interpretation of the data happens.
		/// </summary>
		public class CDIFile
		{
			/// <summary>
			/// Number of sessions
			/// </summary>
			public byte NumSessions;

			/// <summary>
			/// The session blocks
			/// </summary>
			public readonly List<CDISession> Sessions = [ ];

			/// <summary>
			/// The track blocks
			/// </summary>
			public readonly List<CDITrack> Tracks = [ ];
			
			/// <summary>
			/// The disc info block
			/// </summary>
			public readonly CDIDiscInfo DiscInfo = new();

			/// <summary>
			/// Footer size in bytes
			/// </summary>
			public uint Entrypoint;
		}

		/// <summary>
		/// Represents a session block from a CDI file
		/// </summary>
		public class CDISession
		{
			/// <summary>
			/// Number of tracks in session (1..99) (or 0 = no more sessions)
			/// </summary>
			public byte NumTracks;
		}
		
		/// <summary>
		/// Represents a track/disc info block header from a CDI track
		/// </summary>
		public class CDITrackHeader
		{
			/// <summary>
			/// Number of tracks on disc (1..99)
			/// </summary>
			public byte NumTracks;

			/// <summary>
			/// Full Path/Filename (may be empty)
			/// </summary>
			public string Path;

			/// <summary>
			/// 0x0098 = CD-ROM, 0x0038 = DVD-ROM
			/// </summary>
			public ushort MediumType;
		}

		/// <summary>
		/// Represents a CD text block from a CDI track
		/// </summary>
		public class CDICDText
		{
			/// <summary>
			/// A CD text block has 0-18 strings, each of variable length
			/// </summary>
			public readonly IList<string> CdTexts = new List<string>();
		}

		/// <summary>
		/// Represents a track block from a CDI file
		/// </summary>
		public class CDITrack : CDITrackHeader
		{
			/// <summary>
			/// The sector count of each index specified for the track
			/// </summary>
			public readonly IList<uint> IndexSectorCounts = new List<uint>();

			/// <summary>
			/// CD text blocks
			/// </summary>
			public readonly IList<CDICDText> CdTextBlocks = new List<CDICDText>();

			/// <summary>
			/// The specified track mode (0 = Audio, 1 = Mode1, 2 = Mode2/Mixed)
			/// </summary>
			public byte TrackMode;

			/// <summary>
			/// Session number (0-indexed)
			/// </summary>
			public uint SessionNumber;

			/// <summary>
			/// Track number (0-indexed, releative to session)
			/// </summary>
			public uint TrackNumber;

			/// <summary>
			/// Track start address
			/// </summary>
			public uint TrackStartAddress;

			/// <summary>
			/// Track length, in sectors
			/// </summary>
			public uint TrackLength;

			/// <summary>
			/// The specified read mode (0 = Mode1, 1 = Mode2, 2 = Audio, 3 = Raw+Q, 4 = Raw+PQRSTUVW)
			/// </summary>
			public uint ReadMode;

			/// <summary>
			/// Upper 4 bits of ADR/Control
			/// </summary>
			public uint Control;

			/// <summary>
			/// 12-letter/digit string (may be empty)
			/// </summary>
			public string IsrcCode;

			/// <summary>
			/// Any non-zero is valid?
			/// </summary>
			public uint IsrcValidFlag;

			/// <summary>
			/// Only present on last track of a session (0 = Audio/CD-DA, 1 = Mode1/CD-ROM, 2 = Mode2/CD-XA)
			/// </summary>
			public uint SessionType;
		}
		
		/// <summary>
		/// Represents a disc info block from a CDI file
		/// </summary>
		public class CDIDiscInfo : CDITrackHeader
		{
			/// <summary>
			/// Total number of sectors
			/// </summary>
			public uint DiscSize;

			/// <summary>
			/// probably junk for non-ISO data discs
			/// </summary>
			public string VolumeId;

			/// <summary>
			/// 13-digit string (may be empty)
			/// </summary>
			public string Ean13Code;

			/// <summary>
			/// Any non-zero is valid?
			/// </summary>
			public uint Ean13CodeValid;

			/// <summary>
			/// CD text (for lead-in?)
			/// </summary>
			public string CdText;
		}

		public class CDIParseException : Exception
		{
			public CDIParseException(string message) : base(message) { }
		}

		/// <exception cref="CDIParseException">malformed cdi format</exception>
		public static CDIFile ParseFrom(Stream stream)
		{
			var cdif = new CDIFile();
			using var br = new BinaryReader(stream);

			try
			{
				stream.Seek(-4, SeekOrigin.End);
				cdif.Entrypoint = br.ReadUInt32();

				stream.Seek(-cdif.Entrypoint, SeekOrigin.End);

				cdif.NumSessions = br.ReadByte();
				if (cdif.NumSessions == 0)
				{
					throw new CDIParseException("Malformed CDI format: 0 sessions!");
				}

				void ParseTrackHeader(CDITrackHeader header)
				{
					stream.Seek(15, SeekOrigin.Current); // unknown bytes
					header.NumTracks = br.ReadByte();
					var pathLen = br.ReadByte();
					header.Path = br.ReadStringFixedUtf8(pathLen);
					stream.Seek(29, SeekOrigin.Current); // unknown bytes
					header.MediumType = br.ReadUInt16();
					switch (header.MediumType)
					{
						case 0x0038:
							throw new CDIParseException("Malformed CDI format: DVD was specified, but this is not supported!");
						case 0x0098:
							return;
						default:
							throw new CDIParseException("Malformed CDI format: Invalid medium type!");
					}
				}

				for (var i = 0; i <= cdif.NumSessions; i++)
				{
					var session = new CDISession();
					stream.Seek(1, SeekOrigin.Current); // unknown byte
					session.NumTracks = br.ReadByte();
					stream.Seek(13, SeekOrigin.Current); // unknown bytes
					cdif.Sessions.Add(session);

					// the last session block should have 0 tracks (as it indicates no more sessions)
					if (session.NumTracks == 0 && i != cdif.NumSessions)
					{
						throw new CDIParseException("Malformed CDI format: No tracks in session!");
					}

					if (session.NumTracks + cdif.Tracks.Count > 99)
					{
						throw new CDIParseException("Malformed CDI format: More than 99 tracks on disc!");
					}

					for (var j = 0; j < session.NumTracks; j++)
					{
						var track = new CDITrack();
						ParseTrackHeader(track);

						var indexes = br.ReadUInt16();
						if (indexes < 2) // We should have at least 2 indexes (one pre-gap, and one "real" one)
						{
							throw new CDIParseException("Malformed CDI format: Less than 2 indexes in track!");
						}
						for (var k = 0; k < indexes; k++)
						{
							track.IndexSectorCounts.Add(br.ReadUInt32());
						}

						var numCdTextBlocks = br.ReadUInt32();
						for (var k = 0; k < numCdTextBlocks; k++)
						{
							var cdTextBlock = new CDICDText();
							for (var l = 0; l < 18; l++)
							{
								var cdTextLen = br.ReadByte();
								if (cdTextLen > 0)
								{
									cdTextBlock.CdTexts.Add(br.ReadStringFixedUtf8(cdTextLen));
								}
							}
							track.CdTextBlocks.Add(cdTextBlock);
						}

						stream.Seek(2, SeekOrigin.Current); // unknown bytes
						track.TrackMode = br.ReadByte();
						if (track.TrackMode > 2)
						{
							throw new CDIParseException("Malformed CDI format: Invalid track mode!");
						}

						stream.Seek(7, SeekOrigin.Current); // unknown bytes
						track.SessionNumber = br.ReadUInt32();
						if (track.SessionNumber != i)
						{
							throw new CDIParseException("Malformed CDI format: Session number mismatch!");
						}

						track.TrackNumber = br.ReadUInt32();
						if (track.TrackNumber != j) // I think this is relative to the session?
						{
							throw new CDIParseException("Malformed CDI format: Track number mismatch!");
						}

						track.TrackStartAddress = br.ReadUInt32();
						track.TrackLength = br.ReadUInt32();
						stream.Seek(16, SeekOrigin.Current); // unknown bytes

						track.ReadMode = br.ReadUInt32();
						if (track.ReadMode > 4)
						{
							throw new CDIParseException("Malformed CDI format: Invalid read mode!");
						}

						track.Control = br.ReadUInt32();
						if ((track.Control & ~0xF) != 0)
						{
							throw new CDIParseException("Malformed CDI format: Invalid control!");
						}

						stream.Seek(1, SeekOrigin.Current); // unknown byte
						var redundantTrackLen = br.ReadUInt32();
						if (track.TrackLength != redundantTrackLen)
						{
							throw new CDIParseException("Malformed CDI format: Track length mismatch!");
						}

						stream.Seek(4, SeekOrigin.Current); // unknown bytes
						track.IsrcCode = br.ReadStringFixedUtf8(12);
						track.IsrcValidFlag = br.ReadUInt32();
						if (track.IsrcValidFlag == 0)
						{
							track.IsrcCode = string.Empty;
						}

						stream.Seek(87, SeekOrigin.Current); // unknown bytes
						track.SessionType = br.ReadByte();
						switch (track.SessionType)
						{
							case > 2:
								throw new CDIParseException("Malformed CDI format: Invalid session type!");
							case > 0 when j != session.NumTracks - 1:
								throw new CDIParseException("Malformed CDI format: Session type was specified, but this is only supposed to be present on the last track!");
						}

						stream.Seek(5, SeekOrigin.Current); // unknown bytes
						var notLastTrackInSession = br.ReadByte();
						switch (notLastTrackInSession)
						{
							case 0 when j != session.NumTracks - 1:
								throw new CDIParseException("Malformed CDI format: Track was specified to be the last track of the session, but more tracks are available!");
							case > 1:
								throw new CDIParseException("Malformed CDI format: Invalid not last track of session flag!");
						}

						stream.Seek(5, SeekOrigin.Current); // unknown bytes
						// well, the last 4 bytes here are said to be the "address for last track of a session? (otherwise 00,00,FF,FF)"
						// except I'm not sure what the address	is meant to be, by bytes? by sectors? relative to file? relative session start?
						// for now I just ignore it

						cdif.Tracks.Add(track);
					}
				}

				ParseTrackHeader(cdif.DiscInfo);
				cdif.DiscInfo.DiscSize = br.ReadUInt32();
				if (cdif.DiscInfo.DiscSize != cdif.Tracks.Sum(t => t.TrackLength))
				{
					//throw new CDIParseException("Malformed CDI format: Disc size mismatch!");
					//this seems to be wrong? seen: DiscSize == 272440, TrackLength sum == 261190 (02:30:00 difference?)
					//EDIT: This must be the leadout and leadin tracks being included! first leadout is 6750, leadins are 4500, 6750+4500=11250
					//Seems the blob doesn't include leadin and leadout directly anyways
				}

				var volumeIdLen = br.ReadByte();
				cdif.DiscInfo.VolumeId = br.ReadStringFixedUtf8(volumeIdLen);
				stream.Seek(9, SeekOrigin.Current); // unknown bytes

				cdif.DiscInfo.Ean13Code = br.ReadStringFixedUtf8(13);
				cdif.DiscInfo.Ean13CodeValid = br.ReadUInt32();
				if (cdif.DiscInfo.Ean13CodeValid == 0)
				{
					cdif.DiscInfo.Ean13Code = string.Empty;
				}

				var cdTextLengh = br.ReadUInt32();
				if (cdTextLengh > int.MaxValue)
				{
					// suppose technically this might not be considered too large purely going off the format
					// but it's a bit silly to have a >2GB string so this is probably not valid
					throw new CDIParseException("Malformed CDI format: CD text too large!");
				}

				cdif.DiscInfo.CdText = br.ReadStringFixedUtf8((int)cdTextLengh);
				stream.Seek(12, SeekOrigin.Current); // unknown bytes

				if (cdif.Tracks.Exists(track => track.NumTracks != cdif.Tracks.Count) || cdif.DiscInfo.NumTracks != cdif.Tracks.Count)
				{
					throw new CDIParseException("Malformed CDI format: Total track number mismatch!");
				}
				
				if (stream.Position != stream.Length - 4)
				{
					//throw new CDIParseException("Malformed CDI format: Did not reach end of footer after parsing!");
					//not sure about this
				}

				return cdif;
			}
			catch (EndOfStreamException)
			{
				throw new CDIParseException("Malformed CDI format: Unexpected stream end!");
			}
		}

		public class LoadResults
		{
			public CDIFile ParsedCDIFile;
			public bool Valid;
			public CDIParseException FailureException;
			public string CdiPath;
		}

		public static LoadResults LoadCDIPath(string path)
		{
			var ret = new LoadResults
			{
				CdiPath = path
			};
			try
			{
				if (!File.Exists(path)) throw new CDIParseException("Malformed CDI format: nonexistent CDI file!");

				CDIFile cdif;
				using (var infCDI = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
					cdif = ParseFrom(infCDI);

				ret.ParsedCDIFile = cdif;
				ret.Valid = true;
			}
			catch (CDIParseException ex)
			{
				ret.FailureException = ex;
			}

			return ret;
		}

		/// <exception cref="CDIParseException">file <paramref name="cdiPath"/> not found</exception>
		public static Disc LoadCDIToDisc(string cdiPath, DiscMountPolicy IN_DiscMountPolicy)
		{
			var loadResults = LoadCDIPath(cdiPath);
			if (!loadResults.Valid)
				throw loadResults.FailureException;

			var disc = new Disc();
			var cdif = loadResults.ParsedCDIFile;

			IBlob cdiBlob = new Blob_RawFile { PhysicalPath = cdiPath };
			disc.DisposableResources.Add(cdiBlob);

			var trackOffset = 0;
			var blobOffset = 0;
			for (var i = 0; i < cdif.NumSessions; i++)
			{
				var session = new DiscSession { Number = i + 1 };

				// leadin track
				// we create this only for session 2+, not session 1
				var leadinSize = i == 0 ? 0 : 4500;
				for (var j = 0; j < leadinSize; j++)
				{
					// this is most certainly wrong
					// nothing relies on the exact contents for now (only multisession core is VirtualJaguar which doesn't touch leadin)
					// but this will allow the correct TOC to be generated
					var cueTrackType = CueTrackType.Audio;
					if ((cdif.Tracks[trackOffset].Control & 4) != 0)
					{
						cueTrackType = cdif.Tracks[trackOffset + cdif.Sessions[i].NumTracks - 1].SessionType switch
						{
							0 => CueTrackType.Mode1_2352,
							1 => CueTrackType.CDI_2352,
							2 => CueTrackType.Mode2_2352,
							_ => cueTrackType
						};
					}
					disc._Sectors.Add(new SS_Gap
					{
						Policy = IN_DiscMountPolicy,
						TrackType = cueTrackType,
					});
				}

				for (var j = 0; j < cdif.Sessions[i].NumTracks; j++)
				{
					var track = cdif.Tracks[trackOffset + j];

					RawTOCEntry EmitRawTOCEntry()
					{
						var q = default(SubchannelQ);
						//absent some kind of policy for how to set it, this is a safe assumption
						const byte kADR = 1;
						q.SetStatus(kADR, (EControlQ)track.Control);
						q.q_tno = BCD2.FromDecimal(0);
						q.q_index = BCD2.FromDecimal(trackOffset + j + 1);
						q.Timestamp = 0;
						q.zero = 0;
						q.AP_Timestamp = disc._Sectors.Count;
						q.q_crc = 0;
						return new() { QData = q };
					}

					var sectorSize = track.ReadMode switch
					{
						0 => 2048,
						1 => 2336,
						2 => 2352,
						3 => 2368,
						4 => 2448,
						_ => throw new InvalidOperationException()
					};
					var curIndex = 0;
					var relMSF = -track.IndexSectorCounts[0];
					var indexSectorOffset = 0U;
					for (var k = 0; k < track.TrackLength; k++)
					{
						if (track.IndexSectorCounts[curIndex] == k - indexSectorOffset)
						{
							indexSectorOffset += track.IndexSectorCounts[curIndex];
							curIndex++;
							if (track.IndexSectorCounts.Count == curIndex)
							{
								throw new CDIParseException("Malformed CDI Format: Reached end of index list unexpectedly");
							}
							if (curIndex == 1)
							{
								session.RawTOCEntries.Add(EmitRawTOCEntry());
							}
						}

						//note that CDIs contain the pregap data themselves...
						SS_Base synth = track.ReadMode switch
						{
							0 => new SS_Mode1_2048(),
							1 => new SS_Mode2_2336(),
							2 => new SS_2352(),
							3 => new SS_2364_DeinterleavedQ(),
							4 => new SS_2448_Interleaved(),
							_ => throw new InvalidOperationException()
						};
						synth.Blob = cdiBlob;
						synth.BlobOffset = blobOffset;
						synth.Policy = IN_DiscMountPolicy;
						const byte kADR = 1;
						synth.sq.SetStatus(kADR, (EControlQ)track.Control);
						synth.sq.q_tno = BCD2.FromDecimal(trackOffset + j + 1);
						synth.sq.q_index = BCD2.FromDecimal(curIndex);
						synth.sq.Timestamp = !IN_DiscMountPolicy.CUE_PregapContradictionModeA && curIndex == 0
							? (int)relMSF + 1
							: (int)relMSF;
						synth.sq.zero = 0;
						synth.sq.AP_Timestamp = disc._Sectors.Count;
						synth.sq.q_crc = 0;
						synth.Pause = curIndex == 0;
						disc._Sectors.Add(synth);
						blobOffset += sectorSize;
						relMSF++;
					}
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

				var TOCMiscInfo = new Synthesize_A0A1A2_Job(
					firstRecordedTrackNumber: trackOffset + 1,
					lastRecordedTrackNumber: trackOffset + cdif.Sessions[i].NumTracks,
					sessionFormat: (SessionFormat)(cdif.Tracks[trackOffset + cdif.Sessions[i].NumTracks - 1].SessionType * 0x10),
					leadoutTimestamp: disc._Sectors.Count);
				TOCMiscInfo.Run(session.RawTOCEntries);

				disc.Sessions.Add(session);
				trackOffset += cdif.Sessions[i].NumTracks;
			}

			return disc;
		}
	}
}