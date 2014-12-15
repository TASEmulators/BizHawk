using System;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

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
			public List<CCDSession> Sessions = new List<CCDSession>();

			/// <summary>
			/// The [Entry] sctions
			/// </summary>
			public List<CCDTocEntry> TOCEntries = new List<CCDTocEntry>();

			/// <summary>
			/// The [TRACK] sections
			/// </summary>
			public List<CCDTrack> Tracks = new List<CCDTrack>();

			/// <summary>
			/// The [TRACK] sections, indexed by number
			/// </summary>
			public Dictionary<int, CCDTrack> TracksByNumber = new Dictionary<int, CCDTrack>();
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
			/// the CCD specifies this, but it isnt in the actual disc data as such, it is encoded some other (likely difficult to extract) way and thats why CCD puts it here
			/// </summary>
			public int Session;

			/// <summary>
			/// this seems just to be the LBA corresponding to AMIN:ASEC:AFRAME. It's not stored on the disc, and it's redundant.
			/// </summary>
			public int ALBA;

			/// <summary>
			/// this seems just to be the LBA corresponding to PMIN:PSEC:PFRAME. It's not stored on the disc, and it's redundant.
			/// </summary>
			public int PLBA;

			//these correspond pretty directly to values in the Q subchannel fields
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
			/// The specified data mode
			/// </summary>
			public int Mode;

			/// <summary>
			/// The indexes specified for the track (these are 0-indexed)
			/// </summary>
			public Dictionary<int, int> Indexes = new Dictionary<int, int>();
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

		class CCDSection : Dictionary<string,int>
		{
			public string Name;
			public int FetchOrDefault(int def, string key)
			{
				TryGetValue(key, out def);
				return def;
			}
			public int FetchOrFail(string key)
			{
				int ret;
				if(!TryGetValue(key, out ret))
					throw new CCDParseException("Malformed or unexpected CCD format: missing required [Entry] key: " + key);
				return ret;
			}
		}

		List<CCDSection> ParseSections(Stream stream)
		{
			List<CCDSection> sections = new List<CCDSection>();

			//TODO - do we need to attempt to parse out the version tag in a first pass?
			//im doing this from a version 3 example

			StreamReader sr = new StreamReader(stream);
			CCDSection currSection = null;
			for (; ; )
			{
				var line = sr.ReadLine();
				if (line == null) break;
				if (line == "") continue;
				if (line.StartsWith("["))
				{
					currSection = new CCDSection();
					currSection.Name = line.Trim('[', ']').ToUpper();
					sections.Add(currSection);
				}
				else
				{
					var parts = line.Split('=');
					if (parts.Length != 2)
						throw new CCDParseException("Malformed or unexpected CCD format: parsing item into two parts");
					int val;
					if (parts[1].StartsWith("0x") || parts[1].StartsWith("0X"))
						val = int.Parse(parts[1].Substring(2), NumberStyles.HexNumber);
					else val = int.Parse(parts[1]);
					currSection[parts[0].ToUpper()] = val;
				}
			} //loop until lines exhausted

			return sections;
		}

		int PreParseIntegrityCheck(List<CCDSection> sections)
		{
			if (sections.Count == 0) throw new CCDParseException("Malformed CCD format: no sections");

			//we need at least a CloneCD and Disc section
			if (sections.Count < 2) throw new CCDParseException("Malformed CCD format: insufficient sections");

			var ccdSection = sections[0];
			if (ccdSection.Name != "CLONECD")
				throw new CCDParseException("Malformed CCD format: confusing first section name");

			if (!ccdSection.ContainsKey("VERSION"))
				throw new CCDParseException("Malformed CCD format: missing version in CloneCD section");

			if(sections[1].Name != "DISC")
				throw new CCDParseException("Malformed CCD format: section[1] isn't [Disc]");

			int version = ccdSection["VERSION"];

			return version;
		}

		/// <summary>
		/// Parses a CCD file contained in the provided stream
		/// </summary>
		public CCDFile ParseFrom(Stream stream)
		{
			CCDFile ccdf = new CCDFile();

			var sections = ParseSections(stream);
			ccdf.Version = PreParseIntegrityCheck(sections);

			var discSection = sections[1];
			int nTocEntries = discSection["TOCENTRIES"]; //its conceivable that this could be missing
			int nSessions = discSection["SESSIONS"]; //its conceivable that this could be missing
			ccdf.DataTracksScrambled = discSection.FetchOrDefault(0, "DATATRACKSSCRAMBLED");
			ccdf.CDTextLength = discSection.FetchOrDefault(0, "CDTEXTLENGTH");

			if (ccdf.DataTracksScrambled==1) throw new CCDParseException("Malformed CCD format: DataTracksScrambled=1 not supported. Please report this, so we can understand what it means.");

			for (int i = 2; i < sections.Count; i++)
			{
				var section = sections[i];
				if (section.Name.StartsWith("SESSION"))
				{
					int sesnum = int.Parse(section.Name.Split(' ')[1]);
					CCDSession session = new CCDSession(sesnum);
					ccdf.Sessions.Add(session);
					if (sesnum != ccdf.Sessions.Count)
						throw new CCDParseException("Malformed CCD format: wrong session number in sequence");
					session.PregapMode = section.FetchOrDefault(0, "PREGAPMODE");
					session.PregapSubcode = section.FetchOrDefault(0, "PREGAPSUBC");
				}
				else if (section.Name.StartsWith("ENTRY"))
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

					if (new Timestamp(entry.AMin, entry.ASec, entry.AFrame).Sector != entry.ALBA + 150)
						throw new CCDParseException("Warning: inconsistency in CCD ALBA vs computed A MSF");
					if (new Timestamp(entry.PMin, entry.PSec, entry.PFrame).Sector != entry.PLBA + 150)
						throw new CCDParseException("Warning: inconsistency in CCD PLBA vs computed P MSF");

					if(entry.Session != 1)
						throw new CCDParseException("Malformed CCD format: not yet supporting multi-session files"); 
				}
				else if (section.Name.StartsWith("TRACK"))
				{
					int entryNum = int.Parse(section.Name.Split(' ')[1]);
					CCDTrack track = new CCDTrack(entryNum);
					ccdf.Tracks.Add(track);
					ccdf.TracksByNumber[entryNum] = track;
					foreach (var kvp in section)
					{
						if (kvp.Key == "MODE")
							track.Mode = kvp.Value;
						if (kvp.Key.StartsWith("INDEX"))
						{
							int inum = int.Parse(kvp.Key.Split(' ')[1]);
							track.Indexes[inum] = kvp.Value;
						}
					}
				}
			} //sections loop

			return ccdf;
		}

		public class LoadResults
		{
			public List<RawTOCEntry> RawTOCEntries;
			public CCDFile ParsedCCDFile;
			public bool Valid;
			public Exception FailureException;
			public string ImgPath;
			public string SubPath;
			public string CcdPath;
			public int NumImgSectors;
		}

		public static LoadResults LoadCCDPath(string path)
		{
			LoadResults ret = new LoadResults();
			ret.CcdPath = path;
			ret.ImgPath = Path.ChangeExtension(path, ".img");
			ret.SubPath = Path.ChangeExtension(path, ".sub");
			try
			{
				if(!File.Exists(path)) throw new CCDParseException("Malformed CCD format: nonexistent CCD file!");
				if (!File.Exists(ret.ImgPath)) throw new CCDParseException("Malformed CCD format: nonexistent IMG file!");
				if (!File.Exists(ret.SubPath)) throw new CCDParseException("Malformed CCD format: nonexistent SUB file!");

				//quick check of .img and .sub sizes
				long imgLen = new FileInfo(ret.ImgPath).Length;
				long subLen = new FileInfo(ret.SubPath).Length;
				if(imgLen % 2352 != 0) throw new CCDParseException("Malformed CCD format: IMG file length not multiple of 2352");
				ret.NumImgSectors = (int)(imgLen / 2352);
				if (subLen != ret.NumImgSectors * 96) throw new CCDParseException("Malformed CCD format: SUB file length not matching IMG");
				
				CCDFile ccdf;
				using (var infCCD = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
					ccdf = new CCD_Format().ParseFrom(infCCD);

				ret.ParsedCCDFile = ccdf;

				ret.Valid = true;
			}
			catch (CCDParseException ex)
			{
				ret.FailureException = ex;
			}

			return ret;
		}

		/// <summary>
		/// Loads a CCD at the specified path to a Disc object
		/// </summary>
		public Disc LoadCCDToDisc(string ccdPath)
		{
			var loadResults = LoadCCDPath(ccdPath);
			if (!loadResults.Valid)
				throw loadResults.FailureException;

			Disc disc = new Disc();

			var ccdf = loadResults.ParsedCCDFile;
			var imgBlob = new Disc.Blob_RawFile() { PhysicalPath = loadResults.ImgPath };
			var subBlob = new Disc.Blob_RawFile() { PhysicalPath = loadResults.SubPath };
			disc.Blobs.Add(imgBlob);
			disc.Blobs.Add(subBlob);

			//generate DiscTOCRaw items from the ones specified in the CCD file
			//TODO - range validate these (too many truncations to byte)
			disc.RawTOCEntries = new List<RawTOCEntry>();
			BufferedSubcodeSector bss = new BufferedSubcodeSector();
			foreach (var entry in ccdf.TOCEntries)
			{
				var q = new SubchannelQ
				{
					q_status = SubchannelQ.ComputeStatus(entry.ADR, (EControlQ)(entry.Control & 0xF)),
					q_tno = (byte)entry.TrackNo,
					q_index = (byte)entry.Point,
					min = BCD2.FromDecimal(entry.AMin),
					sec = BCD2.FromDecimal(entry.ASec),
					frame = BCD2.FromDecimal(entry.AFrame),
					zero = (byte)entry.Zero,
					ap_min = BCD2.FromDecimal(entry.PMin),
					ap_sec = BCD2.FromDecimal(entry.PSec),
					ap_frame = BCD2.FromDecimal(entry.PFrame),
				};

				//CRC cant be calculated til we've got all the fields setup
				q.q_crc = bss.Synthesize_SubchannelQ(ref q, true);

				disc.RawTOCEntries.Add(new RawTOCEntry { QData = q });
			}

			//generate the toc from the entries
			var tocSynth = new DiscTOCRaw.SynthesizeFromRawTOCEntriesJob() { Entries = disc.RawTOCEntries };
			tocSynth.Run();
			disc.TOCRaw = tocSynth.Result;

			//synthesize DiscStructure
			var structureSynth = new DiscStructure.SynthesizeFromDiscTOCRawJob() { TOCRaw = disc.TOCRaw };
			structureSynth.Run();
			disc.Structure = structureSynth.Result;

			//I *think* implicitly there is an index 0.. at.. i dunno, 0 maybe, for track 1
			{
				var dsi0 = new DiscStructure.Index();
				dsi0.LBA = 0;
				dsi0.Number = 0;
				disc.Structure.Sessions[0].Tracks[0].Indexes.Add(dsi0);
			}

			//now, how to get the track types for the DiscStructure?
			//1. the CCD tells us (somehow the reader has judged)
			//2. scan it out of the Q subchannel
			//lets choose1.
			//TODO - better consider how to handle the situation where we have havent received all the [TRACK] items we need
			foreach (var st in disc.Structure.Sessions[0].Tracks)
			{
				var ccdt = ccdf.TracksByNumber[st.Number];
				switch (ccdt.Mode)
				{
					case 0:
						st.TrackType = ETrackType.Audio; //for CCD, this means audio, apparently.
						break;
					case 1:
						st.TrackType = ETrackType.Mode1_2352;
						break;
					case 2:
						st.TrackType = ETrackType.Mode2_2352;
						break;
					default:
						throw new InvalidOperationException("Unsupported CCD mode");
				}

				//add indexes for this track
				foreach (var ccdi in ccdt.Indexes)
				{
					var dsi = new DiscStructure.Index();
					//if (ccdi.Key == 0) continue;
					dsi.LBA = ccdi.Value;
					dsi.Number = ccdi.Key;
					st.Indexes.Add(dsi);
				}
			}

			//add sectors for the lead-in, which isn't stored in the CCD file, I think
			//TODO - synthesize lead-in sectors from TOC, if the lead-in isn't available.
			//need a test case for that though.
			var leadin_sector_zero = new Sector_Zero();
			var leadin_subcode_zero = new ZeroSubcodeSector();
			for (int i = 0; i < 150; i++)
			{
				var se = new SectorEntry(leadin_sector_zero);
				disc.Sectors.Add(se);
				se.SubcodeSector = leadin_subcode_zero;
			}

			//build the sectors:
			//set up as many sectors as we have img/sub for, even if the TOC doesnt reference them (TOC is unreliable, although the tracks should have covered it all)
			for (int i = 0; i < loadResults.NumImgSectors; i++)
			{
				var isec = new Sector_RawBlob();
				isec.Offset = ((long)i) * 2352;
				isec.Blob = imgBlob;

				var se = new SectorEntry(isec);
				disc.Sectors.Add(se);

				var scsec = new BlobSubcodeSectorPreDeinterleaved();
				scsec.Offset = ((long)i) * 96;
				scsec.Blob = subBlob;
				se.SubcodeSector = scsec;
			}

			return disc;
		}

		public void Dump(Disc disc, string ccdPath)
		{
			//TODO!!!!!!!

			StringWriter sw = new StringWriter();
			sw.WriteLine("[CloneCD]");
			sw.WriteLine("Version=3");
			sw.WriteLine("[Disc]");
			//sw.WriteLine("TocEntries={0}",disc.TOCRaw.TOCItems
			sw.WriteLine("Sessions=1");
			sw.WriteLine("DataTracksScrambled=0");
			sw.WriteLine("CDTextLength=0");
		}

	} //class CCD_Format
}


