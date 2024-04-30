using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.DiscSystem.CUE;

#pragma warning disable BHI1005

//MAME CHD images, using the standard libchdr for reading

namespace BizHawk.Emulation.DiscSystem
{
	public static class CHD_Format
	{
		/// <summary>
		/// Represents a CHD file.
		/// This isn't particularly faithful to the format, but rather it just wraps libchdr's chd_file
		/// </summary>
		public class CHDFile
		{
			/// <summary>
			/// Wrapper of a C# stream to a chd_core_file
			/// </summary>
			public LibChdr.CoreFileStreamWrapper CoreFile;

			/// <summary>
			/// chd_file* to be used for libchdr functions
			/// </summary>
			public IntPtr ChdFile;

			/// <summary>
			/// CHD header, interpreted by libchdr
			/// </summary>
			public LibChdr.chd_header Header;

			/// <summary>
			/// CHD CD metadata for each track
			/// </summary>
			public readonly IList<CHDCdMetadata> CdMetadatas = new List<CHDCdMetadata>();
		}

		/// <summary>
		/// Results of chd_get_metadata with cdrom track metadata tags
		/// </summary>
		public class CHDCdMetadata
		{
			/// <summary>
			/// Track number (1..99)
			/// </summary>
			public uint Track;

			/// <summary>
			/// Indicates this is a CDI format
			/// chd_track_type doesn't have an explicit enum for this
			/// However, this is still important info for discerning the session format
			/// </summary>
			public bool IsCDI;

			/// <summary>
			/// Track type
			/// </summary>
			public LibChdr.chd_track_type TrackType;

			/// <summary>
			/// Subcode type
			/// </summary>
			public LibChdr.chd_sub_type SubType;

			/// <summary>
			/// Size of each sector
			/// </summary>
			public uint SectorSize;

			/// <summary>
			/// Subchannel size
			/// </summary>
			public uint SubSize;

			/// <summary>
			/// Number of frames in this track
			/// This might include pregap, if that is stored in the chd
			/// </summary>
			public uint Frames;

			/// <summary>
			/// Number of "padding" frames in this track
			/// This is done in order to maintain a multiple of 4 frames for each track
			/// These padding frames aren't representative of the actual disc anyways
			/// They're only useful to know the offset of the next track within the chd
			/// </summary>
			public uint Padding;

			/// <summary>
			/// Number of pregap sectors
			/// </summary>
			public uint Pregap;

			/// <summary>
			/// Pregap track type
			/// </summary>
			public LibChdr.chd_track_type PregapTrackType;

			/// <summary>
			/// Pregap subcode type
			/// </summary>
			public LibChdr.chd_sub_type PregapSubType;

			/// <summary>
			/// Indicates whether pregap is in the CHD
			/// If pregap isn't in the CHD, it needs to be generated where appropriate
			/// </summary>
			public bool PregapInChd;

			/// <summary>
			/// Number of postgap sectors
			/// </summary>
			public uint PostGap;
		}

		public class CHDParseException : Exception
		{
			public CHDParseException(string message) : base(message) { }
			public CHDParseException(string message, Exception ex) : base(message, ex) { }
		}

		private static LibChdr.chd_track_type GetTrackType(string type)
		{
			return type switch
			{
				"MODE1" => LibChdr.chd_track_type.CD_TRACK_MODE1,
				"MODE1/2048" => LibChdr.chd_track_type.CD_TRACK_MODE1,
				"MODE1_RAW" => LibChdr.chd_track_type.CD_TRACK_MODE1_RAW,
				"MODE1/2352" => LibChdr.chd_track_type.CD_TRACK_MODE1_RAW,
				"MODE2" => LibChdr.chd_track_type.CD_TRACK_MODE2,
				"MODE2/2336" => LibChdr.chd_track_type.CD_TRACK_MODE2,
				"MODE2_FORM1" => LibChdr.chd_track_type.CD_TRACK_MODE2_FORM1,
				"MODE2/2048" => LibChdr.chd_track_type.CD_TRACK_MODE2_FORM1,
				"MODE2_FORM2" => LibChdr.chd_track_type.CD_TRACK_MODE2_FORM2,
				"MODE2/2324" => LibChdr.chd_track_type.CD_TRACK_MODE2_FORM2,
				"MODE2_FORM_MIX" => LibChdr.chd_track_type.CD_TRACK_MODE2_FORM_MIX,
				"MODE2_RAW" => LibChdr.chd_track_type.CD_TRACK_MODE2_RAW,
				"MODE2/2352" => LibChdr.chd_track_type.CD_TRACK_MODE2_RAW,
				"CDI/2352" => LibChdr.chd_track_type.CD_TRACK_MODE2_RAW,
				"AUDIO" => LibChdr.chd_track_type.CD_TRACK_AUDIO,
				_ => throw new CHDParseException("Malformed CHD format: Invalid track type!"),
			};
		}

		private static (LibChdr.chd_track_type TrackType, bool ChdContainsPregap) GetTrackTypeForPregap(string type)
		{
			if (type.Length > 0 && type[0] == 'V')
			{
				return (GetTrackType(type[1..]), true);
			}

			return (GetTrackType(type), false);
		}

		private static uint GetSectorSize(LibChdr.chd_track_type type)
		{
			return type switch
			{
				LibChdr.chd_track_type.CD_TRACK_MODE1 => 2048,
				LibChdr.chd_track_type.CD_TRACK_MODE1_RAW => 2352,
				LibChdr.chd_track_type.CD_TRACK_MODE2 => 2336,
				LibChdr.chd_track_type.CD_TRACK_MODE2_FORM1 => 2048,
				LibChdr.chd_track_type.CD_TRACK_MODE2_FORM2 => 2324,
				LibChdr.chd_track_type.CD_TRACK_MODE2_FORM_MIX => 2336,
				LibChdr.chd_track_type.CD_TRACK_MODE2_RAW => 2352,
				LibChdr.chd_track_type.CD_TRACK_AUDIO => 2352,
				_ => throw new CHDParseException("Malformed CHD format: Invalid track type!"),
			};
		}

		private static LibChdr.chd_sub_type GetSubType(string type)
		{
			return type switch
			{
				"RW" => LibChdr.chd_sub_type.CD_SUB_NORMAL,
				"RW_RAW" => LibChdr.chd_sub_type.CD_SUB_RAW,
				"NONE" => LibChdr.chd_sub_type.CD_SUB_NONE,
				_ => throw new CHDParseException("Malformed CHD format: Invalid sub type!"),
			};
		}

		private static uint GetSubSize(LibChdr.chd_sub_type type)
		{
			return type switch
			{
				LibChdr.chd_sub_type.CD_SUB_NORMAL => 96,
				LibChdr.chd_sub_type.CD_SUB_RAW => 96,
				LibChdr.chd_sub_type.CD_SUB_NONE => 0,
				_ => throw new CHDParseException("Malformed CHD format: Invalid sub type!"),
			};
		}

		private static readonly string[] _metadataTags = { "TRACK", "TYPE", "SUBTYPE", "FRAMES", "PREGAP", "PGTYPE", "PGSUB", "POSTGAP" };

		private static CHDCdMetadata ParseMetadata2(string metadata)
		{
			var strs = metadata.Split(' ');
			if (strs.Length != 8)
			{
				throw new CHDParseException("Malformed CHD format: Incorrect number of metadata tags");
			}

			for (var i = 0; i < 8; i++)
			{
				var spl = strs[i].Split(':');
				if (spl.Length != 2 || _metadataTags[i] != spl[0])
				{
					throw new CHDParseException("Malformed CHD format: Invalid metadata tag");
				}

				strs[i] = spl[1];
			}

			var ret = new CHDCdMetadata();
			try
			{
				ret.Track = uint.Parse(strs[0]);
				ret.TrackType = GetTrackType(strs[1]);
				ret.SubType = GetSubType(strs[2]);
				ret.Frames = uint.Parse(strs[3]);
				ret.Pregap = uint.Parse(strs[4]);
				(ret.PregapTrackType, ret.PregapInChd) = GetTrackTypeForPregap(strs[5]);
				ret.PregapSubType = GetSubType(strs[6]);
				ret.PostGap = uint.Parse(strs[7]);
			}
			catch (Exception ex)
			{
				throw ex as CHDParseException ?? new("Malformed CHD format: Metadata parsing threw an exception", ex);
			}

			if (ret.PregapInChd && ret.Pregap == 0)
			{
				throw new CHDParseException("Malformed CHD format: CHD track type indicate it contained pregap data, but no pregap data is present");
			}

			ret.IsCDI = strs[1] == "CDI/2352";
			ret.SectorSize = GetSectorSize(ret.TrackType);
			ret.SubSize = GetSubSize(ret.SubType);
			ret.Padding = (0 - ret.Frames) & 3;
			return ret;
		}

		private static CHDCdMetadata ParseMetadata(string metadata)
		{
			var strs = metadata.Split(' ');
			if (strs.Length != 4)
			{
				throw new CHDParseException("Malformed CHD format: Incorrect number of metadata tags");
			}

			for (var i = 0; i < 4; i++)
			{
				var spl = strs[i].Split(':');
				if (spl.Length != 2 || _metadataTags[i] != spl[0])
				{
					throw new CHDParseException("Malformed CHD format: Invalid metadata tag");
				}

				strs[i] = spl[1];
			}

			var ret = new CHDCdMetadata();
			try
			{
				ret.Track = uint.Parse(strs[0]);
				ret.TrackType = GetTrackType(strs[1]);
				ret.SubType = GetSubType(strs[2]);
				ret.Frames = uint.Parse(strs[3]);
			}
			catch (Exception ex)
			{
				throw ex as CHDParseException ?? new("Malformed CHD format: Metadata parsing threw an exception", ex);
			}

			ret.IsCDI = strs[1] == "CDI/2352";
			ret.SectorSize = GetSectorSize(ret.TrackType);
			ret.SubSize = GetSubSize(ret.SubType);
			ret.Padding = (0 - ret.Frames) & 3;
			return ret;
		}

		private static void ParseMetadataOld(ICollection<CHDCdMetadata> cdMetadatas, Span<byte> metadata)
		{
			var numTracks = BinaryPrimitives.ReadUInt32LittleEndian(metadata);
			var bigEndian = numTracks > 99; // apparently old metadata can appear as either little endian or big endian
			if (bigEndian)
			{
				numTracks = BinaryPrimitives.ReverseEndianness(numTracks);
			}

			if (numTracks > 99)
			{
				throw new CHDParseException("Malformed CHD format: Invalid number of tracks");
			}

			for (var i = 0; i < numTracks; i++)
			{
				var track = metadata[(4 + i * 24)..];
				var cdMetadata = new CHDCdMetadata
				{
					Track = (uint)i + 1
				};
				if (bigEndian)
				{
					cdMetadata.TrackType = (LibChdr.chd_track_type)BinaryPrimitives.ReadUInt32BigEndian(track);
					cdMetadata.SubType = (LibChdr.chd_sub_type)BinaryPrimitives.ReadUInt32BigEndian(track[..4]);
					cdMetadata.SectorSize = BinaryPrimitives.ReadUInt32BigEndian(track[..8]);
					cdMetadata.SubSize = BinaryPrimitives.ReadUInt32BigEndian(track[..12]);
					cdMetadata.Frames = BinaryPrimitives.ReadUInt32BigEndian(track[..16]);
					cdMetadata.Padding = BinaryPrimitives.ReadUInt32BigEndian(track[..20]);
				}
				else
				{
					cdMetadata.TrackType = (LibChdr.chd_track_type)BinaryPrimitives.ReadUInt32LittleEndian(track);
					cdMetadata.SubType = (LibChdr.chd_sub_type)BinaryPrimitives.ReadUInt32LittleEndian(track[..4]);
					cdMetadata.SectorSize = BinaryPrimitives.ReadUInt32LittleEndian(track[..8]);
					cdMetadata.SubSize = BinaryPrimitives.ReadUInt32LittleEndian(track[..12]);
					cdMetadata.Frames = BinaryPrimitives.ReadUInt32LittleEndian(track[..16]);
					cdMetadata.Padding = BinaryPrimitives.ReadUInt32LittleEndian(track[..20]);
				}

				if (cdMetadata.SectorSize != GetSectorSize(cdMetadata.TrackType))
				{
					throw new CHDParseException("Malformed CHD format: Invalid sector size");
				}

				if (cdMetadata.SubSize != GetSubSize(cdMetadata.SubType))
				{
					throw new CHDParseException("Malformed CHD format: Invalid sub size");
				}

				var expectedPadding = (0 - cdMetadata.Frames) & 3;
				if (cdMetadata.Padding != expectedPadding)
				{
					throw new CHDParseException("Malformed CHD format: Invalid padding value");
				}

				cdMetadatas.Add(cdMetadata);
			}
		}

		/// <exception cref="CHDParseException">malformed chd format</exception>
		public static CHDFile ParseFrom(Stream stream)
		{
			var chdf = new CHDFile();
			try
			{
				chdf.CoreFile = new(stream);
				var err = LibChdr.chd_open_core_file(chdf.CoreFile.CoreFile, LibChdr.CHD_OPEN_READ, IntPtr.Zero, out chdf.ChdFile);
				if (err != LibChdr.chd_error.CHDERR_NONE)
				{
					throw new CHDParseException($"Malformed CHD format: Failed to open chd, got error {err}");
				}

				unsafe
				{
					var header = (LibChdr.chd_header*)LibChdr.chd_get_header(chdf.ChdFile);
					chdf.Header = *header;
				}

				if (chdf.Header.hunkbytes == 0 || chdf.Header.hunkbytes % LibChdr.CD_FRAME_SIZE != 0)
				{
					throw new CHDParseException("Malformed CHD format: Invalid hunk size");
				}

				// libchdr puts the correct value here for older versions of chds which don't have this
				// for newer chds, it is left as is, which might be invalid
				if (chdf.Header.unitbytes != LibChdr.CD_FRAME_SIZE)
				{
					throw new CHDParseException("Malformed CHD format: Invalid unit size");
				}

				var metadataOutput = new byte[256];
				for (uint i = 0; i < 99; i++)
				{
					err = LibChdr.chd_get_metadata(chdf.ChdFile, LibChdr.CDROM_TRACK_METADATA2_TAG,
						i, metadataOutput, (uint)metadataOutput.Length, out var resultLen, out _, out _);
					if (err == LibChdr.chd_error.CHDERR_NONE)
					{
						var metadata = Encoding.ASCII.GetString(metadataOutput, 0,  (int)resultLen);
						chdf.CdMetadatas.Add(ParseMetadata2(metadata));
						continue;
					}

					err = LibChdr.chd_get_metadata(chdf.ChdFile, LibChdr.CDROM_TRACK_METADATA_TAG,
						i, metadataOutput, (uint)metadataOutput.Length, out resultLen, out _, out _);
					if (err == LibChdr.chd_error.CHDERR_NONE)
					{
						var metadata = Encoding.ASCII.GetString(metadataOutput, 0,  (int)resultLen);
						chdf.CdMetadatas.Add(ParseMetadata(metadata));
						continue;
					}

					// if no more metadata, we're out of tracks
					break;
				}

				// validate track numbers
				if (chdf.CdMetadatas.Where((t, i) => t.Track != i + 1).Any())
				{
					throw new CHDParseException("Malformed CHD format: Invalid track number");
				}

				if (chdf.CdMetadatas.Count == 0)
				{
					// if no metadata was present, we might have "old" metadata instead (which has all track info stored in one entry)
					metadataOutput = new byte[4 + 24 * 99];
					err = LibChdr.chd_get_metadata(chdf.ChdFile, LibChdr.CDROM_OLD_METADATA_TAG,
						0, metadataOutput, (uint)metadataOutput.Length, out var resultLen, out _, out _);
					if (err == LibChdr.chd_error.CHDERR_NONE)
					{
						if (resultLen != metadataOutput.Length)
						{
							throw new CHDParseException("Malformed CHD format: Incorrect length for old metadata");
						}

						ParseMetadataOld(chdf.CdMetadatas, metadataOutput);
					}
				}

				if (chdf.CdMetadatas.Count == 0)
				{
					throw new CHDParseException("Malformed CHD format: No tracks present in chd");
				}

				// validation checks
				var chdExpectedNumSectors = 0L;
				foreach (var cdMetadata in chdf.CdMetadatas)
				{
					// if pregap is in the chd, then the reported frame count includes both pregap and track data
					if (cdMetadata.PregapInChd && cdMetadata.Pregap > cdMetadata.Frames)
					{
						throw new CHDParseException("Malformed CHD format: Pregap in chd is larger than total sectors in chd track");
					}

					chdExpectedNumSectors += cdMetadata.Frames + cdMetadata.Padding;
				}

				var chdActualNumSectors = chdf.Header.hunkcount * (chdf.Header.hunkbytes / LibChdr.CD_FRAME_SIZE);
				//if (chdExpectedNumSectors != chdActualNumSectors) // i see some chds with 4 extra sectors of padding in the end?
				if (chdExpectedNumSectors > chdActualNumSectors)
				{
					throw new CHDParseException("Malformed CHD format: Mismatch in expected and actual number of sectors present");
				}

				return chdf;
			}
			catch (Exception ex)
			{
				if (chdf.ChdFile != IntPtr.Zero)
				{
					LibChdr.chd_close(chdf.ChdFile);
				}

				if (chdf.CoreFile == null)
				{
					stream.Dispose();
				}

				chdf.CoreFile?.Dispose();
				throw ex as CHDParseException ?? new("Malformed CHD format: An unknown exception was thrown while parsing", ex);
			}
		}

		public class LoadResults
		{
			public CHDFile ParsedCHDFile;
			public bool Valid;
			public CHDParseException FailureException;
			public string ChdPath;
		}

		public static LoadResults LoadCHDPath(string path)
		{
			var ret = new LoadResults
			{
				ChdPath = path
			};
			try
			{
				if (!File.Exists(path)) throw new CHDParseException("Malformed CHD format: Nonexistent CHD file!");

				var infCHD = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				ret.ParsedCHDFile = ParseFrom(infCHD);
				ret.Valid = true;
			}
			catch (CHDParseException ex)
			{
				ret.FailureException = ex;
			}

			return ret;
		}

		/// <summary>
		/// CHD is dumb and byteswaps audio samples for some reason
		/// </summary>
		private class SS_CHD_Audio : SS_Base
		{
			public override void Synth(SectorSynthJob job)
			{
				// read the sector user data
				if ((job.Parts & ESectorSynthPart.User2352) != 0)
				{
					Blob.Read(BlobOffset, job.DestBuffer2448, job.DestOffset, 2352);
					EndiannessUtils.MutatingByteSwap16(job.DestBuffer2448.AsSpan().Slice(job.DestOffset, 2352));
				}

				// if subcode is needed, synthesize it
				SynthSubchannelAsNeed(job);
			}
		}

		private class SS_CHD_Sub : ISectorSynthJob2448
		{
			private readonly SS_Base _baseSynth;
			private readonly bool _isInterleaved;

			public SS_CHD_Sub(SS_Base baseSynth, bool isInterleaved)
			{
				_baseSynth = baseSynth;
				_isInterleaved = isInterleaved;
			}

			public void Synth(SectorSynthJob job)
			{
				if ((job.Parts & ESectorSynthPart.SubcodeAny) != 0)
				{
					_baseSynth.Blob.Read(_baseSynth.BlobOffset + 2352, job.DestBuffer2448, job.DestOffset + 2352, 96);

					if ((job.Parts & ESectorSynthPart.SubcodeDeinterleave) != 0 && _isInterleaved)
					{
						SynthUtils.DeinterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
					}

					if ((job.Parts & ESectorSynthPart.SubcodeDeinterleave) == 0 && !_isInterleaved)
					{
						SynthUtils.InterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
					}

					job.Parts &= ~ESectorSynthPart.SubcodeAny;
				}

				_baseSynth.Synth(job);
			}
		}

		/// <exception cref="CHDParseException">file <paramref name="chdPath"/> not found</exception>
		public static Disc LoadCHDToDisc(string chdPath, DiscMountPolicy IN_DiscMountPolicy)
		{
			var loadResults = LoadCHDPath(chdPath);
			if (!loadResults.Valid)
			{
				throw loadResults.FailureException;
			}

			var disc = new Disc();
			try
			{
				var chdf = loadResults.ParsedCHDFile;
				IBlob chdBlob = new Blob_CHD(chdf.CoreFile, chdf.ChdFile, chdf.Header.hunkbytes);
				disc.DisposableResources.Add(chdBlob);

				// chds only support 1 session
				var session = new DiscSession { Number = 1 };
				var chdOffset = 0L;
				foreach (var cdMetadata in chdf.CdMetadatas)
				{
					RawTOCEntry EmitRawTOCEntry()
					{
						var q = default(SubchannelQ);
						//absent some kind of policy for how to set it, this is a safe assumption
						const byte kADR = 1;
						var control = cdMetadata.TrackType != LibChdr.chd_track_type.CD_TRACK_AUDIO
							? EControlQ.DATA
							: EControlQ.None;
						q.SetStatus(kADR, control);
						q.q_tno = BCD2.FromDecimal(0);
						q.q_index = BCD2.FromDecimal((int)cdMetadata.Track);
						q.Timestamp = 0;
						q.zero = 0;
						q.AP_Timestamp = disc._Sectors.Count;
						q.q_crc = 0;
						return new() { QData = q };
					}

					static SS_Base CreateSynth(LibChdr.chd_track_type trackType)
					{
						return trackType switch
						{
							LibChdr.chd_track_type.CD_TRACK_MODE1 => new SS_Mode1_2048(),
							LibChdr.chd_track_type.CD_TRACK_MODE1_RAW => new SS_2352(),
							LibChdr.chd_track_type.CD_TRACK_MODE2 => new SS_Mode2_2336(),
							LibChdr.chd_track_type.CD_TRACK_MODE2_FORM1 => new SS_Mode2_Form1_2048(),
							LibChdr.chd_track_type.CD_TRACK_MODE2_FORM2 => new SS_Mode2_Form2_2324(),
							LibChdr.chd_track_type.CD_TRACK_MODE2_FORM_MIX => new SS_Mode2_2336(),
							LibChdr.chd_track_type.CD_TRACK_MODE2_RAW => new SS_2352(),
							LibChdr.chd_track_type.CD_TRACK_AUDIO => new SS_CHD_Audio(),
							_ => throw new InvalidOperationException(),
						};
					}

					static CueTrackType ToCueTrackType(LibChdr.chd_track_type chdTrackType, bool isCdi)
					{
						// rough matches, not too important if these are somewhat wrong (they're just used for generated gaps)
						return chdTrackType switch
						{
							LibChdr.chd_track_type.CD_TRACK_MODE1 => CueTrackType.Mode1_2048,
							LibChdr.chd_track_type.CD_TRACK_MODE1_RAW => CueTrackType.Mode1_2352,
							LibChdr.chd_track_type.CD_TRACK_MODE2 => CueTrackType.Mode2_2336,
							LibChdr.chd_track_type.CD_TRACK_MODE2_FORM1 => CueTrackType.Mode2_2336,
							LibChdr.chd_track_type.CD_TRACK_MODE2_FORM2 => CueTrackType.Mode2_2336,
							LibChdr.chd_track_type.CD_TRACK_MODE2_FORM_MIX => CueTrackType.Mode2_2336,
							LibChdr.chd_track_type.CD_TRACK_MODE2_RAW when isCdi => CueTrackType.CDI_2352,
							LibChdr.chd_track_type.CD_TRACK_MODE2_RAW => CueTrackType.Mode2_2352,
							LibChdr.chd_track_type.CD_TRACK_AUDIO => CueTrackType.Audio,
							_ => throw new InvalidOperationException(),
						};
					}

					var pregapLength = cdMetadata.Pregap;
					// force 150 sector pregap for the first track if not present in the chd
					if (!cdMetadata.PregapInChd && cdMetadata.Track == 1)
					{
						cdMetadata.PregapTrackType = cdMetadata.TrackType;
						cdMetadata.PregapSubType = cdMetadata.SubType;
						pregapLength = 150;
					}

					var relMSF = -pregapLength;
					for (var i = 0; i < pregapLength; i++)
					{
						SS_Base synth;
						if (cdMetadata.PregapInChd)
						{
							synth = CreateSynth(cdMetadata.PregapTrackType);
							synth.Blob = chdBlob;
							synth.BlobOffset = chdOffset;
						}
						else
						{
							synth = new SS_Gap { TrackType = ToCueTrackType(cdMetadata.PregapTrackType, cdMetadata.IsCDI) };
						}

						synth.Policy = IN_DiscMountPolicy;
						const byte kADR = 1;
						var control = cdMetadata.PregapTrackType != LibChdr.chd_track_type.CD_TRACK_AUDIO
							? EControlQ.DATA
							: EControlQ.None;
						synth.sq.SetStatus(kADR, control);
						synth.sq.q_tno = BCD2.FromDecimal((int)cdMetadata.Track);
						synth.sq.q_index = BCD2.FromDecimal(0);
						synth.sq.Timestamp = !IN_DiscMountPolicy.CUE_PregapContradictionModeA
							? (int)relMSF + 1
							: (int)relMSF;
						synth.sq.zero = 0;
						synth.sq.AP_Timestamp = disc._Sectors.Count;
						synth.sq.q_crc = 0;
						synth.Pause = true;

						if (cdMetadata.PregapInChd)
						{
							// wrap the base synth with our special synth if we have subcode in the chd
							ISectorSynthJob2448 chdSynth = cdMetadata.PregapSubType switch
							{
								LibChdr.chd_sub_type.CD_SUB_NORMAL => new SS_CHD_Sub(synth, isInterleaved: true),
								LibChdr.chd_sub_type.CD_SUB_RAW => new SS_CHD_Sub(synth, isInterleaved: false),
								LibChdr.chd_sub_type.CD_SUB_NONE => synth,
								_ => throw new InvalidOperationException(),
							};

							disc._Sectors.Add(chdSynth);
							chdOffset += LibChdr.CD_FRAME_SIZE;
						}
						else
						{
							disc._Sectors.Add(synth);
						}

						relMSF++;
					}

					session.RawTOCEntries.Add(EmitRawTOCEntry());

					var trackLength = cdMetadata.Frames;
					if (cdMetadata.PregapInChd)
					{
						trackLength -= pregapLength;
					}

					for (var i = 0; i < trackLength; i++)
					{
						var synth = CreateSynth(cdMetadata.TrackType);
						synth.Blob = chdBlob;
						synth.BlobOffset = chdOffset;
						synth.Policy = IN_DiscMountPolicy;
						const byte kADR = 1;
						var control = cdMetadata.TrackType != LibChdr.chd_track_type.CD_TRACK_AUDIO
							? EControlQ.DATA
							: EControlQ.None;
						synth.sq.SetStatus(kADR, control);
						synth.sq.q_tno = BCD2.FromDecimal((int)cdMetadata.Track);
						synth.sq.q_index = BCD2.FromDecimal(1);
						synth.sq.Timestamp = (int)relMSF;
						synth.sq.zero = 0;
						synth.sq.AP_Timestamp = disc._Sectors.Count;
						synth.sq.q_crc = 0;
						synth.Pause = false;
						ISectorSynthJob2448 chdSynth = cdMetadata.SubType switch
						{
							LibChdr.chd_sub_type.CD_SUB_NORMAL => new SS_CHD_Sub(synth, isInterleaved: true),
							LibChdr.chd_sub_type.CD_SUB_RAW => new SS_CHD_Sub(synth, isInterleaved: false),
							LibChdr.chd_sub_type.CD_SUB_NONE => synth,
							_ => throw new InvalidOperationException(),
						};
						disc._Sectors.Add(chdSynth);
						chdOffset += LibChdr.CD_FRAME_SIZE;
						relMSF++;
					}

					chdOffset += cdMetadata.Padding * LibChdr.CD_FRAME_SIZE;

					for (var i = 0; i < cdMetadata.PostGap; i++)
					{
						var synth = new SS_Gap
						{
							TrackType = ToCueTrackType(cdMetadata.TrackType, cdMetadata.IsCDI),
							Policy = IN_DiscMountPolicy
						};
						const byte kADR = 1;
						var control = cdMetadata.TrackType != LibChdr.chd_track_type.CD_TRACK_AUDIO
							? EControlQ.DATA
							: EControlQ.None;
						synth.sq.SetStatus(kADR, control);
						synth.sq.q_tno = BCD2.FromDecimal((int)cdMetadata.Track);
						synth.sq.q_index = BCD2.FromDecimal(2);
						synth.sq.Timestamp = (int)relMSF;
						synth.sq.zero = 0;
						synth.sq.AP_Timestamp = disc._Sectors.Count;
						synth.sq.q_crc = 0;
						synth.Pause = true;
						disc._Sectors.Add(synth);
						relMSF++;
					}
				}

				SessionFormat GuessSessionFormat()
				{
					foreach (var cdMetadata in chdf.CdMetadatas)
					{
						if (cdMetadata.IsCDI)
						{
							return SessionFormat.Type10_CDI;
						}

						if (cdMetadata.TrackType is LibChdr.chd_track_type.CD_TRACK_MODE2
							or LibChdr.chd_track_type.CD_TRACK_MODE2_FORM1
							or LibChdr.chd_track_type.CD_TRACK_MODE2_FORM2
							or LibChdr.chd_track_type.CD_TRACK_MODE2_FORM_MIX
							or LibChdr.chd_track_type.CD_TRACK_MODE2_RAW)
						{
							return SessionFormat.Type20_CDXA;
						}
					}

					return SessionFormat.Type00_CDROM_CDDA;
				}

				var TOCMiscInfo = new Synthesize_A0A1A2_Job(
					firstRecordedTrackNumber: 1,
					lastRecordedTrackNumber: chdf.CdMetadatas.Count,
					sessionFormat: GuessSessionFormat(),
					leadoutTimestamp: disc._Sectors.Count);
				TOCMiscInfo.Run(session.RawTOCEntries);

				disc.Sessions.Add(session);
				return disc;
			}
			catch
			{
				disc.Dispose();
				throw;
			}
		}
	}
}