using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

//this rule is not supported correctly: `The first track number can be greater than one, but all track numbers after the first must be sequential.`

namespace BizHawk.DiscSystem
{
	partial class Disc
	{
		/// <summary>
		/// finds a file in the same directory with an extension alternate to the supplied one.
		/// If two are found, an exception is thrown (later, we may have heuristics to try to acquire the desired content)
		/// </summary>
		string FindAlternateExtensionFile(string path, bool caseSensitive)
		{
			string targetFile = Path.GetFileName(path);
			string targetFragment = Path.GetFileNameWithoutExtension(path);
			var di = new FileInfo(path).Directory;
			var results = new List<FileInfo>();
			foreach (var fi in di.GetFiles())
			{
				string fragment = Path.GetFileNameWithoutExtension(fi.FullName);
				//match files with differing extensions
				int cmp = string.Compare(fragment, targetFragment, !caseSensitive);
				if(cmp != 0)
					//match files with another extension added on (likely to be mygame.bin.ecm)
					cmp = string.Compare(fragment, targetFile, !caseSensitive);
				if (cmp == 0)
					results.Add(fi);

			}
			if(results.Count == 0) throw new DiscReferenceException(path, "Cannot find the specified file");
			if (results.Count > 1) throw new DiscReferenceException(path, "Cannot choose between multiple options");
			return results[0].FullName;
		}

		void FromCuePathInternal(string cuePath, CueBinPrefs prefs)
		{
			string cueDir = Path.GetDirectoryName(cuePath);
			var cue = new Cue();
			cue.LoadFromPath(cuePath);

			var session = new DiscTOC.Session();
			session.num = 1;
			TOC.Sessions.Add(session);
			var pregap_sector = new Sector_Zero();

			int curr_track = 1;

			foreach (var cue_file in cue.Files)
			{
				//structural validation
				if (cue_file.Tracks.Count < 1) throw new Cue.CueBrokenException("`You must specify at least one track per file.`");

				string blobPath = Path.Combine(cueDir, cue_file.Path);

				int blob_sectorsize = Cue.BINSectorSizeForTrackType(cue_file.Tracks[0].TrackType);
				int blob_length_aba, blob_leftover;
				IBlob cue_blob = null;

				//try any way we can to acquire a file
				if (!File.Exists(blobPath) && prefs.ExtensionAware)
				{
					blobPath = FindAlternateExtensionFile(blobPath, prefs.CaseSensitive);
				}

				if (!File.Exists(blobPath))
					throw new DiscReferenceException(blobPath,"");

				//some simple rules to mutate the file type if we received something fishy
				string blobPathExt = Path.GetExtension(blobPath).ToLower();
				if (blobPathExt == ".ape") cue_file.FileType = Cue.CueFileType.Wave;
				if (blobPathExt == ".mp3") cue_file.FileType = Cue.CueFileType.Wave;
				if (blobPathExt == ".mpc") cue_file.FileType = Cue.CueFileType.Wave;
				if (blobPathExt == ".flac") cue_file.FileType = Cue.CueFileType.Wave;
				if (blobPathExt == ".ecm") cue_file.FileType = Cue.CueFileType.ECM;

				if (cue_file.FileType == Cue.CueFileType.Binary || cue_file.FileType == Cue.CueFileType.Unspecified)
				{
					//make a blob for the file
					Blob_RawFile blob = new Blob_RawFile();
					blob.PhysicalPath = blobPath;
					Blobs.Add(blob);

					blob_length_aba = (int)(blob.Length / blob_sectorsize);
					blob_leftover = (int)(blob.Length - blob_length_aba * blob_sectorsize);
					cue_blob = blob;
				}
				else if (cue_file.FileType == Cue.CueFileType.ECM)
				{
					if(!Blob_ECM.IsECM(blobPath))
					{
						throw new DiscReferenceException(blobPath, "an ECM file was specified or detected, but it isn't a valid ECM file. You've got issues. Consult your iso vendor.");
					}
					Blob_ECM blob = new Blob_ECM();
					Blobs.Add(blob);
					blob.Parse(blobPath);
					cue_blob = blob;
					blob_length_aba = (int)(blob.Length / blob_sectorsize);
					blob_leftover = (int)(blob.Length - blob_length_aba * blob_sectorsize);
				}
				else if (cue_file.FileType == Cue.CueFileType.Wave)
				{
					Blob_WaveFile blob = new Blob_WaveFile();
					Blobs.Add(blob);

					try
					{
						//check whether we can load the wav directly
						bool loaded = false;
						if (File.Exists(blobPath) && Path.GetExtension(blobPath).ToUpper() == ".WAV")
						{
							try
							{
								blob.Load(blobPath);
								loaded = true;
							}
							catch
							{
							}
						}

						//if that didnt work or wasnt possible, try loading it through ffmpeg
						if (!loaded)
						{
							FFMpeg ffmpeg = new FFMpeg();
							if (!ffmpeg.QueryServiceAvailable())
							{
								throw new DiscReferenceException(blobPath, "No decoding service was available (make sure ffmpeg.exe is available. even though this may be a wav, ffmpeg is used to load oddly formatted wave files. If you object to this, please send us a note and we'll see what we can do. It shouldn't be too hard.)");
							}
							AudioDecoder dec = new AudioDecoder();
							byte[] buf = dec.AcquireWaveData(blobPath);
							blob.Load(new MemoryStream(buf));
							WasSlowLoad = true;
						}
					}
					catch (Exception ex)
					{
						throw new DiscReferenceException(blobPath, ex);
					}

					blob_length_aba = (int)(blob.Length / blob_sectorsize);
					blob_leftover = (int)(blob.Length - blob_length_aba * blob_sectorsize);
					cue_blob = blob;
				}
				else throw new Exception("Internal error - Unhandled cue blob type");

				//TODO - make CueTimestamp better, and also make it a struct, and also just make it DiscTimestamp

				//start timekeeping for the blob. every time we hit an index, this will advance
				int blob_timestamp = 0;

				//the aba that this cue blob starts on
				int blob_disc_aba_start = Sectors.Count;

				//for each track within the file, create an index 0 if it is missing.
				//also check to make sure there is an index 1
				for (int t = 0; t < cue_file.Tracks.Count; t++)
				{
					var cue_track = cue_file.Tracks[t];
					if (!cue_track.Indexes.ContainsKey(1))
						throw new Cue.CueBrokenException("Track was missing an index 01");
					if (!cue_track.Indexes.ContainsKey(0))
					{
						//index 0 will default to the same as index 1.
						//i am not sure whether it is valid to have two indexes with the same timestamp.
						//we will do this to simplify some processing, but we can purge it in a later pass if we need to.
						var cti = new Cue.CueTrackIndex(0);
						cue_track.Indexes[0] = cti;
						cti.Timestamp = cue_track.Indexes[1].Timestamp;
					}
				}

				//validate that the first index in the file is 00:00:00
				if (cue_file.Tracks[0].Indexes[0].Timestamp.ABA != 0) throw new Cue.CueBrokenException("`The first index of a blob must start at 00:00:00.`");


				//for each track within the file:
				for (int t = 0; t < cue_file.Tracks.Count; t++)
				{
					var cue_track = cue_file.Tracks[t];

					//record the disc ABA that this track started on
					int track_disc_aba_start = Sectors.Count;

					//record the pregap location. it will default to the start of the track unless we supplied a pregap command
					int track_disc_pregap_aba = track_disc_aba_start;

					int blob_track_start = blob_timestamp;

					//enforce a rule of our own: every track within the file must have the same sector size
					//we do know that files can change between track types within a file, but we're not sure what to do if the sector size changes
					if (Cue.BINSectorSizeForTrackType(cue_track.TrackType) != blob_sectorsize) throw new Cue.CueBrokenException("Found different sector sizes within a cue blob. We don't know how to handle that.");

					//check integrity of track sequence and setup data structures
					//TODO - check for skipped tracks in cue parser instead
					if (cue_track.TrackNum != curr_track) throw new Cue.CueBrokenException("Found a cue with skipped tracks");
					var toc_track = new DiscTOC.Track();
					toc_track.num = curr_track;
					toc_track.TrackType = cue_track.TrackType;
					session.Tracks.Add(toc_track);

					if (curr_track == 1)
					{
						if (cue_track.PreGap.ABA != 0)
							throw new InvalidOperationException("not supported (yet): cue files with track 1 pregaps");
						//but now we add one anyway, because every existing cue+bin seems to implicitly specify this
						cue_track.PreGap = new Timestamp(150);
					}

					//check whether a pregap is requested.
					//this causes empty sectors to get generated without consuming data from the blob
					if (cue_track.PreGap.ABA > 0)
					{
						for (int i = 0; i < cue_track.PreGap.ABA; i++)
						{
							Sectors.Add(new SectorEntry(pregap_sector));
						}
					}

					//look ahead to the next track's index 1 so we can see how long this track's last index is
					//or, for the last track, use the length of the file
					int track_length_aba;
					if (t == cue_file.Tracks.Count - 1)
						track_length_aba = blob_length_aba - blob_timestamp;
					else track_length_aba = cue_file.Tracks[t + 1].Indexes[1].Timestamp.ABA - blob_timestamp;
					//toc_track.length_aba = track_length_aba; //xxx

					//find out how many indexes we have
					int num_indexes = 0;
					for (num_indexes = 0; num_indexes <= 99; num_indexes++)
						if (!cue_track.Indexes.ContainsKey(num_indexes)) break;

					//for each index, calculate length of index and then emit it
					for (int index = 0; index < num_indexes; index++)
					{
						bool is_last_index = index == num_indexes - 1;

						//install index into hierarchy
						var toc_index = new DiscTOC.Index();
						toc_index.num = index;
						toc_track.Indexes.Add(toc_index);
						if (index == 0)
						{
							toc_index.aba = track_disc_pregap_aba - (cue_track.Indexes[1].Timestamp.ABA - cue_track.Indexes[0].Timestamp.ABA);
						}
						else toc_index.aba = Sectors.Count;

						//calculate length of the index
						//if it is the last index then we use our calculation from before, otherwise we check the next index
						int index_length_aba;
						if (is_last_index)
							index_length_aba = track_length_aba - (blob_timestamp - blob_track_start);
						else index_length_aba = cue_track.Indexes[index + 1].Timestamp.ABA - blob_timestamp;

						//emit sectors
						for (int aba = 0; aba < index_length_aba; aba++)
						{
							bool is_last_aba_in_index = (aba == index_length_aba - 1);
							bool is_last_aba_in_track = is_last_aba_in_index && is_last_index;

							switch (cue_track.TrackType)
							{
								case ETrackType.Audio:  //all 2352 bytes are present
								case ETrackType.Mode1_2352: //2352 bytes are present, containing 2048 bytes of user data as well as ECM
								case ETrackType.Mode2_2352: //2352 bytes are present, containing 2336 bytes of user data, with no ECM
									{
										//these cases are all 2352 bytes
										//in all these cases, either no ECM is present or ECM is provided.
										//so we just emit a Sector_Raw
										Sector_RawBlob sector_rawblob = new Sector_RawBlob();
										sector_rawblob.Blob = cue_blob;
										sector_rawblob.Offset = (long)blob_timestamp * 2352;
										Sector_Raw sector_raw = new Sector_Raw();
										sector_raw.BaseSector = sector_rawblob;
										//take care to handle final sectors that are too short.
										if (is_last_aba_in_track && blob_leftover > 0)
										{
											Sector_ZeroPad sector_zeropad = new Sector_ZeroPad();
											sector_zeropad.BaseSector = sector_rawblob;
											sector_zeropad.BaseLength = 2352 - blob_leftover;
											sector_raw.BaseSector = sector_zeropad;
											Sectors.Add(new SectorEntry(sector_raw));
										}
										Sectors.Add(new SectorEntry(sector_raw));
										break;
									}
								case ETrackType.Mode1_2048:
									//2048 bytes are present. ECM needs to be generated to create a full sector
									{
										//ECM needs to know the sector number so we have to record that here
										int curr_disc_aba = Sectors.Count;
										var sector_2048 = new Sector_Mode1_2048(curr_disc_aba + 150);
										sector_2048.Blob = new ECMCacheBlob(cue_blob);
										sector_2048.Offset = (long)blob_timestamp * 2048;
										if (blob_leftover > 0) throw new Cue.CueBrokenException("TODO - Incomplete 2048 byte/sector bin files (iso files) not yet supported.");
										Sectors.Add(new SectorEntry(sector_2048));
										break;
									}
							} //switch(TrackType)

							//we've emitted an ABA, so consume it from the blob
							blob_timestamp++;

						} //aba emit loop

					} //index loop

					//check whether a postgap is requested. if it is, we need to generate silent sectors
					for (int i = 0; i < cue_track.PostGap.ABA; i++)
					{
						Sectors.Add(new SectorEntry(pregap_sector));
					}

					//we're done with the track now.
					//record its length:
					toc_track.length_aba = Sectors.Count - toc_track.Indexes[1].aba;
					curr_track++;

				} //track loop
			} //file loop

			//finally, analyze the length of the sessions and the entire disc by summing the lengths of the tracks
			//this is a little more complex than it looks, because the length of a thing is not determined by summing it
			//but rather by the difference in abas between start and end
			TOC.length_aba = 0;
			foreach (var toc_session in TOC.Sessions)
			{
				var firstTrack = toc_session.Tracks[0];

				//track 0, index 0 is actually -150. but cue sheets will never say that
				//firstTrack.Indexes[0].aba -= 150;

				var lastTrack = toc_session.Tracks[toc_session.Tracks.Count - 1];
				session.length_aba = lastTrack.Indexes[1].aba + lastTrack.length_aba - firstTrack.Indexes[0].aba;
				TOC.length_aba += toc_session.length_aba;
			}
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
				if (cf.FileType == CueFileType.Binary) sb.Append(" BINARY");
				if (cf.FileType == CueFileType.Wave) sb.Append(" WAVE");
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

		public enum CueFileType
		{
			Unspecified, Binary, Wave, ECM
		}

		public class CueFile
		{
			public string Path;
			public List<CueTrack> Tracks = new List<CueTrack>();

			public CueFileType FileType = CueFileType.Unspecified;
			public string StrFileType;
		}

		public List<CueFile> Files = new List<CueFile>();

		public static int BINSectorSizeForTrackType(ETrackType type)
		{
			switch (type)
			{
				case ETrackType.Mode1_2352:
				case ETrackType.Mode2_2352:
				case ETrackType.Audio:
					return 2352;
				case ETrackType.Mode1_2048:
					return 2048;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static string TrackTypeStringForTrackType(ETrackType type)
		{
			switch (type)
			{
				case ETrackType.Mode1_2352: return "MODE1/2352";
				case ETrackType.Mode2_2352: return "MODE2/2352";
				case ETrackType.Audio: return "AUDIO";
				case ETrackType.Mode1_2048: return "MODE1/2048";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static string RedumpTypeStringForTrackType(ETrackType type)
		{
			switch (type)
			{
				case ETrackType.Mode1_2352: return "Data/Mode 1";
				case ETrackType.Mode1_2048: throw new InvalidOperationException("guh dunno what to put here");
				case ETrackType.Mode2_2352: return "Data/Mode 2";
				case ETrackType.Audio: return "Audio";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public class CueTrack
		{
			public ETrackType TrackType;
			public int TrackNum;
			public Timestamp PreGap = new Timestamp();
			public Timestamp PostGap = new Timestamp();
			public Dictionary<int, CueTrackIndex> Indexes = new Dictionary<int, CueTrackIndex>();
		}

		public class CueTrackIndex
		{
			public CueTrackIndex(int num) { IndexNum = num; }
			public int IndexNum;
			public Timestamp Timestamp;
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

			bool track_has_pregap = false;
			bool track_has_postgap = false;
			int last_index_num = -1;
			CueFile currFile = null;
			CueTrack currTrack = null;
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
								switch (temp)
								{
									case "BINARY":
										currFile.FileType = CueFileType.Binary;
										break;
									case "WAVE":
									case "MP3":
										currFile.FileType = CueFileType.Wave;
										break;
								}
								currFile.StrFileType = temp;
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
							if (tracknum < 0 || tracknum > 99) throw new CueBrokenException("`All track numbers must be between 1 and 99 inclusive.`");
							string strtracktype = clp.ReadToken().ToUpper();
							currTrack = new CueTrack();
							switch (strtracktype)
							{
								case "MODE1/2352": currTrack.TrackType = ETrackType.Mode1_2352; break;
								case "MODE1/2048": currTrack.TrackType = ETrackType.Mode1_2048; break;
								case "MODE2/2352": currTrack.TrackType = ETrackType.Mode2_2352; break;
								case "AUDIO": currTrack.TrackType = ETrackType.Audio; break;
								default:
									throw new CueBrokenException("unhandled track type");
							}
							currTrack.TrackNum = tracknum;
							currFile.Tracks.Add(currTrack);
							track_has_pregap = false;
							track_has_postgap = false;
							last_index_num = -1;
							break;
						}
					case "INDEX":
						{
							if (currTrack == null) throw new CueBrokenException("invalid cue structure");
							if (clp.EOF) throw new CueBrokenException("invalid cue structure");
							if (track_has_postgap) throw new CueBrokenException("`The POSTGAP command must appear after all INDEX commands for the current track.`");
							string strindexnum = clp.ReadToken();
							int indexnum;
							if (!int.TryParse(strindexnum, out indexnum))
								throw new CueBrokenException("malformed index number");
							if (clp.EOF) throw new CueBrokenException("invalid cue structure (missing index timestamp)");
							string str_timestamp = clp.ReadToken();
							if (indexnum < 0 || indexnum > 99) throw new CueBrokenException("`All index numbers must be between 0 and 99 inclusive.`");
							if (indexnum != 1 && indexnum != last_index_num + 1) throw new CueBrokenException("`The first index must be 0 or 1 with all other indexes being sequential to the first one.`");
							last_index_num = indexnum;
							CueTrackIndex cti = new CueTrackIndex(indexnum);
							cti.Timestamp = new Timestamp(str_timestamp);
							cti.IndexNum = indexnum;
							currTrack.Indexes[indexnum] = cti;
							break;
						}
					case "PREGAP":
						if (track_has_pregap) throw new CueBrokenException("`Only one PREGAP command is allowed per track.`");
						if (currTrack.Indexes.Count > 0) throw new CueBrokenException("`The PREGAP command must appear after a TRACK command, but before any INDEX commands.`");
						currTrack.PreGap = new Timestamp(clp.ReadToken());
						track_has_pregap = true;
						break;
					case "POSTGAP":
						if (track_has_postgap) throw new CueBrokenException("`Only one POSTGAP command is allowed per track.`");
						track_has_postgap = true;
						currTrack.PostGap = new Timestamp(clp.ReadToken());
						break;
					case "CATALOG":
					case "PERFORMER":
					case "SONGWRITER":
					case "TITLE":
					case "ISRC":
						//TODO - keep these for later?
						break;
					default:
						throw new CueBrokenException("unsupported cue command: " + key);
				}
			} //end cue parsing loop
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
							case '/':
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