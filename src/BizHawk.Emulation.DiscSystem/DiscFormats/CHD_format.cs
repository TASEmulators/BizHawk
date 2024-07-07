using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.DiscSystem.CUE;

#pragma warning disable BHI1005

// MAME CHD images, using chd-rs for reading
// helpful reference: https://problemkaputt.de/psxspx-cdrom-disk-images-chd-mame.htm

namespace BizHawk.Emulation.DiscSystem
{
	public static class CHD_Format
	{
		/// <summary>
		/// Represents a CHD file.
		/// This isn't particularly faithful to the format, but rather it just wraps a chd_file
		/// </summary>
		public class CHDFile
		{
			/// <summary>
			/// chd_file* to be used for chd_ functions
			/// </summary>
			public IntPtr ChdFile;

			/// <summary>
			/// CHD header, interpreted by chd-rs
			/// </summary>
			public LibChd.chd_header Header;

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
			public LibChd.chd_track_type TrackType;

			/// <summary>
			/// Subcode type
			/// </summary>
			public LibChd.chd_sub_type SubType;

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
			public LibChd.chd_track_type PregapTrackType;

			/// <summary>
			/// Pregap subcode type
			/// </summary>
			public LibChd.chd_sub_type PregapSubType;

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

		private static LibChd.chd_track_type GetTrackType(string type)
		{
			return type switch
			{
				"MODE1" => LibChd.chd_track_type.CD_TRACK_MODE1,
				"MODE1/2048" => LibChd.chd_track_type.CD_TRACK_MODE1,
				"MODE1_RAW" => LibChd.chd_track_type.CD_TRACK_MODE1_RAW,
				"MODE1/2352" => LibChd.chd_track_type.CD_TRACK_MODE1_RAW,
				"MODE2" => LibChd.chd_track_type.CD_TRACK_MODE2,
				"MODE2/2336" => LibChd.chd_track_type.CD_TRACK_MODE2,
				"MODE2_FORM1" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM1,
				"MODE2/2048" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM1,
				"MODE2_FORM2" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM2,
				"MODE2/2324" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM2,
				"MODE2_FORM_MIX" => LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX,
				"MODE2_RAW" => LibChd.chd_track_type.CD_TRACK_MODE2_RAW,
				"MODE2/2352" => LibChd.chd_track_type.CD_TRACK_MODE2_RAW,
				"CDI/2352" => LibChd.chd_track_type.CD_TRACK_MODE2_RAW,
				"AUDIO" => LibChd.chd_track_type.CD_TRACK_AUDIO,
				_ => throw new CHDParseException("Malformed CHD format: Invalid track type!"),
			};
		}

		private static (LibChd.chd_track_type TrackType, bool ChdContainsPregap) GetTrackTypeForPregap(string type)
		{
			if (type.Length > 0 && type[0] == 'V')
			{
				return (GetTrackType(type[1..]), true);
			}

			return (GetTrackType(type), false);
		}

		private static uint GetSectorSize(LibChd.chd_track_type type)
		{
			return type switch
			{
				LibChd.chd_track_type.CD_TRACK_MODE1 => 2048,
				LibChd.chd_track_type.CD_TRACK_MODE1_RAW => 2352,
				LibChd.chd_track_type.CD_TRACK_MODE2 => 2336,
				LibChd.chd_track_type.CD_TRACK_MODE2_FORM1 => 2048,
				LibChd.chd_track_type.CD_TRACK_MODE2_FORM2 => 2324,
				LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX => 2336,
				LibChd.chd_track_type.CD_TRACK_MODE2_RAW => 2352,
				LibChd.chd_track_type.CD_TRACK_AUDIO => 2352,
				_ => throw new CHDParseException("Malformed CHD format: Invalid track type!"),
			};
		}

		private static LibChd.chd_sub_type GetSubType(string type)
		{
			return type switch
			{
				"RW" => LibChd.chd_sub_type.CD_SUB_NORMAL,
				"RW_RAW" => LibChd.chd_sub_type.CD_SUB_RAW,
				"NONE" => LibChd.chd_sub_type.CD_SUB_NONE,
				_ => throw new CHDParseException("Malformed CHD format: Invalid sub type!"),
			};
		}

		private static uint GetSubSize(LibChd.chd_sub_type type)
		{
			return type switch
			{
				LibChd.chd_sub_type.CD_SUB_NORMAL => 96,
				LibChd.chd_sub_type.CD_SUB_RAW => 96,
				LibChd.chd_sub_type.CD_SUB_NONE => 0,
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
				throw new CHDParseException("Malformed CHD format: CHD track type indicates it contained pregap data, but no pregap data is present");
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
					cdMetadata.TrackType = (LibChd.chd_track_type)BinaryPrimitives.ReadUInt32BigEndian(track);
					cdMetadata.SubType = (LibChd.chd_sub_type)BinaryPrimitives.ReadUInt32BigEndian(track[..4]);
					cdMetadata.SectorSize = BinaryPrimitives.ReadUInt32BigEndian(track[..8]);
					cdMetadata.SubSize = BinaryPrimitives.ReadUInt32BigEndian(track[..12]);
					cdMetadata.Frames = BinaryPrimitives.ReadUInt32BigEndian(track[..16]);
					cdMetadata.Padding = BinaryPrimitives.ReadUInt32BigEndian(track[..20]);
				}
				else
				{
					cdMetadata.TrackType = (LibChd.chd_track_type)BinaryPrimitives.ReadUInt32LittleEndian(track);
					cdMetadata.SubType = (LibChd.chd_sub_type)BinaryPrimitives.ReadUInt32LittleEndian(track[..4]);
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
		public static CHDFile ParseFrom(string path)
		{
			var chdf = new CHDFile();
			try
			{
				// .NET Standard 2.0 doesn't have UnmanagedType.LPUTF8Str :(
				// (although .NET Framework has it just fine along with modern .NET)
				var nb = Encoding.UTF8.GetMaxByteCount(path.Length);
				var ptr = Marshal.AllocCoTaskMem(checked(nb + 1));
				try
				{
					unsafe
					{
						fixed (char* c = path)
						{
							var pbMem = (byte*)ptr;
							var nbWritten = Encoding.UTF8.GetBytes(c, path.Length, pbMem!, nb);
							pbMem[nbWritten] = 0;
						}
					}

					var err = LibChd.chd_open(ptr, LibChd.CHD_OPEN_READ, IntPtr.Zero, out chdf.ChdFile);
					if (err != LibChd.chd_error.CHDERR_NONE)
					{
						throw new CHDParseException($"Malformed CHD format: Failed to open chd, got error {err}");
					}

					err = LibChd.chd_read_header(ptr, ref chdf.Header);
					if (err != LibChd.chd_error.CHDERR_NONE)
					{
						throw new CHDParseException($"Malformed CHD format: Failed to read chd header, got error {err}");
					}
				}
				finally
				{
					Marshal.FreeCoTaskMem(ptr);
				}

				if (chdf.Header.hunkbytes == 0 || chdf.Header.hunkbytes % LibChd.CD_FRAME_SIZE != 0)
				{
					throw new CHDParseException("Malformed CHD format: Invalid hunk size");
				}

				// chd-rs puts the correct value here for older versions of chds which don't have this
				// for newer chds, it is left as is, which might be invalid
				if (chdf.Header.unitbytes != LibChd.CD_FRAME_SIZE)
				{
					throw new CHDParseException("Malformed CHD format: Invalid unit size");
				}

				var metadataOutput = new byte[256];
				for (uint i = 0; i < 99; i++)
				{
					var err = LibChd.chd_get_metadata(chdf.ChdFile, LibChd.CDROM_TRACK_METADATA2_TAG,
						i, metadataOutput, (uint)metadataOutput.Length, out var resultLen, out _, out _);
					if (err == LibChd.chd_error.CHDERR_NONE)
					{
						var metadata = Encoding.ASCII.GetString(metadataOutput, 0,  (int)resultLen).TrimEnd('\0');
						chdf.CdMetadatas.Add(ParseMetadata2(metadata));
						continue;
					}

					err = LibChd.chd_get_metadata(chdf.ChdFile, LibChd.CDROM_TRACK_METADATA_TAG,
						i, metadataOutput, (uint)metadataOutput.Length, out resultLen, out _, out _);
					if (err == LibChd.chd_error.CHDERR_NONE)
					{
						var metadata = Encoding.ASCII.GetString(metadataOutput, 0,  (int)resultLen).TrimEnd('\0');
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
					var err = LibChd.chd_get_metadata(chdf.ChdFile, LibChd.CDROM_OLD_METADATA_TAG,
						0, metadataOutput, (uint)metadataOutput.Length, out var resultLen, out _, out _);
					if (err == LibChd.chd_error.CHDERR_NONE)
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

				// pad expected sectors up to the next hunk
				var sectorsPerHunk = chdf.Header.hunkbytes / LibChd.CD_FRAME_SIZE;
				chdExpectedNumSectors = (chdExpectedNumSectors + sectorsPerHunk - 1) / sectorsPerHunk * sectorsPerHunk;

				var chdActualNumSectors = chdf.Header.hunkcount * sectorsPerHunk;
				if (chdExpectedNumSectors != chdActualNumSectors)
				{
					throw new CHDParseException("Malformed CHD format: Mismatch in expected and actual number of sectors present");
				}

				return chdf;
			}
			catch (Exception ex)
			{
				if (chdf.ChdFile != IntPtr.Zero)
				{
					LibChd.chd_close(chdf.ChdFile);
				}

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

				ret.ParsedCHDFile = ParseFrom(path);
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
			private readonly uint _subOffset;
			private readonly bool _isInterleaved;

			public SS_CHD_Sub(SS_Base baseSynth, uint subOffset, bool isInterleaved)
			{
				_baseSynth = baseSynth;
				_subOffset = subOffset;
				_isInterleaved = isInterleaved;
			}

			public void Synth(SectorSynthJob job)
			{
				if ((job.Parts & ESectorSynthPart.SubcodeAny) != 0)
				{
					_baseSynth.Blob.Read(_baseSynth.BlobOffset + _subOffset, job.DestBuffer2448, job.DestOffset + 2352, 96);
					job.Parts &= ~ESectorSynthPart.SubcodeAny;

					if ((job.Parts & ESectorSynthPart.SubcodeDeinterleave) != 0 && _isInterleaved)
					{
						SynthUtils.DeinterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
					}

					if ((job.Parts & ESectorSynthPart.SubcodeDeinterleave) == 0 && !_isInterleaved)
					{
						SynthUtils.InterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
					}
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
				IBlob chdBlob = new Blob_CHD(chdf.ChdFile, chdf.Header.hunkbytes);
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
						var control = cdMetadata.TrackType != LibChd.chd_track_type.CD_TRACK_AUDIO
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

					static SS_Base CreateSynth(LibChd.chd_track_type trackType)
					{
						return trackType switch
						{
							LibChd.chd_track_type.CD_TRACK_MODE1 => new SS_Mode1_2048(),
							LibChd.chd_track_type.CD_TRACK_MODE1_RAW => new SS_2352(),
							LibChd.chd_track_type.CD_TRACK_MODE2 => new SS_Mode2_2336(),
							LibChd.chd_track_type.CD_TRACK_MODE2_FORM1 => new SS_Mode2_Form1_2048(),
							LibChd.chd_track_type.CD_TRACK_MODE2_FORM2 => new SS_Mode2_Form2_2324(),
							LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX => new SS_Mode2_2336(),
							LibChd.chd_track_type.CD_TRACK_MODE2_RAW => new SS_2352(),
							LibChd.chd_track_type.CD_TRACK_AUDIO => new SS_CHD_Audio(),
							_ => throw new InvalidOperationException(),
						};
					}

					static CueTrackType ToCueTrackType(LibChd.chd_track_type chdTrackType, bool isCdi)
					{
						// rough matches, not too important if these are somewhat wrong (they're just used for generated gaps)
						return chdTrackType switch
						{
							LibChd.chd_track_type.CD_TRACK_MODE1 => CueTrackType.Mode1_2048,
							LibChd.chd_track_type.CD_TRACK_MODE1_RAW => CueTrackType.Mode1_2352,
							LibChd.chd_track_type.CD_TRACK_MODE2 => CueTrackType.Mode2_2336,
							LibChd.chd_track_type.CD_TRACK_MODE2_FORM1 => CueTrackType.Mode2_2336,
							LibChd.chd_track_type.CD_TRACK_MODE2_FORM2 => CueTrackType.Mode2_2336,
							LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX => CueTrackType.Mode2_2336,
							LibChd.chd_track_type.CD_TRACK_MODE2_RAW when isCdi => CueTrackType.CDI_2352,
							LibChd.chd_track_type.CD_TRACK_MODE2_RAW => CueTrackType.Mode2_2352,
							LibChd.chd_track_type.CD_TRACK_AUDIO => CueTrackType.Audio,
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
						var control = cdMetadata.PregapTrackType != LibChd.chd_track_type.CD_TRACK_AUDIO
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
								LibChd.chd_sub_type.CD_SUB_NORMAL => new SS_CHD_Sub(synth, GetSectorSize(cdMetadata.PregapTrackType), isInterleaved: false),
								LibChd.chd_sub_type.CD_SUB_RAW => new SS_CHD_Sub(synth, GetSectorSize(cdMetadata.PregapTrackType), isInterleaved: true),
								LibChd.chd_sub_type.CD_SUB_NONE => synth,
								_ => throw new InvalidOperationException(),
							};

							disc._Sectors.Add(chdSynth);
							chdOffset += LibChd.CD_FRAME_SIZE;
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
						var control = cdMetadata.TrackType != LibChd.chd_track_type.CD_TRACK_AUDIO
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
							LibChd.chd_sub_type.CD_SUB_NORMAL => new SS_CHD_Sub(synth, cdMetadata.SectorSize, isInterleaved: false),
							LibChd.chd_sub_type.CD_SUB_RAW => new SS_CHD_Sub(synth, cdMetadata.SectorSize, isInterleaved: true),
							LibChd.chd_sub_type.CD_SUB_NONE => synth,
							_ => throw new InvalidOperationException(),
						};
						disc._Sectors.Add(chdSynth);
						chdOffset += LibChd.CD_FRAME_SIZE;
						relMSF++;
					}

					chdOffset += cdMetadata.Padding * LibChd.CD_FRAME_SIZE;

					for (var i = 0; i < cdMetadata.PostGap; i++)
					{
						var synth = new SS_Gap
						{
							TrackType = ToCueTrackType(cdMetadata.TrackType, cdMetadata.IsCDI),
							Policy = IN_DiscMountPolicy
						};
						const byte kADR = 1;
						var control = cdMetadata.TrackType != LibChd.chd_track_type.CD_TRACK_AUDIO
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

						if (cdMetadata.TrackType is LibChd.chd_track_type.CD_TRACK_MODE2
							or LibChd.chd_track_type.CD_TRACK_MODE2_FORM1
							or LibChd.chd_track_type.CD_TRACK_MODE2_FORM2
							or LibChd.chd_track_type.CD_TRACK_MODE2_FORM_MIX
							or LibChd.chd_track_type.CD_TRACK_MODE2_RAW)
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

		// crc16 table taken from https://github.com/mamedev/mame/blob/26b5eb211924acbe4b78f67da8d0ae3cbe77aa6d/src/lib/util/hashing.cpp#L400C1-L434C4
		private static readonly ushort[] _crc16Table =
		{
			0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50A5, 0x60C6, 0x70E7,
			0x8108, 0x9129, 0xA14A, 0xB16B, 0xC18C, 0xD1AD, 0xE1CE, 0xF1EF,
			0x1231, 0x0210, 0x3273, 0x2252, 0x52B5, 0x4294, 0x72F7, 0x62D6,
			0x9339, 0x8318, 0xB37B, 0xA35A, 0xD3BD, 0xC39C, 0xF3FF, 0xE3DE,
			0x2462, 0x3443, 0x0420, 0x1401, 0x64E6, 0x74C7, 0x44A4, 0x5485,
			0xA56A, 0xB54B, 0x8528, 0x9509, 0xE5EE, 0xF5CF, 0xC5AC, 0xD58D,
			0x3653, 0x2672, 0x1611, 0x0630, 0x76D7, 0x66F6, 0x5695, 0x46B4,
			0xB75B, 0xA77A, 0x9719, 0x8738, 0xF7DF, 0xE7FE, 0xD79D, 0xC7BC,
			0x48C4, 0x58E5, 0x6886, 0x78A7, 0x0840, 0x1861, 0x2802, 0x3823,
			0xC9CC, 0xD9ED, 0xE98E, 0xF9AF, 0x8948, 0x9969, 0xA90A, 0xB92B,
			0x5AF5, 0x4AD4, 0x7AB7, 0x6A96, 0x1A71, 0x0A50, 0x3A33, 0x2A12,
			0xDBFD, 0xCBDC, 0xFBBF, 0xEB9E, 0x9B79, 0x8B58, 0xBB3B, 0xAB1A,
			0x6CA6, 0x7C87, 0x4CE4, 0x5CC5, 0x2C22, 0x3C03, 0x0C60, 0x1C41,
			0xEDAE, 0xFD8F, 0xCDEC, 0xDDCD, 0xAD2A, 0xBD0B, 0x8D68, 0x9D49,
			0x7E97, 0x6EB6, 0x5ED5, 0x4EF4, 0x3E13, 0x2E32, 0x1E51, 0x0E70,
			0xFF9F, 0xEFBE, 0xDFDD, 0xCFFC, 0xBF1B, 0xAF3A, 0x9F59, 0x8F78,
			0x9188, 0x81A9, 0xB1CA, 0xA1EB, 0xD10C, 0xC12D, 0xF14E, 0xE16F,
			0x1080, 0x00A1, 0x30C2, 0x20E3, 0x5004, 0x4025, 0x7046, 0x6067,
			0x83B9, 0x9398, 0xA3FB, 0xB3DA, 0xC33D, 0xD31C, 0xE37F, 0xF35E,
			0x02B1, 0x1290, 0x22F3, 0x32D2, 0x4235, 0x5214, 0x6277, 0x7256,
			0xB5EA, 0xA5CB, 0x95A8, 0x8589, 0xF56E, 0xE54F, 0xD52C, 0xC50D,
			0x34E2, 0x24C3, 0x14A0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
			0xA7DB, 0xB7FA, 0x8799, 0x97B8, 0xE75F, 0xF77E, 0xC71D, 0xD73C,
			0x26D3, 0x36F2, 0x0691, 0x16B0, 0x6657, 0x7676, 0x4615, 0x5634,
			0xD94C, 0xC96D, 0xF90E, 0xE92F, 0x99C8, 0x89E9, 0xB98A, 0xA9AB,
			0x5844, 0x4865, 0x7806, 0x6827, 0x18C0, 0x08E1, 0x3882, 0x28A3,
			0xCB7D, 0xDB5C, 0xEB3F, 0xFB1E, 0x8BF9, 0x9BD8, 0xABBB, 0xBB9A,
			0x4A75, 0x5A54, 0x6A37, 0x7A16, 0x0AF1, 0x1AD0, 0x2AB3, 0x3A92,
			0xFD2E, 0xED0F, 0xDD6C, 0xCD4D, 0xBDAA, 0xAD8B, 0x9DE8, 0x8DC9,
			0x7C26, 0x6C07, 0x5C64, 0x4C45, 0x3CA2, 0x2C83, 0x1CE0, 0x0CC1,
			0xEF1F, 0xFF3E, 0xCF5D, 0xDF7C, 0xAF9B, 0xBFBA, 0x8FD9, 0x9FF8,
			0x6E17, 0x7E36, 0x4E55, 0x5E74, 0x2E93, 0x3EB2, 0x0ED1, 0x1EF0,
		};

		private static ushort CalcCrc16(ReadOnlySpan<byte> bytes)
		{
			ushort crc16 = 0xFFFF;
			foreach (var b in bytes)
			{
				crc16 = (ushort)((crc16 << 8) ^ _crc16Table[(crc16 >> 8) ^ b]);
			}

			return crc16;
		}

		private class ChdHunkMapEntry
		{
			public uint CompressedLength;
			public long HunkOffset;
			public ushort Crc16;
		}

		private static readonly byte[] _chdTag = Encoding.ASCII.GetBytes("MComprHD");
		// 8 frames is apparently the standard, but we can probably afford to go a little extra ;)
		private const uint CD_FRAMES_PER_HUNK = 75; // 1 second

		public static void Dump(Disc disc, string path)
		{
			// limited dumping support, v5 only with zstd compression
			// however, this is a lot better than a lot of other dumps
			// as the reference chdman can't easily dump subcode data
			// this is important as chds don't have index info listed!

			// check if we have a multisession disc (CHD doesn't support those)
			if (disc.Sessions.Count > 2)
			{
				throw new NotSupportedException("CHD does not support multisession discs");
			}

			using var fs = File.Create(path);
			using var bw = new BinaryWriter(fs);

			// write header
			// note CHD header has values in big endian, while BinaryWriter will write in little endian
			bw.Write(_chdTag);
			bw.Write(BinaryPrimitives.ReverseEndianness(LibChd.CHD_V5_HEADER_SIZE));
			bw.Write(BinaryPrimitives.ReverseEndianness(LibChd.CHD_HEADER_VERSION));
			// v5 chd allows for 4 different compression types
			// we only have 1 implemented here
			bw.Write(BinaryPrimitives.ReverseEndianness(LibChd.CHD_CODEC_ZSTD));
			bw.Write(0);
			bw.Write(0);
			bw.Write(0);
			bw.Write(0L); // total size of all uncompressed data (written later)
			bw.Write(0L); // offset to hunk map (written later)
			bw.Write(0L); // offset to first metadata (written later)
			bw.Write(BinaryPrimitives.ReverseEndianness(LibChd.CD_FRAME_SIZE * CD_FRAMES_PER_HUNK)); // bytes per hunk
			bw.Write(BinaryPrimitives.ReverseEndianness(LibChd.CD_FRAME_SIZE)); // bytes per sector (always CD_FRAME_SIZE)
			var blankSha1 = new byte[LibChd.CHD_SHA1_BYTES];
			bw.Write(blankSha1); // SHA1 of raw data (written later)
			bw.Write(blankSha1); // SHA1 of raw data + metadata (written later)
			bw.Write(blankSha1); // SHA1 of raw data + metadata for parent (N/A, always 0 for us)

			// collect metadata
			var cdMetadatas = new List<CHDCdMetadata>();
			var session = disc.Session1;
			for (var i = 1; i <= session.InformationTrackCount; i++)
			{
				var track = session.Tracks[i];
				// frames includes the pregap, so we need to make sure to include the first pregap
				// other pregaps can be included as part of the previous track
				// not really so bad in practice, since we do full raw tracks
				var firstIndexLba = track.Number == 1 ? 0 : track.LBA;

				var cdMetadata = new CHDCdMetadata
				{
					Track = (uint)track.Number,
					IsCDI = track.Mode == 2 && session.TOC.SessionFormat == SessionFormat.Type10_CDI,
					TrackType = track.Mode switch
					{
						0 => LibChd.chd_track_type.CD_TRACK_AUDIO,
						1 => LibChd.chd_track_type.CD_TRACK_MODE1_RAW,
						2 => LibChd.chd_track_type.CD_TRACK_MODE2_RAW,
						_ => throw new InvalidOperationException(),
					},
					SubType = LibChd.chd_sub_type.CD_SUB_RAW,
					SectorSize = 2352,
					SubSize = 96,
					Frames = (uint)(track.NextTrack.LBA - firstIndexLba),
					Pregap = (uint)(track.LBA - firstIndexLba),
					PostGap = 0,
				};

				cdMetadata.PregapInChd = cdMetadata.Pregap > 0;
				cdMetadata.Padding = (0 - cdMetadata.Frames) & 3;
				cdMetadata.PregapTrackType = cdMetadata.TrackType;
				cdMetadatas.Add(cdMetadata);
			}

			// we'll need to collect hunk locations for the hunk map
			var hunkMapEntries = new List<ChdHunkMapEntry>();
			// a "proper" CHD should have a SHA1 of the uncompressed contents
			using var sha1Inc = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
			using var zstd = new Zstd();
			var dsr = new DiscSectorReader(disc) { Policy = { DeinterleavedSubcode = true, DeterministicClearBuffer = true } };
			var sectorBuf = new byte[LibChd.CD_FRAME_SIZE];
			var cdLba = 0;
			uint chdLba = 0, chdPos;
			var curHunk = new byte[LibChd.CD_FRAME_SIZE * CD_FRAMES_PER_HUNK];
#if false // TODO: cdzs
			const uint COMPRESSION_LEN_BYTES = LibChd.CD_FRAME_SIZE * CD_FRAMES_PER_HUNK < 65536 ? 2 : 3;
			const uint ECC_BYTES = (CD_FRAMES_PER_HUNK + 7) / 8;
			var hunkHeader = new byte[COMPRESSION_LEN_BYTES + ECC_BYTES];
#endif

			void EndHunk(uint hashLen)
			{
				var hunkOffset = bw.BaseStream.Position;

				// TODO: adjust compression level?
				using (var cstream = zstd.CreateZstdCompressionStream(bw.BaseStream, Zstd.MaxCompressionLevel))
				{
					cstream.Write(curHunk, 0, curHunk.Length);
				}

				hunkMapEntries.Add(new()
				{
					CompressedLength = (uint)(bw.BaseStream.Position - hunkOffset),
					HunkOffset = hunkOffset,
					Crc16 = CalcCrc16(curHunk),
				});

				sha1Inc.AppendData(curHunk, 0, (int)hashLen);
				Array.Clear(curHunk, 0, curHunk.Length);
			}

			foreach (var cdMetadata in cdMetadatas)
			{
				for (var i = 0; i < cdMetadata.Frames; i++)
				{
					dsr.ReadLBA_2448(cdLba, sectorBuf, 0);

					// audio samples are byteswapped, so make sure to account for that
					var trackType = i < cdMetadata.Pregap ? cdMetadata.PregapTrackType : cdMetadata.TrackType;
					if (trackType == LibChd.chd_track_type.CD_TRACK_AUDIO)
					{
						EndiannessUtils.MutatingByteSwap16(sectorBuf.AsSpan()[..2352]);
					}

					chdPos = chdLba % CD_FRAMES_PER_HUNK;
#if false // TODO: cdzs
					Buffer.BlockCopy(sectorBuf, 0, curHunk, (int)(2352U * chdPos), 2352);
					Buffer.BlockCopy(sectorBuf, 2352, curHunk, (int)(2352U * CD_FRAMES_PER_HUNK + 96U * chdPos), 96);
#else
					Buffer.BlockCopy(sectorBuf, 0, curHunk, (int)(LibChd.CD_FRAME_SIZE * chdPos), (int)LibChd.CD_FRAME_SIZE);
#endif
					if (chdPos == CD_FRAMES_PER_HUNK - 1)
					{
						EndHunk(CD_FRAMES_PER_HUNK * LibChd.CD_FRAME_SIZE);
					}

					cdLba++;
					chdLba++;
				}

				for (var i = 0; i < cdMetadata.Padding; i++)
				{
					chdPos = chdLba % CD_FRAMES_PER_HUNK;
					if (chdPos == CD_FRAMES_PER_HUNK - 1)
					{
						EndHunk(CD_FRAMES_PER_HUNK * LibChd.CD_FRAME_SIZE);
					}

					chdLba++;
				}
			}

			// make sure to write out any remaining pending hunk
			chdPos = chdLba % CD_FRAMES_PER_HUNK;
			if (chdPos != 0)
			{
				EndHunk(chdPos * LibChd.CD_FRAME_SIZE);
			}

			static string TrackTypeStr(LibChd.chd_track_type trackType, bool isCdi)
			{
				// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
				return trackType switch
				{
					LibChd.chd_track_type.CD_TRACK_AUDIO => "AUDIO",
					LibChd.chd_track_type.CD_TRACK_MODE1_RAW => "MODE1_RAW",
					LibChd.chd_track_type.CD_TRACK_MODE2_RAW when isCdi => "CDI/2352",
					LibChd.chd_track_type.CD_TRACK_MODE2_RAW => "MODE2_RAW",
					_ => throw new InvalidOperationException(),
				};
			}

			var metadataOffset = bw.BaseStream.Position;
			var metadataHashes = new byte[cdMetadatas.Count][];
			// write metadata
			for (var i = 0; i < cdMetadatas.Count; i++)
			{
				var cdMetadata = cdMetadatas[i];
				var trackType = TrackTypeStr(cdMetadata.TrackType, cdMetadata.IsCDI);
				var pgTrackType = TrackTypeStr(cdMetadata.PregapTrackType, cdMetadata.IsCDI);
				if (cdMetadata.PregapInChd)
				{
					pgTrackType = $"V{pgTrackType}";
				}

				var metadataStr = $"TRACK:{cdMetadata.Track} TYPE:{trackType} SUBTYPE:RW FRAMES:{cdMetadata.Frames} PREGAP:{cdMetadata.Pregap} PGTYPE:{pgTrackType} PGSUB:RW POSTGAP:0\0";
				var metadataBytes = Encoding.ASCII.GetBytes(metadataStr);

				bw.Write(BinaryPrimitives.ReverseEndianness(LibChd.CDROM_TRACK_METADATA2_TAG));
				bw.Write(LibChd.CHD_MDFLAGS_CHECKSUM);
				var chunkDataSize = new byte[3]; // 24 bit integer
				chunkDataSize[0] = (byte)((metadataBytes.Length >> 16) & 0xFF);
				chunkDataSize[1] = (byte)((metadataBytes.Length >> 8) & 0xFF);
				chunkDataSize[2] = (byte)(metadataBytes.Length & 0xFF);
				bw.Write(chunkDataSize);

				bw.Write(i == cdMetadatas.Count - 1
					? 0L // last chunk
					: BinaryPrimitives.ReverseEndianness(bw.BaseStream.Position + 8 + metadataBytes.Length)); // offset to next chunk

				bw.Write(metadataBytes);

				metadataHashes[i] = SHA1Checksum.Compute(metadataBytes);
			}

			var uncompressedHunkMap = new byte[hunkMapEntries.Count * 12];
			// compute uncompressed hunk map
			for (var i = 0; i < hunkMapEntries.Count; i++)
			{
				var hunkMapEntry = hunkMapEntries[i];
				var mapEntryOffset = i * 12;
				uncompressedHunkMap[mapEntryOffset + 0] = 0; // Codec 0
				uncompressedHunkMap[mapEntryOffset + 1] = (byte)((hunkMapEntry.CompressedLength >> 16) & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 2] = (byte)((hunkMapEntry.CompressedLength >> 8) & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 3] = (byte)(hunkMapEntry.CompressedLength & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 4] = (byte)((hunkMapEntry.HunkOffset >> 40) & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 5] = (byte)((hunkMapEntry.HunkOffset >> 32) & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 6] = (byte)((hunkMapEntry.HunkOffset >> 24) & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 7] = (byte)((hunkMapEntry.HunkOffset >> 16) & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 8] = (byte)((hunkMapEntry.HunkOffset >> 8) & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 9] = (byte)(hunkMapEntry.HunkOffset & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 10] = (byte)((hunkMapEntry.Crc16 >> 8) & 0xFF);
				uncompressedHunkMap[mapEntryOffset + 11] = (byte)(hunkMapEntry.Crc16 & 0xFF);
			}

			var hunkMapCrc16 = CalcCrc16(uncompressedHunkMap);
			var hunkMapOffset = bw.BaseStream.Position;

			var firstOffset = new byte[6];
			// write hunk map header
			bw.Write(0); // compressed map length (written later)
			Buffer.BlockCopy(uncompressedHunkMap, 4, firstOffset, 0, 6);
			bw.Write(firstOffset); // first hunk offset
			bw.Write(BinaryPrimitives.ReverseEndianness(hunkMapCrc16)); // uncompressed map crc16
			bw.Write((byte)24); // num bits used to stored compression length
			bw.Write((byte)0); // num bits used to stored self refs (not used)
			bw.Write((byte)0); // num bits used to stored parent unit refs (not used)
			bw.Write((byte)0); // reserved (should just be 0)

			// huffman map
			// we always have anything decoded return 0
			// so we can be lazy here an define a somewhat bogus map which allows us to skip compression

			bw.Write((byte)0x11); // makes command 0 take 1 bit

			// 60 bits are now left to write for the huffman map, we'll want them all to be 0
			// also, after the huffman map proceeds the compression type bits, which for us will just be a ton of 0 bits
			// each hunk needing a bit set to 0 to indicate compression type 0

			// basic bit writing code
			byte curByte = 0;
			var curBit = 60 + hunkMapEntries.Count;
			while (curBit >= 8)
			{
				bw.Write((byte)0);
				curBit -= 8;
			}

			void WriteByteBits(byte b)
			{
				for (var i = 0; i < 8; i++)
				{
					var bit = ((b >> (7 - i)) & 1) != 0;
					if (bit)
					{
						curByte |= (byte)(1 << (7 - curBit));
					}

					curBit++;
					if (curBit == 8)
					{
						bw.Write(curByte);
						curBit = 0;
						curByte = 0;
					}
				}
			}

			for (var i = 0; i < hunkMapEntries.Count; i++)
			{
				var mapEntryOffset = i * 12;
				// length
				WriteByteBits(uncompressedHunkMap[mapEntryOffset + 1]);
				WriteByteBits(uncompressedHunkMap[mapEntryOffset + 2]);
				WriteByteBits(uncompressedHunkMap[mapEntryOffset + 3]);
				// crc16
				WriteByteBits(uncompressedHunkMap[mapEntryOffset + 10]);
				WriteByteBits(uncompressedHunkMap[mapEntryOffset + 11]);
			}

			// write final byte if present
			if (curBit != 0)
			{
				bw.Write(curByte);
			}

			// finish everything up

			var hunkMapEnd = bw.BaseStream.Position;
			bw.BaseStream.Seek(hunkMapOffset, SeekOrigin.Begin);
			// hunk map length sans header
			bw.Write(BinaryPrimitives.ReverseEndianness((uint)(hunkMapEnd - hunkMapOffset - 16)));

			bw.BaseStream.Seek(0x20, SeekOrigin.Begin);
			bw.Write(BinaryPrimitives.ReverseEndianness(chdLba * (long)LibChd.CD_FRAME_SIZE));
			bw.Write(BinaryPrimitives.ReverseEndianness(hunkMapOffset));
			bw.Write(BinaryPrimitives.ReverseEndianness(metadataOffset));

			var rawSha1 = sha1Inc.GetHashAndReset();
			// calc overall sha1 now (uses raw sha1 and metadata hashes)
			sha1Inc.AppendData(rawSha1);
			// apparently these are expected to be sorted with memcmp semantics
			Array.Sort(metadataHashes, static (x, y) =>
			{
				for (var i = 0; i < x.Length; i++)
				{
					if (x[i] < y[i])
					{
						return -1;
					}

					if (x[i] > y[i])
					{
						return 1;
					}
				}

				return 0;
			});

			// tag is hashed alongside the hash
			// we use the same tag every time, so we can just reuse this array
			var metadataTag = new byte[4];
			metadataTag[0] = (byte)((LibChd.CDROM_TRACK_METADATA2_TAG >> 24) & 0xFF);
			metadataTag[1] = (byte)((LibChd.CDROM_TRACK_METADATA2_TAG >> 16) & 0xFF);
			metadataTag[2] = (byte)((LibChd.CDROM_TRACK_METADATA2_TAG >> 8) & 0xFF);
			metadataTag[3] = (byte)(LibChd.CDROM_TRACK_METADATA2_TAG & 0xFF);
			foreach (var metadataHash in metadataHashes)
			{
				sha1Inc.AppendData(metadataTag);
				sha1Inc.AppendData(metadataHash);
			}

			var overallSha1 = sha1Inc.GetHashAndReset();

			bw.BaseStream.Seek(0x40, SeekOrigin.Begin);
			bw.Write(rawSha1);
			bw.Write(overallSha1);
		}
	}
}
