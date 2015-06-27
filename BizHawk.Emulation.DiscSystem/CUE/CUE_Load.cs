//TODO:
//"The first index of a file must start at 00:00:00" - if this isnt the case, we'll be doing nonsense for sure. so catch it
//Recover the idea of TOCPoints maybe, as it's a more flexible way of generating the structure.

//TODO
//check for flags changing after a PREGAP is processed. the PREGAP can't correctly run if the flags aren't set

using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	partial class CUE_Format2
	{
		/// <summary>
		/// Loads a cue file into a Disc.
		/// For this job, virtually all nonsense input is treated as errors, but the process will try to recover as best it can.
		/// The user should still reject any jobs which generated errors
		/// </summary>
		public class LoadCueJob : LoggedJob
		{
			/// <summary>
			/// The results of the analysis job, a prerequisite for this
			/// </summary>
			public AnalyzeCueJob IN_AnalyzeJob;

			/// <summary>
			/// The resulting disc
			/// </summary>
			public Disc OUT_Disc;
		}

		private enum SectorWriteType
		{
			Normal, Pregap, Postgap
		}

		public class CDTextData
		{
			public string Songwriter;
			public string Performer;
			public string Title;
		}

		/// <summary>
		/// Represents a significant event in the structure of the disc which you might encounter while reading it sequentially.
		/// </summary>
		public class DiscPoint
		{
			/// <summary>
			/// The Absolute LBA of this event
			/// </summary>
			public int AbsoluteLBA;

			/// <summary>
			/// The relative LBA of this event -- in other words, relative to Index 1.
			/// This would be used for starting the pregap relative addressing.
			/// </summary>
			public int RelativeLBA;

			/// <summary>
			/// Track number at this point
			/// </summary>
			public int TrackNumber;

			/// <summary>
			/// Index number at this point.
			/// If it's 0, we'll be in a pregap
			/// </summary>
			public int IndexNumber;

			/// <summary>
			/// Q status at this point
			/// </summary>
			public BCD2 Q_Status;

			/// <summary>
			/// The CD Text information at this point
			/// </summary>
			public CDTextData CDText = new CDTextData();

			/// <summary>
			/// The ISRC code (if present) at this point. null if not present.
			/// </summary>
			public string ISRC;
		}


		class CueIndexInfo
		{
			public int Number;
			public Timestamp FileTimestamp;

			public override string ToString()
			{
				return string.Format("I#{0:D2} {1}", Number, FileTimestamp);
			}
		}

		class CueTrackInfo
		{
			public int BlobIndex;
			public int Number;
			public CDTextData CDTextData = new CDTextData();
			public Timestamp Pregap, Postgap;
			public CueFile.TrackFlags Flags = CueFile.TrackFlags.None;
			public CueFile.TrackType TrackType = CueFile.TrackType.Unknown;

			public List<CueIndexInfo> Indexes = new List<CueIndexInfo>();

			public override string ToString()
			{
				var idx = Indexes.Find((i)=>i.Number == 1);
				if (idx == null)
					return string.Format("T#{0:D2} NO INDEX 1", Number);
				else
				{
					var indexlist = string.Join("|",Indexes);
					return string.Format("T#{0:D2} {1}:{2} ({3})", Number, BlobIndex, idx.FileTimestamp,indexlist);
					}
			}
		}

		enum CuePointType
		{
			ZeroPregap, NormalPregap, Normal, Postgap,
		}

		class CuePoint
		{
			public DiscPoint DiscPoint = new DiscPoint();
			public CuePointType Type;
			public int BlobIndex;
			public int BlobTimestampLBA;
		}

		void AnalyzeTracks(LoadCueJob job)
		{
			var a = job.IN_AnalyzeJob;
			var cue = job.IN_AnalyzeJob.IN_CueFile;

			List<CueTrackInfo> tracks = new List<CueTrackInfo>();

			//current file tracking
			int blob_index = -1;

			//current cdtext and ISRC state
			string cdtext_songwriter = null, cdtext_performer = null, cdtext_title = null;
			string isrc = null;

			//current track/index state
			bool trackHasFlags = false;
			CueTrackInfo track_curr = null;

			//first, collate information into high level description of each track
			for (int i = 0; i < cue.Commands.Count; i++)
			{
				var cmd = cue.Commands[i];

				//these commands get dealt with globally. nothing to be done here yet
				if (cmd is CueFile.Command.CATALOG || cmd is CueFile.Command.CDTEXTFILE) continue;

				//nothing to be done for comments
				if (cmd is CueFile.Command.REM) continue;
				if (cmd is CueFile.Command.COMMENT) continue;

				//handle cdtext and ISRC state updates, theyre kind of like little registers
				if (cmd is CueFile.Command.PERFORMER)
					cdtext_performer = (cmd as CueFile.Command.PERFORMER).Value;
				if (cmd is CueFile.Command.SONGWRITER)
					cdtext_songwriter = (cmd as CueFile.Command.SONGWRITER).Value;
				if (cmd is CueFile.Command.TITLE)
					cdtext_title = (cmd as CueFile.Command.TITLE).Value;
				if (cmd is CueFile.Command.ISRC)
					isrc = (cmd as CueFile.Command.ISRC).Value;

				//flags are also a kind of a register. but the flags value is reset by the track command
				if (cmd is CueFile.Command.FLAGS)
				{
					if (track_curr == null)
						job.Warn("FLAG command received before a TRACK command; ignoring");
					else if (trackHasFlags)
						job.Warn("Multiple FLAGS commands in track {0}; subsequent commands are ignored", track_curr.Number);
					else
					{
						track_curr.Flags = (cmd as CueFile.Command.FLAGS).Flags;
						trackHasFlags = true;
					}
				}

				if (cmd is CueFile.Command.TRACK)
				{
					if(blob_index == -1)
						job.Error("TRACK command received before FILE command; ignoring");
					else
					{
						var track = cmd as CueFile.Command.TRACK;

						//setup new track
						track_curr = new CueTrackInfo();
						track_curr.BlobIndex = blob_index;
						track_curr.Number = track.Number;
						track_curr.TrackType = track.Type;
						tracks.Add(track_curr);

						//setup default flags for the track
						if (track.Type != CueFile.TrackType.Audio)
							track_curr.Flags = CueFile.TrackFlags.DATA;
						trackHasFlags = false;
					}
				}

				if (cmd is CueFile.Command.FILE)
				{
					blob_index++;
					track_curr = null;
				}

				if (cmd is CueFile.Command.INDEX)
				{
					var cfindex = cmd as CueFile.Command.INDEX;
					if(track_curr == null)
						job.Error("INDEX command received before TRACK command; ignoring");
					else if (track_curr.Postgap.Valid)
						job.Warn("INDEX command received after POSTGAP; ignoring");
					else if (cfindex.Number != track_curr.Indexes.Count && (cfindex.Number != 1 && track_curr.Indexes.Count==0))
						job.Warn("non-sequential INDEX command received; ignoring");
					else {
						var index = new CueIndexInfo { Number = cfindex.Number, FileTimestamp = cfindex.Timestamp };
						track_curr.Indexes.Add(index);
					}
				}
				
				if (cmd is CueFile.Command.POSTGAP)
				{
					if(track_curr == null)
						job.Warn("POSTGAP command received before TRACK command; ignoring");
					else if(track_curr.Postgap.Valid)
						job.Warn("Multiple POSTGAP commands specified for a track; ignoring");
					else
						track_curr.Postgap = (cmd as CueFile.Command.POSTGAP).Length;
				}

				if (cmd is CueFile.Command.PREGAP)
				{
					if (track_curr == null)
						job.Warn("PREGAP command received before TRACK command; ignoring");
					else if (track_curr.Pregap.Valid)
						job.Warn("Multiple PREGAP commands specified for a track; ignoring");
					else
						track_curr.Pregap = (cmd as CueFile.Command.PREGAP).Length;
				}
			} //commands loop

			//check for tracks with no indexes, which will wreck our processing later and is an error anyway
			{
			RETRY:
				foreach (var t in tracks)
					if(t.Indexes.Count == 0)
					{
						job.Error("TRACK {0} is missing an INDEX",t.Number);
						tracks.Remove(t);
						goto RETRY;
					}
				}

			//now, create a todo list
			List<CuePoint> todo = new List<CuePoint>();
			int lba = 0;
			foreach (var t in tracks)
			{
				//find total length of pregap, so we can figure out how negative the relative timestamp goes
				int lbaPregap0to1 = 0;
				if (t.Indexes.Count > 1)
					lbaPregap0to1 = t.Indexes[1].FileTimestamp.Sector - t.Indexes[0].FileTimestamp.Sector;

				//if(t.Pregap.Valid)
				//{
				//  var cpPregap = new CuePoint();
				//  cpPregap.DiscPoint.AbsoluteLBA = lba;
				//  cpPregap.DiscPoint
				
			}
		}

		/// <summary>
		/// runs a LoadCueJob
		/// </summary>
		public void LoadCueFile(LoadCueJob job)
		{
			AnalyzeTracks(job);

			//params
			var a = job.IN_AnalyzeJob;
			var cue = job.IN_AnalyzeJob.IN_CueFile;
			var disc = new Disc();

			//utils
			var zero_sector = new Sector_Zero();
			var zero_subSector = new ZeroSubcodeSector();

			//generate lead-in gap
			//TODO - shouldnt this have proper subcode? dunno
			//mednafen will probably be testing this in the next release
			for(int i=0;i<150;i++)
			{
				var se_leadin = new SectorEntry(zero_sector);
				se_leadin.SubcodeSector = zero_subSector;
				disc.Sectors.Add(se_leadin);
			}

			//current cdtext and ISRC state
			string cdtext_songwriter = null, cdtext_performer = null, cdtext_title = null;
			string isrc = null;

			//current track state
			CueFile.TrackFlags track_pendingFlags = CueFile.TrackFlags.None;
			CueFile.TrackFlags track_flags = CueFile.TrackFlags.None;
			bool track_hasPendingFlags = false;
			int track_num = 0;
			//int track_index0MSF = -1; //NOT NEED ANYMORE
			int track_index1MSF = -1;
			bool track_readyForPregapCommand = false;
			CueFile.Command.TRACK track_pendingCommand = null;
			CueFile.Command.TRACK track_currentCommand = null;
			Timestamp postgap_pending = new Timestamp();
			Timestamp pregap_pending = new Timestamp();
			Timestamp pregap_current = new Timestamp();

			//current index state
			int index_num = -1;
			int index_msf = -1; //file-relative, remember

			//current file state
			CueFile.Command.FILE file_command = null;
			int file_cfi_index = -1;
			long file_ofs = 0, file_len = 0;
			int file_ownmsf = -1;
			IBlob file_blob = null;

			//current output state
			SubchannelQ priorSubchannelQ = new SubchannelQ();
			int LBA = 0;
			List<IDisposable> resources = disc.DisposableResources;

			//lets start with session type CDDA. we'll change it if we ever find a track of a different format (this is based on mednafen's behaviour)
			var TOCMiscInfo = new Synthesize_A0A1A2_Job { IN_FirstRecordedTrackNumber = -1, IN_LastRecordedTrackNumber = -1, IN_LeadoutTimestamp = new Timestamp(0) };
			TOCMiscInfo.IN_Session1Format = DiscTOCRaw.SessionFormat.Type00_CDROM_CDDA; 

			//a little subroutine to wrap a sector if the blob is out of space
			Func<int,Sector_RawBlob> eatBlobAndWrap = (required) =>
			{
				IBlob blob = file_blob;
				var ret = new Sector_RawBlob { Blob = file_blob, Offset = file_ofs };
				if (file_ofs + required > file_len)
				{
					job.Warn("Zero-padding mis-sized file: " + Path.GetFileName(file_command.Path));
					blob = Disc.Blob_ZeroPadBuffer.MakeBufferFrom(file_blob,file_ofs,required);
					resources.Add(blob);
					ret.Blob = blob;
					ret.Offset = 0;
				}
				file_ofs += required;
				return ret;
			};

			Action emitRawTocEntry = () =>
			{
				//NOT TOO SURE ABOUT ALL THIS YET. NEED TO VALIDATE MORE DEEPLY.

				SubchannelQ sq = new SubchannelQ();

				//absent some kind of policy for how to set it, this is a safe assumption:
				byte ADR = 1;

				sq.SetStatus(ADR, (EControlQ)(int)track_flags);
				sq.q_tno.BCDValue = 0; //kind of a little weird here.. the track number becomes the 'point' and put in the index instead. 0 is the track number here.
				sq.q_index = BCD2.FromDecimal(track_num);

				//not too sure about these yet
				sq.min = BCD2.FromDecimal(0);
				sq.sec = BCD2.FromDecimal(0);
				sq.frame = BCD2.FromDecimal(0);

				sq.AP_Timestamp = new Timestamp(LBA + 150); //its supposed to be an absolute timestamp

				disc.RawTOCEntries.Add(new RawTOCEntry { QData = sq });
			};

			//a little subroutine to write a sector
			//TODO - I intend to rethink how the sector interfaces work, but not yet. It works well enough now.
			Action<SectorWriteType> writeSector = (SectorWriteType type) =>
			{
				ISector siface = null;

				if (type == SectorWriteType.Normal)
				{
					switch (track_currentCommand.Type)
					{
						default:
						case CueFile.TrackType.Unknown:
							throw new InvalidOperationException("Internal error processing unknown track type which should have been caught earlier");

						case CueFile.TrackType.CDI_2352:
						case CueFile.TrackType.Mode1_2352:
						case CueFile.TrackType.Mode2_2352:
						case CueFile.TrackType.Audio:
							//these cases are all 2352 bytes.
							//in all these cases, either no ECM is present or ECM is provided. so we just emit a Sector_RawBlob
							siface = eatBlobAndWrap(2352);
							break;

						case CueFile.TrackType.Mode1_2048:
							{
								//2048 bytes are present. ECM needs to be generated to create a full raw sector
								var raw = eatBlobAndWrap(2048);
								siface = new Sector_Mode1_2048(LBA + 150)  //pass the ABA I guess
								{
									Blob = new ECMCacheBlob(raw.Blob), //archaic
									Offset = raw.Offset
								};
								break;
							}

						case CueFile.TrackType.CDG: //2448
						case CueFile.TrackType.Mode2_2336:
						case CueFile.TrackType.CDI_2336:
							throw new InvalidOperationException("Track types not supported yet");
					}
				}
				else if (type == SectorWriteType.Postgap)
				{
					//TODO - does subchannel need to get written differently?
					siface = zero_sector;
				}
				else if (type == SectorWriteType.Pregap)
				{
					//TODO - does subchannel need to get written differently?
					siface = zero_sector;
				}

				//make the subcode
				//TODO - according to policies, or better yet, defer this til it's needed (user delivers a policies object to disc reader apis)
				//at any rate, we'd paste this logic into there so let's go ahead and write it here
				var subcode = new BufferedSubcodeSector();
				SubchannelQ sq = new SubchannelQ();

				//absent some kind of policy for how to set it, this is a safe assumption:
				byte ADR = 1;

				sq.SetStatus(ADR, (EControlQ)(int)track_flags);

				sq.q_tno = BCD2.FromDecimal(track_num);
				sq.q_index = BCD2.FromDecimal(index_num);

				int ABA = LBA + 150;
				sq.ap_min = BCD2.FromDecimal(new Timestamp(ABA).MIN);
				sq.ap_sec = BCD2.FromDecimal(new Timestamp(ABA).SEC);
				sq.ap_frame = BCD2.FromDecimal(new Timestamp(ABA).FRAC);

				int track_relative_msf = file_ownmsf - track_index1MSF;

				//adjust the track_relative_msf here, since it's inserted before index 0
				if (type == SectorWriteType.Pregap)
				{
					track_relative_msf -= pregap_current.Sector;
				}

				if (index_num == 0 || type == SectorWriteType.Pregap)
				{
					//PREGAP:
					//things are negative here.
					if (track_relative_msf > 0) throw new InvalidOperationException("Perplexing internal error with non-negative pregap MSF");
					track_relative_msf = -track_relative_msf;

					//now for something special.
					//yellow-book says:
					//pre-gap for "first part of a digital data track not containing user data and encoded as a pause"
					//first interval: at least 75 sectors coded as preceding track
					//second interval: at least 150 sectors coded as user data track.
					//so... we ASSUME the 150 sector pregap is more important. so if thats all there is, theres no 75 sector pregap like the old track
					//if theres a longer pregap, then we generate weird old track pregap to contain the rest.
					//TODO - GENERATE P SUBCHANNEL
					if (track_relative_msf > 150)
					{
						//only if we're burning a data track now
						if((track_flags & CueFile.TrackFlags.DATA)!=0)
							sq.q_status = priorSubchannelQ.q_status;
					}
				}
				sq.min = BCD2.FromDecimal(new Timestamp(track_relative_msf).MIN);
				sq.sec = BCD2.FromDecimal(new Timestamp(track_relative_msf).SEC);
				sq.frame = BCD2.FromDecimal(new Timestamp(track_relative_msf).FRAC);
				
				//finally we're done: synthesize subchannel
				subcode.Synthesize_SubchannelQ(ref sq, true);

				priorSubchannelQ = sq;

				//now we have the ISector and subcode; make the SectorEntry
				var se = new SectorEntry(siface);
				se.SubcodeSector = subcode;
				disc.Sectors.Add(se);
				LBA++;
				file_ownmsf++;
			};

			//generates sectors until the file is exhausted
			Action finishFile = () =>
			{
				while (file_ofs < file_len)
					writeSector(SectorWriteType.Normal);
			};

			Action writePostgap = () =>
			{
				if (postgap_pending.Valid)
					for (int i = 0; i < postgap_pending.Sector; i++)
					{
						writeSector(SectorWriteType.Postgap);
					}
			};

			Action writePregap = () =>
			{
				if (pregap_current.Valid)
				{
					if (pregap_current.Sector > 150)
					{
						int zzz = 9;
					}
					for (int i = 0; i < pregap_current.Sector; i++)
					{
						writeSector(SectorWriteType.Pregap);
					}
				}
			};
			
			//prepare disc structure
			disc.Structure = new DiscStructure();
			disc.Structure.Sessions.Add(new DiscStructure.Session());

			//now for the magic. Process commands in order
			for (int i = 0; i < cue.Commands.Count; i++)
			{
				var cmd = cue.Commands[i];

				//these commands get dealt with globally. nothing to be done here
				if (cmd is CueFile.Command.CATALOG || cmd is CueFile.Command.CDTEXTFILE) continue;

				//nothing to be done for comments
				if (cmd is CueFile.Command.REM) continue;
				if (cmd is CueFile.Command.COMMENT) continue;

				//handle cdtext and ISRC state updates, theyre kind of like little registers
				if (cmd is CueFile.Command.PERFORMER)
					cdtext_performer = (cmd as CueFile.Command.PERFORMER).Value;
				if (cmd is CueFile.Command.SONGWRITER)
					cdtext_songwriter = (cmd as CueFile.Command.SONGWRITER).Value;
				if (cmd is CueFile.Command.TITLE)
					cdtext_title = (cmd as CueFile.Command.TITLE).Value;
				if(cmd is CueFile.Command.ISRC)
					isrc = (cmd as CueFile.Command.ISRC).Value;

				//flags are also a kind of a register. but the flags value is reset by the track command
				if (cmd is CueFile.Command.FLAGS)
				{
					if (track_hasPendingFlags)
						job.Warn("Multiple FLAGS commands in track {0}; subsequent commands are ignored", track_num);
					else
					{
						track_pendingFlags = (cmd as CueFile.Command.FLAGS).Flags;
						track_hasPendingFlags = true;
					}
				}

				if (cmd is CueFile.Command.TRACK)
				{
					var track = cmd as CueFile.Command.TRACK;

					//HOW TO HANDLE TRACKS:
					//Tracks don't have timestamps. Therefore, they're always pending until something that does have a timestamp
					track_pendingCommand = track;
					track_pendingFlags = CueFile.TrackFlags.None;
					track_readyForPregapCommand = true;
				}

				if (cmd is CueFile.Command.FILE)
				{
					var file = cmd as CueFile.Command.FILE;

					//HOW TO HANDLE FILES:
					//1. flush the current file with all current register settings
					//2. mount the next file
					//3. clear some register settings

					//1. do the flush, if needed
					if (file_cfi_index != -1)
						finishFile();
					
					//2a. clean up by nulling before starting next file...
					file_command = null;
					file_ofs = 0;
					file_len = 0;
					file_blob = null;

					//2b. mount the next file 
					file_command = file;
					file_ownmsf = 0;
					var cfi = a.OUT_FileInfos[++file_cfi_index];

					//TODO - a lot of redundant code here, maybe Blob should know his length in the IBlob interface since every freaking thing depends on it
					if (cfi.Type == AnalyzeCueJob.CueFileType.BIN || cfi.Type == AnalyzeCueJob.CueFileType.Unknown)
					{
						//raw files:
						var blob = new Disc.Blob_RawFile { PhysicalPath = cfi.FullPath };
						resources.Add(file_blob = blob);
						file_len = blob.Length;
					}
					else if (cfi.Type == AnalyzeCueJob.CueFileType.ECM)
					{
						var blob = new Disc.Blob_ECM();
						resources.Add(file_blob = blob);
						blob.Load(cfi.FullPath);
						file_len = blob.Length;
					}
					else if (cfi.Type == AnalyzeCueJob.CueFileType.WAVE)
					{
						var blob = new Disc.Blob_WaveFile();
						resources.Add(file_blob = blob);
						blob.Load(cfi.FullPath);
						file_len = blob.Length;
					}
					else if (cfi.Type == AnalyzeCueJob.CueFileType.DecodeAudio)
					{
						FFMpeg ffmpeg = new FFMpeg();
						if (!ffmpeg.QueryServiceAvailable())
						{
							throw new DiscReferenceException(cfi.FullPath, "No decoding service was available (make sure ffmpeg.exe is available. even though this may be a wav, ffmpeg is used to load oddly formatted wave files. If you object to this, please send us a note and we'll see what we can do. It shouldn't be too hard.)");
						}
						AudioDecoder dec = new AudioDecoder();
						byte[] buf = dec.AcquireWaveData(cfi.FullPath);
						var blob = new Disc.Blob_WaveFile();
						resources.Add(file_blob = blob);
						blob.Load(new MemoryStream(buf));
					}

					//3. reset track and index registers
					track_num = 0;
					//TODO - do I need to reset the track extra stuff here? nothing can get generated
					//track_pendingCommand = null;
					//track_currentCommand = null;
					//track_flags = CueFile.TrackFlags.None;
					//track_hasFlags = false;
					//track_index0MSF = -1;
					//track_index1MSF = -1;
					//and how about this? it should get reset when the track begins
					//index_num = -1;
					//index_msf = -1;
					
				} //FILE command

				if (cmd is CueFile.Command.INDEX)
				{
					var index = cmd as CueFile.Command.INDEX;
					var timestamp = index.Timestamp;
					var i_num = index.Number;

					//cant get a pregap command anymore
					track_readyForPregapCommand = false;

					//ok, now for some truly arcane stuff.

					//first, a warning if this isn't sequential
					if (i_num != 0 && i_num != 1 && i_num != index_num + 1)
					{
						job.Error("Invalid index number {0}: must be one greater than previous. Fixing that for you.", i_num);
						i_num = index_num + 1;
					}

					//now, an error if this is the first index and isnt 0 or 1
					if (index_num == -1 && i_num != 0 && i_num != 1)
					{
						job.Error("Invalid index {0}: 1st IDX of track must be 0 or 1. Pretend it's 1.", i_num);
						//how to recover? assume it's 1
						i_num = 1;
					}

					//now, an error if this is not at 00:00:00 for the first index in a file
					if (file_ofs == 0 && timestamp.Sector != 0 && index_num == -1)
					{
						job.Error("Invalid IDX {0}: 1st IDX in file must be at 00:00:00 but it's at {1}", i_num, index.Timestamp);
						timestamp = new Timestamp(0);
					}

					if (i_num == 0)
					{
						//if this is index 0, we're gonna have a pregap.
						//lets just record that we got this index, and go to the next command
						index_num = 0;
						//DONT NEED THIS ANYMORE?
						//track_index0MSF = timestamp.Sector;
					}
					else if (i_num == 1)
					{
						//if we got an index 1:
						track_index1MSF = timestamp.Sector;

						//DONT NEED THIS ANYMORE?
						////if we didnt get an index 0 LBA, give up and assume it's the same as this
						//if (track_index0MSF == -1)
						//  track_index0MSF = timestamp.Sector;

						//we can now execute a pending track change command
						if (track_pendingCommand == null)
							throw new InvalidOperationException("Unrecoverable error processing CUE: index without track context");

						//before we begin a track, write any postgap that we may have queued...
						writePostgap();

						//...and also generate sectors up til the current index (as the last index)
						while (file_ownmsf < timestamp.Sector)
							writeSector(SectorWriteType.Normal);

						//begin the track:
						track_currentCommand = track_pendingCommand;
						track_pendingCommand = null;
						track_hasPendingFlags = false;
						track_flags = track_pendingFlags;
						track_num = track_currentCommand.Number;

						pregap_current = pregap_pending;
						pregap_pending = new Timestamp();

						//default flags:
						if (track_currentCommand.Type == CueFile.TrackType.Audio) { }
						else track_flags |= CueFile.TrackFlags.DATA;
						postgap_pending = new Timestamp();

						//account for it in structure
						var st = new DiscStructure.Track { Number = track_num };
						//TODO - move out of here into analysis stage
						switch (track_currentCommand.Type)
						{
							default:
							case CueFile.TrackType.CDG:
							case CueFile.TrackType.Unknown: throw new InvalidOperationException("UNEXPECTED PANDA MAYOR");

							case CueFile.TrackType.Audio:
								st.TrackType = DiscStructure.ETrackType.Audio;
								st.ModeHeuristic = 0;
								break;
							case CueFile.TrackType.Mode1_2048:
							case CueFile.TrackType.Mode1_2352:
								st.TrackType = DiscStructure.ETrackType.Data;
								st.ModeHeuristic = 1;
								break;
							case CueFile.TrackType.Mode2_2352:
							case CueFile.TrackType.Mode2_2336:
							case CueFile.TrackType.CDI_2336:
							case CueFile.TrackType.CDI_2352:
								st.TrackType = DiscStructure.ETrackType.Data;
								st.ModeHeuristic = 1;
								break;
						}
						disc.Structure.Sessions[0].Tracks.Add(st);

						//maintain some memos
						if (TOCMiscInfo.IN_FirstRecordedTrackNumber == -1)
							TOCMiscInfo.IN_FirstRecordedTrackNumber = track_num;
						TOCMiscInfo.IN_LastRecordedTrackNumber = track_num;
						//update the disc type... do we need to do any double checks here to check for regressions? doubt it.
						if (TOCMiscInfo.IN_Session1Format == DiscTOCRaw.SessionFormat.Type00_CDROM_CDDA)
						{
							switch (track_currentCommand.Type)
							{
								case CueFile.TrackType.Mode2_2336:
								case CueFile.TrackType.Mode2_2352:
									TOCMiscInfo.IN_Session1Format = DiscTOCRaw.SessionFormat.Type20_CDXA;
									break;
								case CueFile.TrackType.CDI_2336:
								case CueFile.TrackType.CDI_2352:
									TOCMiscInfo.IN_Session1Format = DiscTOCRaw.SessionFormat.Type10_CDI;
									break;
								default:
									//no changes here
									break;
							}
						}

						{
							var _currTrack = disc.Structure.Sessions[0].Tracks[disc.Structure.Sessions[0].Tracks.Count - 1];
							if (_currTrack.Indexes.Count == 0)
								_currTrack.Indexes.Add(new DiscStructure.Index { Number = 0, LBA = LBA });
						}

						//write a special pregap if a command was pending
						writePregap();
					}

					//emit it to TOC if needed
					if (i_num == 1)
						emitRawTocEntry();

					//update sense of current index 
					index_num = i_num;
					index_msf = timestamp.Sector;

					{
						var _currTrack = disc.Structure.Sessions[0].Tracks[disc.Structure.Sessions[0].Tracks.Count - 1];
						_currTrack.Indexes.Add(new DiscStructure.Index { Number = index_num, LBA = LBA });
					}
				}

				if (cmd is CueFile.Command.POSTGAP)
				{
					var postgap = cmd as CueFile.Command.POSTGAP;

					if(index_num == -1)
					{
						job.Warn("Ignoring POSTGAP before any indexes");
						goto NO_POSTGAP;
					}

					if(postgap_pending.Valid)
					{
						job.Warn("Ignoring multiple POSTGAPs for track");
						goto NO_POSTGAP;
					}

					postgap_pending = postgap.Length;

					NO_POSTGAP: ;
				}

				if (cmd is CueFile.Command.PREGAP)
				{
					//see particularly http://digitalx.org/cue-sheet/examples/index.html#example05 for the most annoying example
					var pregap = cmd as CueFile.Command.PREGAP;

					if (!track_readyForPregapCommand)
					{
						job.Warn("Not ready for PREGAP command; ignoring");
						goto NO_PREGAP;
					}

					pregap_pending = pregap.Length;
					track_readyForPregapCommand = false;
				
				NO_PREGAP:
					;
				}

			}

			//do a final flush
			finishFile();
			writePostgap();

			//do some stupid crap for testing. maybe it isnt so stupid, but its what we have for now
			disc.Structure.LengthInSectors = disc.Sectors.Count;

			//add RawTOCEntries A0 A1 A2 to round out the TOC
			TOCMiscInfo.IN_LeadoutTimestamp = new Timestamp(LBA + 150);
			TOCMiscInfo.Run(disc.RawTOCEntries);

			//generate the TOCRaw from the RawTocEntries
			var tocSynth = new DiscTOCRaw.SynthesizeFromRawTOCEntriesJob() { Entries = disc.RawTOCEntries };
			tocSynth.Run();
			disc.TOCRaw = tocSynth.Result;

			//generate lead-out track with some canned number of sectors
			//TODO - move this somewhere else and make it controllable depending on which console is loading up the disc
			//TODO - we're not doing this yet
			//var synthLeadoutJob = new Disc.SynthesizeLeadoutJob { Disc = disc, Length = 150 };
			//synthLeadoutJob.Run();

			//blech, old crap, maybe
			disc.Structure.Synthesize_TOCPointsFromSessions();

			job.OUT_Disc = disc;
			job.FinishLog();

		} //LoadCueFile	
	}
}