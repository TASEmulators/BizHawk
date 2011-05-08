using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Disc
{
	partial class Disc
	{
		void FromCuePathInternal(string cuePath)
		{
			string cueDir = Path.GetDirectoryName(cuePath);
			var cue = new Cue();
			cue.LoadFromPath(cuePath);

			var session = new DiscTOC.Session();
			session.num = 1;
			TOC.Sessions.Add(session);

			int track_counter = 1;
			int curr_lba = 0;
			foreach (var cue_file in cue.Files)
			{
				//make a raw file blob for the source binfile
				Blob_RawFile blob = new Blob_RawFile();
				blob.PhysicalPath = Path.Combine(cueDir, cue_file.Path);
				Blobs.Add(blob);

				//structural validation
				if (cue_file.Tracks.Count < 1) throw new Cue.CueBrokenException("Found a cue file with no tracks");

				//structural validation: every track must be the same type with cue/bin (i think)
				var trackType = cue_file.Tracks[0].TrackType;
				for (int i = 1; i < cue_file.Tracks.Count; i++)
				{
					if (cue_file.Tracks[i].TrackType != trackType) throw new Cue.CueBrokenException("cue has different track types per datafile (not supported now; maybe never)");
				}

				//structural validaton: make sure file is a correct size and analyze its length
				long flen = new FileInfo(blob.PhysicalPath).Length;
				int numlba;
				int leftover = 0;
				switch (trackType)
				{
					case Cue.ECueTrackType.Audio:
						numlba = (int)(flen / 2352);
						leftover = (int)(flen - numlba * 2352);
						break;

					case Cue.ECueTrackType.Mode1_2352:
					case Cue.ECueTrackType.Mode2_2352:
						if (flen % 2352 != 0) throw new Cue.CueBrokenException("Found a modeN_2352 cue file that is wrongly-sized");
						numlba = (int)(flen / 2352);
						break;
					case Cue.ECueTrackType.Mode1_2048:
						if (flen % 2048 != 0) throw new Cue.CueBrokenException("Found a modeN_2048 cue file that is wrongly-sized");
						numlba = (int)(flen / 2048);
						break;

					default: throw new InvalidOperationException();
				}

				List<DiscTOC.Track> new_toc_tracks = new List<DiscTOC.Track>();
				for (int i = 0; i < cue_file.Tracks.Count; i++)
				{
					bool last_track = (i == cue_file.Tracks.Count - 1);

					var cue_track = cue_file.Tracks[i];
					if (cue_track.TrackNum != track_counter) throw new Cue.CueBrokenException("Missing a track in the middle of the cue");
					track_counter++;

					DiscTOC.Track toc_track = new DiscTOC.Track();
					toc_track.num = track_counter;
					session.Tracks.Add(toc_track);
					new_toc_tracks.Add(toc_track);

					//analyze indices
					int idx;
					for (idx = 0; idx <= 99; idx++)
					{
						if (!cue_track.Indexes.ContainsKey(idx))
						{
							if (idx == 0) continue;
							if (idx == 1) throw new Cue.CueBrokenException("cue track is missing an index 1");
							break;
						}
						var cue_index = cue_track.Indexes[idx];
						//todo - add pregap/postgap from cue?

						DiscTOC.Index toc_index = new DiscTOC.Index();
						toc_index.num = idx;
						toc_track.Indexes.Add(toc_index);
						toc_index.lba = cue_index.Timestamp.LBA + curr_lba;
					}

					//look for extra indices (i.e. gaps)
					for (; idx <= 99; idx++)
					{
						if (cue_track.Indexes.ContainsKey(idx))
							throw new Cue.CueBrokenException("cue track is has an index gap");
					}
				} //track loop

				//analyze length of each track and index
				DiscTOC.Index last_toc_index = null;
				foreach (var toc_track in new_toc_tracks)
				{
					foreach (var toc_index in toc_track.Indexes)
					{
						if (last_toc_index != null)
							last_toc_index.length_lba = toc_index.lba - last_toc_index.lba;
						last_toc_index = toc_index;
					}
				}
				if (last_toc_index != null)
					last_toc_index.length_lba = (curr_lba + numlba) - last_toc_index.lba;

				//generate the sectors from this file
				long curr_src_addr = 0;
				for (int i = 0; i < numlba; i++)
				{
					ISector sector;
					switch (trackType)
					{
						case Cue.ECueTrackType.Audio:
						//all 2352 bytes are present
						case Cue.ECueTrackType.Mode1_2352:
						//2352 bytes are present, containing 2048 bytes of user data as well as ECM
						case Cue.ECueTrackType.Mode2_2352:
							//2352 bytes are present, containing 2336 bytes of user data, replacing ECM
							{
								Sector_RawBlob sector_rawblob = new Sector_RawBlob();
								sector_rawblob.Blob = blob;
								sector_rawblob.Offset = curr_src_addr;
								curr_src_addr += 2352;
								Sector_Raw sector_raw = new Sector_Raw();
								sector_raw.BaseSector = sector_rawblob;
								if (i == numlba - 1 && leftover != 0)
								{
									Sector_ZeroPad sector_zeropad = new Sector_ZeroPad();
									sector_zeropad.BaseSector = sector_rawblob;
									sector_zeropad.BaseLength = 2352 - leftover;
									sector_raw.BaseSector = sector_zeropad;
								}
								sector = sector_raw;
								break;
							}
						case Cue.ECueTrackType.Mode1_2048:
							//2048 bytes are present. ECM needs to be generated to create a full sector
							{
								var sector_2048 = new Sector_Mode1_2048(i + 150);
								sector_2048.Blob = new ECMCacheBlob(blob);
								sector_2048.Offset = curr_src_addr;
								curr_src_addr += 2048;
								sector = sector_2048;
								break;
							}
						default: throw new InvalidOperationException();
					}
					SectorEntry se = new SectorEntry();
					se.Sector = sector;
					Sectors.Add(se);
				}

				curr_lba += numlba;

			} //done analyzing cue datafiles

			TOC.AnalyzeLengthsFromIndexLengths();
		}
	}

	public class Cue
	{
		//TODO - export from isobuster and observe the SESSION directive, as well as the MSF directive.

		public string DebugPrint()
		{
			StringBuilder sb = new StringBuilder();
			foreach (CueFile cf in Files)
			{
				sb.AppendFormat("FILE \"{0}\"", cf.Path);
				if (cf.Binary) sb.Append(" BINARY");
				sb.AppendLine();
				foreach (CueTrack ct in cf.Tracks)
				{
					sb.AppendFormat("  TRACK {0:D2} {1}\n", ct.TrackNum, ct.TrackType.ToString().Replace("_", "/").ToUpper());
					foreach (CueTrackIndex cti in ct.Indexes.Values)
					{
						sb.AppendFormat("    INDEX {0:D2} {1}\n", cti.IndexNum, cti.Timestamp.Value);
					}
				}
			}

			return sb.ToString();
		}

		public class CueFile
		{
			public string Path;
			public bool Binary;
			public List<CueTrack> Tracks = new List<CueTrack>();
		}

		public List<CueFile> Files = new List<CueFile>();

		public enum ECueTrackType
		{
			Mode1_2352,
			Mode1_2048,
			Mode2_2352,
			Audio
		}

		public class CueTrack
		{
			public ECueTrackType TrackType;
			public int TrackNum;
			public Dictionary<int, CueTrackIndex> Indexes = new Dictionary<int, CueTrackIndex>();
		}

		public class CueTimestamp
		{
			public CueTimestamp(string value) { 
				this.Value = value;
				MIN = int.Parse(value.Substring(0, 2));
				SEC = int.Parse(value.Substring(3, 2));
				FRAC = int.Parse(value.Substring(6, 2));
				LBA = MIN * 60 * 75 + SEC * 75 + FRAC;
			}
			public readonly string Value;
			public readonly int MIN, SEC, FRAC, LBA;
		}

		public class CueTrackIndex
		{
			public int IndexNum;
			public CueTimestamp Timestamp;
		}

		public class CueBrokenException : Exception
		{
			public CueBrokenException(string why)
				: base(why)
			{
			}
		}

		public void LoadFromPath(string cuePath)
		{
			FileInfo fiCue = new FileInfo(cuePath);
			if (!fiCue.Exists) throw new FileNotFoundException();
			File.ReadAllText(cuePath);
			TextReader tr = new StreamReader(cuePath);

			CueFile currFile = null;
			CueTrack currTrack = null;
			int state = 0;
			for (; ; )
			{
				string line = tr.ReadLine();
				if (line == null) break;
				if (line == "") continue;
				line = line.Trim();
				var clp = new CueLineParser(line);

				string key = clp.ReadToken().ToUpper();
				switch (key)
				{
					case "REM":
						break;

					case "FILE":
						{
							currTrack = null;
							currFile = new CueFile();
							Files.Add(currFile);
							currFile.Path = clp.ReadPath().Trim('"');
							if (!clp.EOF)
							{
								string temp = clp.ReadToken().ToUpper();
								if (temp == "BINARY")
									currFile.Binary = true;
							}
							break;
						}
					case "TRACK":
						{
							if (currFile == null) throw new CueBrokenException("invalid cue structure");
							if (clp.EOF) throw new CueBrokenException("invalid cue structure");
							string strtracknum = clp.ReadToken();
							int tracknum;
							if (!int.TryParse(strtracknum, out tracknum))
								throw new CueBrokenException("malformed track number");
							if (clp.EOF) throw new CueBrokenException("invalid cue structure");
							string strtracktype = clp.ReadToken().ToUpper();
							currTrack = new CueTrack();
							switch (strtracktype)
							{
								case "MODE1/2352": currTrack.TrackType = ECueTrackType.Mode1_2352; break;
								case "MODE1/2048": currTrack.TrackType = ECueTrackType.Mode1_2048; break;
								case "MODE2/2352": currTrack.TrackType = ECueTrackType.Mode2_2352; break;
								case "AUDIO": currTrack.TrackType = ECueTrackType.Audio; break;
								default:
									throw new CueBrokenException("unhandled track type");
							}
							currTrack.TrackNum = tracknum;
							currFile.Tracks.Add(currTrack);
							break;
						}
					case "INDEX":
						{
							if (currTrack == null) throw new CueBrokenException("invalid cue structure");
							if (clp.EOF) throw new CueBrokenException("invalid cue structure");
							string strindexnum = clp.ReadToken();
							int indexnum;
							if (!int.TryParse(strindexnum, out indexnum))
								throw new CueBrokenException("malformed index number");
							if (clp.EOF) throw new CueBrokenException("invalid cue structure (missing index timestamp)");
							string str_timestamp = clp.ReadToken();
							CueTrackIndex cti = new CueTrackIndex();
							cti.Timestamp = new CueTimestamp(str_timestamp); ;
							cti.IndexNum = indexnum;
							currTrack.Indexes[indexnum] = cti;
							break;
						}
					case "PREGAP":
					case "POSTGAP":
						throw new CueBrokenException("cue postgap/pregap command not supported yet");
					default:
						throw new CueBrokenException("unsupported cue command: " + key);
				}
			}
		}


		class CueLineParser
		{
			int index;
			string str;
			public bool EOF;
			public CueLineParser(string line)
			{
				this.str = line;
			}

			public string ReadPath() { return ReadToken(true); }
			public string ReadToken() { return ReadToken(false); }

			public string ReadToken(bool isPath)
			{
				if (EOF) return null;
				int startIndex = index;
				bool inToken = false;
				bool inQuote = false;
				for (; ; )
				{
					bool done = false;
					char c = str[index];
					bool isWhiteSpace = (c == ' ' || c == '\t');

					if (isWhiteSpace)
					{
						if (inQuote)
							index++;
						else
						{
							if (inToken)
								done = true;
							else
								index++;
						}
					}
					else
					{
						bool startedQuote = false;
						if (!inToken)
						{
							startIndex = index;
							if (isPath && c == '"')
								startedQuote = inQuote = true;
							inToken = true;
						}
						switch (str[index])
						{
							case '"':
								index++;
								if (inQuote && !startedQuote)
								{
									done = true;
								}
								break;
							case '\\':
								index++;
								break;

							default:
								index++;
								break;
						}
					}
					if (index == str.Length)
					{
						EOF = true;
						done = true;
					}
					if (done) break;
				}

				return str.Substring(startIndex, index - startIndex);
			}

		}
	}
}