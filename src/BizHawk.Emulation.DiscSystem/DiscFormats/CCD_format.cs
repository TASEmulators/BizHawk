using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;

//check out ccd2iso linux program?
//https://wiki.archlinux.org/index.php/CD_Burning#TOC.2FCUE.2FBIN_for_mixed-mode_disks advice here
//also referencing mednafen sources

//TODO - copy mednafen sanity checking
//TODO - check subcode vs TOC (warnings if different)
//TODO - check TOC vs tracks (warnings if different)

namespace BizHawk.Emulation.DiscSystem
{
	public class CCD_Format
	{
		/// <summary>
		/// Represents a CCD file, faithfully. Minimal interpretation of the data happens.
		/// Currently the [TRACK] sections aren't parsed, though.
		/// </summary>
		public class CCDFile
		{
			/// <summary>
			/// which version CCD file this came from. We hope it shouldn't affect the semantics of anything else in here, but just in case..
			/// </summary>
			public int Version;

			/// <summary>
			/// this is probably a 0 or 1 bool
			/// </summary>
			public int DataTracksScrambled;

			/// <summary>
			/// ???
			/// </summary>
			public int CDTextLength;

			/// <summary>
			/// The [Session] sections
			/// </summary>
			public readonly IList<CCDSession> Sessions = new List<CCDSession>();

			/// <summary>
			/// The [Entry] sctions
			/// </summary>
			public readonly IList<CCDTocEntry> TOCEntries = new List<CCDTocEntry>();

			/// <summary>
			/// The [TRACK] sections
			/// </summary>
			public readonly IList<CCDTrack> Tracks = new List<CCDTrack>();

			/// <summary>
			/// The [TRACK] sections, indexed by number
			/// </summary>
			public readonly IDictionary<int, CCDTrack> TracksByNumber = new Dictionary<int, CCDTrack>();
		}

		/// <summary>
		/// Represents an [Entry] section from a CCD file
		/// </summary>
		public class CCDTocEntry
		{
			public CCDTocEntry(int entryNum)
			{
				EntryNum = entryNum;
			}
			
			/// <summary>
			/// these should be 0-indexed
			/// </summary>
			public int EntryNum;

			/// <summary>
			/// the CCD specifies this, but it isnt in the actual disc data as such, it is encoded some other (likely difficult to extract) way and that's why CCD puts it here
			/// </summary>
			public int Session;

			/// <summary>
			/// this seems just to be the LBA corresponding to AMIN:ASEC:AFRAME (give or take 150). It's not stored on the disc, and it's redundant.
			/// </summary>
			public int ALBA;

			/// <summary>
			/// this seems just to be the LBA corresponding to PMIN:PSEC:PFRAME (give or take 150). It's not stored on the disc, and it's redundant.
			/// </summary>
			public int PLBA;

			//these correspond pretty directly to values in the Q subchannel fields
			//NOTE: they're specified as absolute MSF. That means, they're 2 seconds off from what they should be when viewed as final TOC values
			public int Control;
			public int ADR;
			public int TrackNo;
			public int Point;
			public int AMin;
			public int ASec;
			public int AFrame;
			public int Zero;
			public int PMin;
			public int PSec;
			public int PFrame;
		}

		/// <summary>
		/// Represents a [Track] section from a CCD file
		/// </summary>
		public class CCDTrack
		{
			public CCDTrack(int number)
			{
				this.Number = number;
			}

			/// <summary>
			/// note: this is 1-indexed
			/// </summary>
			public int Number;

			/// <summary>
			/// The specified data mode.
			/// </summary>
			public int Mode;

			/// <summary>
			/// The indexes specified for the track (these are 0-indexed)
			/// </summary>
			public readonly IDictionary<int, int> Indexes = new Dictionary<int, int>();
		}

		/// <summary>
		/// Represents a [Session] section from a CCD file
		/// </summary>
		public class CCDSession
		{
			public CCDSession(int number)
			{
				this.Number = number;
			}

			/// <summary>
			/// note: this is 1-indexed.
			/// </summary>
			public int Number;

			//Not sure what the default should be.. ive only seen mode=2
			public int PregapMode;

			/// <summary>
			/// this is probably a 0 or 1 bool
			/// </summary>
			public int PregapSubcode;
		}

		public class CCDParseException : Exception
		{
			public CCDParseException(string message) : base(message) { }
		}

		private class CCDSection : Dictionary<string, int>
		{
			public string Name;

			public int FetchOrDefault(int def, string key)
				=> TryGetValue(key, out int val) ? val : def;

			/// <exception cref="CCDParseException"><paramref name="key"/> not found in <see langword="this"/></exception>
			public int FetchOrFail(string key)
			{
				if (!TryGetValue(key, out int ret))
					throw new CCDParseException($"Malformed or unexpected CCD format: missing required [Entry] key: {key}");
				return ret;
			}
		}

		private static List<CCDSection> ParseSections(Stream stream)
		{
			List<CCDSection> sections = new List<CCDSection>();

			//TODO - do we need to attempt to parse out the version tag in a first pass?
			//im doing this from a version 3 example

			StreamReader sr = new StreamReader(stream);
			CCDSection currSection = null;
			while (true)
			{
				string line = sr.ReadLine();
				if (line is null) break;
				if (line == string.Empty) continue;
				if (line.StartsWith('['))
				{
					currSection = new()
					{
						Name = line.Trim('[', ']').ToUpperInvariant()
					};
					sections.Add(currSection);
				}
				else
				{
					if (currSection is null)
						throw new CCDParseException("Malformed or unexpected CCD format: started without [");
					string[] parts = line.Split('=');
					if (parts.Length != 2)
						throw new CCDParseException("Malformed or unexpected CCD format: parsing item into two parts");
					if (parts[0].ToUpperInvariant() == "FLAGS")
					{
						// flags are a space-separated collection of symbolic constants:
						// https://www.gnu.org/software/ccd2cue/manual/html_node/FLAGS-_0028Compact-Disc-fields_0029.html#FLAGS-_0028Compact-Disc-fields_0029
						// But we don't seem to do anything with them?  Skip because the subsequent code will fail to parse them
						continue;
					}
					int val;
					if (parts[1].StartsWith("0x", StringComparison.OrdinalIgnoreCase))
						val = int.Parse(parts[1].Substring(2), NumberStyles.HexNumber);
					else
						val = int.Parse(parts[1]);
					currSection[parts[0].ToUpperInvariant()] = val;
				}
			}

			return sections;
		}

		private static int PreParseIntegrityCheck(IReadOnlyList<CCDSection> sections)
		{
			switch (sections.Count)
			{
				case 0:
					throw new CCDParseException("Malformed CCD format: no sections");
				//we need at least a CloneCD and Disc section
				case < 2:
					throw new CCDParseException("Malformed CCD format: insufficient sections");
			}

			var ccdSection = sections[0];
			if (ccdSection.Name != "CLONECD")
				throw new CCDParseException("Malformed CCD format: confusing first section name");

			if (!ccdSection.TryGetValue("VERSION", out int version))
				throw new CCDParseException("Malformed CCD format: missing version in CloneCD section");

			if (sections[1].Name != "DISC")
				throw new CCDParseException("Malformed CCD format: section[1] isn't [Disc]");

			return version;
		}

		/// <exception cref="CCDParseException">parsed <see cref="CCDFile.DataTracksScrambled"/> is <c>1</c>, parsed session number is not <c>1</c>, or malformed entry</exception>
		public static CCDFile ParseFrom(Stream stream)
		{
			CCDFile ccdf = new CCDFile();

			var sections = ParseSections(stream);
			ccdf.Version = PreParseIntegrityCheck(sections);

			var discSection = sections[1];
			int nTocEntries = discSection["TOCENTRIES"]; //its conceivable that this could be missing
			int nSessions = discSection["SESSIONS"]; //its conceivable that this could be missing
			ccdf.DataTracksScrambled = discSection.FetchOrDefault(0, "DATATRACKSSCRAMBLED");
			ccdf.CDTextLength = discSection.FetchOrDefault(0, "CDTEXTLENGTH");

			if (ccdf.DataTracksScrambled == 1)
				throw new CCDParseException($"Malformed CCD format: {nameof(ccdf.DataTracksScrambled)}=1 not supported. Please report this, so we can understand what it means.");

			for (int i = 2; i < sections.Count; i++)
			{
				var section = sections[i];
				if (section.Name.StartsWithOrdinal("SESSION"))
				{
					int sesnum = int.Parse(section.Name.Split(' ')[1]);
					CCDSession session = new CCDSession(sesnum);
					ccdf.Sessions.Add(session);
					if (sesnum != ccdf.Sessions.Count)
						throw new CCDParseException("Malformed CCD format: wrong session number in sequence");
					session.PregapMode = section.FetchOrDefault(0, "PREGAPMODE");
					session.PregapSubcode = section.FetchOrDefault(0, "PREGAPSUBC");
				}
				else if (section.Name.StartsWithOrdinal("ENTRY"))
				{
					int entryNum = int.Parse(section.Name.Split(' ')[1]);
					CCDTocEntry entry = new CCDTocEntry(entryNum);
					ccdf.TOCEntries.Add(entry);
					
					entry.Session = section.FetchOrFail("SESSION");
					entry.Point = section.FetchOrFail("POINT");
					entry.ADR = section.FetchOrFail("ADR");
					entry.Control = section.FetchOrFail("CONTROL");
					entry.TrackNo = section.FetchOrFail("TRACKNO");
					entry.AMin = section.FetchOrFail("AMIN");
					entry.ASec = section.FetchOrFail("ASEC");
					entry.AFrame = section.FetchOrFail("AFRAME");
					entry.ALBA = section.FetchOrFail("ALBA");
					entry.Zero = section.FetchOrFail("ZERO");
					entry.PMin = section.FetchOrFail("PMIN");
					entry.PSec = section.FetchOrFail("PSEC");
					entry.PFrame = section.FetchOrFail("PFRAME");
					entry.PLBA = section.FetchOrFail("PLBA");

					//note: LBA 0 is Ansolute MSF 00:02:00
					if (new Timestamp(entry.AMin, entry.ASec, entry.AFrame).Sector != entry.ALBA + 150)
						throw new CCDParseException("Warning: inconsistency in CCD ALBA vs computed A MSF");
					if (new Timestamp(entry.PMin, entry.PSec, entry.PFrame).Sector != entry.PLBA + 150)
						throw new CCDParseException("Warning: inconsistency in CCD PLBA vs computed P MSF");
				}
				else if (section.Name.StartsWithOrdinal("TRACK"))
				{
					int entryNum = int.Parse(section.Name.Split(' ')[1]);
					CCDTrack track = new CCDTrack(entryNum);
					ccdf.Tracks.Add(track);
					ccdf.TracksByNumber[entryNum] = track;
					foreach (var (k, v) in section)
					{
						if (k == "MODE") track.Mode = v;
						else if (k.StartsWithOrdinal("INDEX")) track.Indexes[int.Parse(k.Split(' ')[1])] = v;
					}
				}
			}

			return ccdf;
		}

		public class LoadResults
		{
			public List<RawTOCEntry> RawTOCEntries;
			public CCDFile ParsedCCDFile;
			public bool Valid;
			public CCDParseException FailureException;
			public string ImgPath;
			public string SubPath;
			public string CcdPath;
		}

		public static LoadResults LoadCCDPath(string path)
		{
			LoadResults ret = new LoadResults
			{
				CcdPath = path,
				ImgPath = Path.ChangeExtension(path, ".img"),
				SubPath = Path.ChangeExtension(path, ".sub")
			};
			try
			{
				if (!File.Exists(path)) throw new CCDParseException("Malformed CCD format: nonexistent CCD file!");

				CCDFile ccdf;
				using (FileStream infCCD = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
					ccdf = ParseFrom(infCCD);

				ret.ParsedCCDFile = ccdf;

				ret.Valid = true;
			}
			catch (CCDParseException ex)
			{
				ret.FailureException = ex;
			}

			return ret;
		}

		public static void Dump(Disc disc, string path)
		{
			using (StreamWriter sw = new StreamWriter(path))
			{
				//NOTE: IsoBuster requires the A0,A1,A2 RawTocEntries to be first or else it can't do anything with the tracks
				//if we ever get them in a different order, we'll have to re-order them here

				sw.WriteLine("[CloneCD]");
				sw.WriteLine("Version=3");
				sw.WriteLine();
				sw.WriteLine("[Disc]");
				sw.WriteLine("TocEntries={0}", disc.Sessions.Sum(s => s?.RawTOCEntries.Count ?? 0));
				sw.WriteLine("Sessions={0}", disc.Sessions.Count - 1);
				sw.WriteLine("DataTracksScrambled=0");
				sw.WriteLine("CDTextLength=0"); //not supported anyway
				sw.WriteLine();
				for (int i = 1; i < disc.Sessions.Count; i++)
				{
					var session = disc.Sessions[i];
					
					sw.WriteLine("[Session {0}]", i);
					sw.WriteLine("PreGapMode=2");
					sw.WriteLine("PreGapSubC=1");
					sw.WriteLine();
					
					for (int j = 0; j < session.RawTOCEntries.Count; j++)
					{
						var entry = session.RawTOCEntries[j];

						//ehhh something's wrong with how I track these
						int point = entry.QData.q_index.DecimalValue;
						if (point == 100) point = 0xA0;
						if (point == 101) point = 0xA1;
						if (point == 102) point = 0xA2;

						sw.WriteLine("[Entry {0}]", j);
						sw.WriteLine("Session={0}", i);
						sw.WriteLine("Point=0x{0:x2}", point);
						sw.WriteLine("ADR=0x{0:x2}", entry.QData.ADR);
						sw.WriteLine("Control=0x{0:x2}", (int)entry.QData.CONTROL);
						sw.WriteLine("TrackNo={0}", entry.QData.q_tno.DecimalValue);
						sw.WriteLine("AMin={0}", entry.QData.min.DecimalValue);
						sw.WriteLine("ASec={0}", entry.QData.sec.DecimalValue);
						sw.WriteLine("AFrame={0}", entry.QData.frame.DecimalValue);
						sw.WriteLine("ALBA={0}", entry.QData.Timestamp - 150); //remember to adapt the absolute MSF to an LBA (this field is redundant...)
						sw.WriteLine("Zero={0}", entry.QData.zero);
						sw.WriteLine("PMin={0}", entry.QData.ap_min.DecimalValue);
						sw.WriteLine("PSec={0}", entry.QData.ap_sec.DecimalValue);
						sw.WriteLine("PFrame={0}", entry.QData.ap_frame.DecimalValue);
						sw.WriteLine("PLBA={0}", entry.QData.AP_Timestamp - 150); //remember to adapt the absolute MSF to an LBA (this field is redundant...)
						sw.WriteLine();
					}
					
					//this is nonsense, really. the whole CCD track list shouldn't be needed.
					//but in order to make a high quality CCD which can be inspected by various other tools, we need it
					//now, regarding the indexes.. theyre truly useless. having indexes written out with the tracks is bad news.
					//index information is only truly stored in subQ
					for (int tnum = 1; tnum <= session.InformationTrackCount; tnum++)
					{
						var track = session.Tracks[tnum];
						sw.WriteLine("[TRACK {0}]", track.Number);
						sw.WriteLine("MODE={0}", track.Mode);
						//indexes are BS, don't write them. but we certainly need an index 1
						sw.WriteLine("INDEX 1={0}", track.LBA);
						sw.WriteLine();
					}
				}
			}

			//TODO - actually re-add
			//dump the img and sub
			//TODO - acquire disk size first
			string imgPath = Path.ChangeExtension(path, ".img");
			string subPath = Path.ChangeExtension(path, ".sub");
			byte[] buf2448 = new byte[2448];
			DiscSectorReader dsr = new DiscSectorReader(disc);

			using var imgFile = File.OpenWrite(imgPath);
			using var subFile = File.OpenWrite(subPath);
			int nLBA = disc.Sessions[disc.Sessions.Count - 1].LeadoutLBA;
			for (int lba = 0; lba < nLBA; lba++)
			{
				dsr.ReadLBA_2448(lba, buf2448, 0);
				imgFile.Write(buf2448, 0, 2352);
				subFile.Write(buf2448, 2352, 96);
			}
		}

		private class SS_CCD : ISectorSynthJob2448
		{
			public void Synth(SectorSynthJob job)
			{
				//CCD is always containing everything we'd need (unless a .sub is missing?) so don't worry about flags
				IBlob imgBlob = (IBlob) job.Disc.DisposableResources[0];
				IBlob subBlob = (IBlob) job.Disc.DisposableResources[1];
				//Read_2442(job.LBA, job.DestBuffer2448, job.DestOffset);
				
				//read the IMG data if needed
				if ((job.Parts & ESectorSynthPart.UserAny) != 0)
				{
					long ofs = job.LBA * 2352;
					imgBlob.Read(ofs, job.DestBuffer2448, 0, 2352);
				}

				//if subcode is needed, read it
				if ((job.Parts & (ESectorSynthPart.SubcodeAny)) != 0)
				{
					long ofs = job.LBA * 96;
					subBlob.Read(ofs, job.DestBuffer2448, 2352, 96);

					//subcode comes to us deinterleved; we may still need to interleave it
					if ((job.Parts & (ESectorSynthPart.SubcodeDeinterleave)) == 0)
					{
						SynthUtils.InterleaveSubcodeInplace(job.DestBuffer2448, job.DestOffset + 2352);
					}
				}
			}
		}

		/// <exception cref="CCDParseException">file <paramref name="ccdPath"/> not found, nonexistent IMG file, nonexistent SUB file, IMG or SUB file not multiple of <c>2352 B</c>, or IMG and SUB files differ in length</exception>
		public static Disc LoadCCDToDisc(string ccdPath, DiscMountPolicy IN_DiscMountPolicy)
		{
			var loadResults = LoadCCDPath(ccdPath);
			if (!loadResults.Valid)
				throw loadResults.FailureException;

			Disc disc = new Disc();

			IBlob imgBlob = null;
			long imgLen = -1;

			//mount the IMG file
			//first check for a .ecm in place of the img
			string imgPath = loadResults.ImgPath;
			if (!File.Exists(imgPath))
			{
				string ecmPath = Path.ChangeExtension(imgPath, ".img.ecm");
				if (File.Exists(ecmPath))
				{
					if (Blob_ECM.IsECM(ecmPath))
					{
						Blob_ECM ecm = new Blob_ECM();
						ecm.Load(ecmPath);
						imgBlob = ecm;
						imgLen = ecm.Length;
					}
				}
			}
			if (imgBlob == null)
			{
				if (!File.Exists(loadResults.ImgPath)) throw new CCDParseException("Malformed CCD format: nonexistent IMG file!");
				Blob_RawFile imgFile = new Blob_RawFile() { PhysicalPath = loadResults.ImgPath };
				imgLen = imgFile.Length;
				imgBlob = imgFile;
			}
			disc.DisposableResources.Add(imgBlob);

			//mount the SUB file
			if (!File.Exists(loadResults.SubPath)) throw new CCDParseException("Malformed CCD format: nonexistent SUB file!");
			Blob_RawFile subFile = new Blob_RawFile { PhysicalPath = loadResults.SubPath };
			disc.DisposableResources.Add(subFile);
			long subLen = subFile.Length;

			//quick integrity check of file sizes
			if (imgLen % 2352 != 0) throw new CCDParseException("Malformed CCD format: IMG file length not multiple of 2352");
			int NumImgSectors = (int)(imgLen / 2352);
			if (subLen != NumImgSectors * 96) throw new CCDParseException("Malformed CCD format: SUB file length not matching IMG");

			var ccdf = loadResults.ParsedCCDFile;

			//the only instance of a sector synthesizer we'll need
			SS_CCD synth = new SS_CCD();

			// create the initial session
			int curSession = 1;
			disc.Sessions.Add(new() { Number = curSession });

			//generate DiscTOCRaw items from the ones specified in the CCD file
			//TODO - range validate these (too many truncations to byte)
			foreach (var entry in ccdf.TOCEntries.OrderBy(te => te.Session))
			{
				if (entry.Session != curSession)
				{
					if (entry.Session != curSession + 1)
						throw new CCDParseException("Malformed CCD format: Session incremented more than one");
					curSession = entry.Session;
					disc.Sessions.Add(new() { Number = curSession });
				}

				//this should actually be zero. im not sure if this is stored as BCD2 or not
				BCD2 tno = BCD2.FromDecimal(entry.TrackNo);

				//these are special values.. I think, taken from this:
				//http://www.staff.uni-mainz.de/tacke/scsi/SCSI2-14.html
				//the CCD will contain Points as decimal values except for these specially converted decimal values which should stay as BCD.
				//Why couldn't they all be BCD? I don't know. I guess because BCD is inconvenient, but only A0 and friends have special meaning. It's confusing.
				BCD2 ino = BCD2.FromDecimal(entry.Point);
				ino.BCDValue = entry.Point switch
				{
					0xA0 or 0xA1 or 0xA2 => (byte)entry.Point,
					_ => ino.BCDValue
				};

				SubchannelQ q = new SubchannelQ
				{
					q_status = SubchannelQ.ComputeStatus(entry.ADR, (EControlQ)(entry.Control & 0xF)),
					q_tno = tno,
					q_index = ino,
					min = BCD2.FromDecimal(entry.AMin),
					sec = BCD2.FromDecimal(entry.ASec),
					frame = BCD2.FromDecimal(entry.AFrame),
					zero = (byte)entry.Zero,
					ap_min = BCD2.FromDecimal(entry.PMin),
					ap_sec = BCD2.FromDecimal(entry.PSec),
					ap_frame = BCD2.FromDecimal(entry.PFrame),
					q_crc = 0, //meaningless
				};

				disc.Sessions[curSession].RawTOCEntries.Add(new() { QData = q });
			}

			//analyze the RAWTocEntries to figure out what type of track track 1 is
			Synthesize_DiscTOC_From_RawTOCEntries_Job tocSynth = new Synthesize_DiscTOC_From_RawTOCEntries_Job(disc.Session1.RawTOCEntries);
			tocSynth.Run();
			
			//Add sectors for the mandatory track 1 pregap, which isn't stored in the CCD file
			//We reuse some CUE code for this.
			//If we load other formats later we might should abstract this even further (to a synthesizer job)
			//It can't really be abstracted from cue files though due to the necessity of merging this with other track 1 pregaps
			var pregapTrackType = CUE.CueTrackType.Audio;
			if (tocSynth.Result.TOCItems[1].IsData)
			{
				pregapTrackType = tocSynth.Result.SessionFormat switch
				{
					SessionFormat.Type20_CDXA => CUE.CueTrackType.Mode2_2352,
					SessionFormat.Type10_CDI => CUE.CueTrackType.CDI_2352,
					SessionFormat.Type00_CDROM_CDDA => CUE.CueTrackType.Mode1_2352,
					_ => pregapTrackType
				};
			}

			for (int i = 0; i < 150; i++)
			{
				CUE.SS_Gap ss_gap = new CUE.SS_Gap()
				{
					Policy = IN_DiscMountPolicy,
					TrackType = pregapTrackType
				};
				disc._Sectors.Add(ss_gap);

				int qRelMSF = i - 150;

				//tweak relMSF due to ambiguity/contradiction in yellowbook docs
				if (!IN_DiscMountPolicy.CUE_PregapContradictionModeA)
					qRelMSF++;

				//setup subQ
				const byte ADR = 1; //absent some kind of policy for how to set it, this is a safe assumption:
				ss_gap.sq.SetStatus(ADR, tocSynth.Result.TOCItems[1].Control);
				ss_gap.sq.q_tno = BCD2.FromDecimal(1);
				ss_gap.sq.q_index = BCD2.FromDecimal(0);
				ss_gap.sq.AP_Timestamp = i;
				ss_gap.sq.Timestamp = qRelMSF;

				//setup subP
				ss_gap.Pause = true;
			}

			//build the sectors:
			//set up as many sectors as we have img/sub for, even if the TOC doesnt reference them
			//(the TOC is unreliable, and the Track records are redundant)
			for (int i = 0; i < NumImgSectors; i++)
			{
				disc._Sectors.Add(synth);
			}

			return disc;
		}
	}
}
