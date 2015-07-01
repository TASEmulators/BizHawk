//TODO:
//"The first index of a file must start at 00:00:00" - if this isnt the case, we'll be doing nonsense for sure. so catch it
//Recover the idea of TOCPoints maybe, as it's a more flexible way of generating the structure.

//TODO
//check for flags changing after a PREGAP is processed. the PREGAP can't correctly run if the flags aren't set
//IN GENERAL: validate more pedantically (but that code gets in the way majorly)
// - perhaps isolate validation checks into a different pass distinct from a Burn pass

//NEW IDEA:
//a cue file is a compressed representation of a more verbose format which is easier to understand
//most fundamentally, it is organized with TRACK and INDEX commands alternating.
//these should be flattened into individual records with CURRTRACK and CURRINDEX fields.
//more generally, it's organized with 'register' settings and INDEX commands alternating.
//whenever an INDEX command is received from the cue file, individual flattened records are written with the current 'register' settings 
//and an incrementing timestamp until the INDEX command appears (or the EOF happens)
//PREGAP commands are special : at the moment it is received, emit flat records with a different pregap structure
//POSTGAP commands are special : TBD

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
		internal class LoadCueJob : LoggedJob
		{
			/// <summary>
			/// The results of the compile job, a prerequisite for this
			/// </summary>
			public CompileCueJob IN_CompileJob;

			/// <summary>
			/// The resulting disc
			/// </summary>
			public Disc OUT_Disc;

			private enum BurnType
			{
				Normal, Pregap, Postgap
			}

			//current blob file state
			int file_cfi_index = -1;
			IBlob file_blob = null;
			CueFile.Command.FILE file_currentCommand = null;
			long file_ofs = 0, file_len = 0;
			int file_msf = -1;

			//current track, flags, and index state
			CueFile.Command.TRACK track_pendingCommand = null;
			CueFile.Command.TRACK track_currentCommand = null;
			CueFile.TrackFlags track_pendingFlags = CueFile.TrackFlags.None;
			CueFile.TrackFlags track_currentFlags = CueFile.TrackFlags.None;

			//burn state.
			//TODO - separate burner into another class?
			BurnType burntype_current;
			Timestamp burn_pregap_timestamp;


			void BeginBurnPregap()
			{
				//TODO?
			}

			void BurnPregap(Timestamp length)
			{
				burntype_current = BurnType.Pregap;
				burn_pregap_timestamp = length;
				int length_lba = length.Sector;

				//TODO: read [IEC10149] 20, 20.1, & 20.2 to assign pre-gap and post-gap types correctly depending on track number and previous track
				//ALSO, if the last track is data, we need to make a post-gap
				//we can grab the previously generated sector in order to figure out how to encode new pregap sectors
				for(int i=0;i<length_lba;i++)
					BurnSector();
			}

			void ProcessFile(CueFile.Command.FILE file)
			{
				////if we're currently in a file, finish it
				//if (file_currentCommand != null)
				//  BurnToEOF();

				////open the new blob
				//file_currentCommand = file;
				//file_msf = 0;
				//var cfi = IN_CompileJob.OUT_FileInfos[++file_cfi_index];

				////mount the file
				//if (cfi.Type == AnalyzeCueJob.CueFileType.BIN || cfi.Type == AnalyzeCueJob.CueFileType.Unknown)
				//{
				//  //raw files:
				//  var blob = new Disc.Blob_RawFile { PhysicalPath = cfi.FullPath };
				//  OUT_Disc.DisposableResources.Add(file_blob = blob);
				//  file_len = blob.Length;
				//}
				//else if (cfi.Type == AnalyzeCueJob.CueFileType.ECM)
				//{
				//  var blob = new Disc.Blob_ECM();
				//  OUT_Disc.DisposableResources.Add(file_blob = blob);
				//  blob.Load(cfi.FullPath);
				//  file_len = blob.Length;
				//}
				//else if (cfi.Type == AnalyzeCueJob.CueFileType.WAVE)
				//{
				//  var blob = new Disc.Blob_WaveFile();
				//  OUT_Disc.DisposableResources.Add(file_blob = blob);
				//  blob.Load(cfi.FullPath);
				//  file_len = blob.Length;
				//}
				//else if (cfi.Type == AnalyzeCueJob.CueFileType.DecodeAudio)
				//{
				//  FFMpeg ffmpeg = new FFMpeg();
				//  if (!ffmpeg.QueryServiceAvailable())
				//  {
				//    throw new DiscReferenceException(cfi.FullPath, "No decoding service was available (make sure ffmpeg.exe is available. even though this may be a wav, ffmpeg is used to load oddly formatted wave files. If you object to this, please send us a note and we'll see what we can do. It shouldn't be too hard.)");
				//  }
				//  AudioDecoder dec = new AudioDecoder();
				//  byte[] buf = dec.AcquireWaveData(cfi.FullPath);
				//  var blob = new Disc.Blob_WaveFile();
				//  OUT_Disc.DisposableResources.Add(file_blob = blob);
				//  blob.Load(new MemoryStream(buf));
				//}
			}

			void BurnToEOF()
			{
				while (file_ofs < file_len)
					BurnSector();

				//TODO - if a postgap was requested, do it now
			}

			void ProcessIndex(CueFile.Command.INDEX index)
			{
				//burn sectors with the previous registers until we reach the current index MSF
				int index_file_msf = index.Timestamp.Sector;
				while (file_msf < index_file_msf)
					BurnSector();

				//latch current track settings
				track_currentCommand = track_pendingCommand;
				track_currentFlags = track_pendingFlags;

				//index 0 is annoying. we have to code its subchannels while knowing the index that comes next
				//this is the main reason for transforming the cue file into a CueGrid (any index 0 can easily reference the index 1 that comes after it)
				if (index.Number == 0)
				{
				}
			}

			void EatBlobFileSector(int required, out IBlob blob, out long blobOffset)
			{
				blob = file_blob;
				blobOffset = file_ofs;
				if (file_ofs + required > file_len)
				{
					Warn("Zero-padding mis-sized cue blob file: " + Path.GetFileName(file_currentCommand.Path));
					blob = Disc.Blob_ZeroPadBuffer.MakeBufferFrom(file_blob,file_ofs,required);
					OUT_Disc.DisposableResources.Add(blob);
					blobOffset = 0;
				}
				file_ofs += required;
			}

			void BurnSector_Normal()
			{
				SS_Base ss = null;
				switch (track_currentCommand.Type)
				{
					case CueFile.TrackType.Mode2_2352:
						ss = new SS_2352();
						EatBlobFileSector(2352, out ss.Blob, out ss.BlobOffset);
						break;
					case CueFile.TrackType.Audio:
						ss = new SS_2352();
						EatBlobFileSector(2352, out ss.Blob, out ss.BlobOffset);
						break;
				}

				var se = new SectorEntry(null);
				se.SectorSynth = ss;
				OUT_Disc.Sectors.Add(se);
			}

			void BurnSector_Pregap()
			{
				var se = new SectorEntry(null);
				se.SectorSynth = new SS_Mode1_2048(); //TODO - actually burn the right thing
				OUT_Disc.Sectors.Add(se);

				burn_pregap_timestamp = new Timestamp(burn_pregap_timestamp.Sector - 1);
			}

			void BurnSector()
			{
				switch (burntype_current)
				{
					case BurnType.Normal:
						BurnSector_Normal();
						break;
					case BurnType.Pregap:
						BurnSector_Pregap();
						break;
				}
			}

			void MountBlobs()
			{
				foreach (var ccf in IN_CompileJob.OUT_CompiledCueFiles)
				{
					switch (ccf.Type)
					{
						case CompiledCueFileType.BIN:
						case CompiledCueFileType.Unknown:
							{
								//raw files:
								var blob = new Disc.Blob_RawFile { PhysicalPath = ccf.FullPath };
								OUT_Disc.DisposableResources.Add(file_blob = blob);
								file_len = blob.Length;
								break;
							}
						case CompiledCueFileType.ECM:
							{
								var blob = new Disc.Blob_ECM();
								OUT_Disc.DisposableResources.Add(file_blob = blob);
								blob.Load(ccf.FullPath);
								file_len = blob.Length;
								break;
							}
						case CompiledCueFileType.WAVE:
							{
								var blob = new Disc.Blob_WaveFile();
								OUT_Disc.DisposableResources.Add(file_blob = blob);
								blob.Load(ccf.FullPath);
								file_len = blob.Length;
								break;
							}
						case CompiledCueFileType.DecodeAudio:
							{
								FFMpeg ffmpeg = new FFMpeg();
								if (!ffmpeg.QueryServiceAvailable())
								{
									throw new DiscReferenceException(ccf.FullPath, "No decoding service was available (make sure ffmpeg.exe is available. even though this may be a wav, ffmpeg is used to load oddly formatted wave files. If you object to this, please send us a note and we'll see what we can do. It shouldn't be too hard.)");
								}
								AudioDecoder dec = new AudioDecoder();
								byte[] buf = dec.AcquireWaveData(ccf.FullPath);
								var blob = new Disc.Blob_WaveFile();
								OUT_Disc.DisposableResources.Add(file_blob = blob);
								blob.Load(new MemoryStream(buf));
								break;
							}
						default:
							throw new InvalidOperationException();
					}
				}
			}

			public void Run()
			{
				//params
				var compiled = IN_CompileJob;
				OUT_Disc = new Disc();

				//mount all input files
				MountBlobs();

				//make a lookup from track number to CompiledCueTrack
				Dictionary<int, CompiledCueTrack> trackLookup = new Dictionary<int, CompiledCueTrack>();
				foreach (var cct in compiled.OUT_CompiledCueTracks)
					trackLookup[cct.Number] = cct;

				//loop from track 1 to 99
				//(track 0 isnt handled yet, that's way distant work)
				for (int t = 1; t <= 99; t++)
				{
					CompiledCueTrack cct;
					if (!trackLookup.TryGetValue(t, out cct))
						continue;

					//---------------------------------
					//generate track pregap
					//per "Example 05" on digitalx.org, pregap can come from index specification and pregap command
					int specifiedPregapLength = cct.PregapLength.Sector;
					int impliedPregapLength = cct.Indexes[1].FileMSF.Sector - cct.Indexes[0].FileMSF.Sector;
					//total pregap is needed for subQ addressing of the entire pregap area
					int totalPregapLength = specifiedPregapLength + impliedPregapLength;
					for (int s = 0; s < specifiedPregapLength; s++)
					{
						//TODO - do a better job synthesizing
						var zero_sector = new Sector_Zero();
						var zero_subSector = new ZeroSubcodeSector();
						var se_pregap = new SectorEntry(zero_sector);
						se_pregap.SubcodeSector = zero_subSector;
						se_pregap.SectorSynth = new SS_Mode1_2048();
						OUT_Disc.Sectors.Add(se_pregap);
					}

					//after this, pregap sectors are generated like a normal sector, but the subQ is specified as a pregap instead of a normal track
					//---------------------------------

					//---------------------------------
					//WE ARE NOW AT INDEX 1
					//---------------------------------

					//---------------------------------
					//generate the RawTOCEntry for this track
					SubchannelQ sq = new SubchannelQ();
					//absent some kind of policy for how to set it, this is a safe assumption:
					byte ADR = 1;
					sq.SetStatus(ADR, (EControlQ)(int)cct.Flags);
					sq.q_tno.BCDValue = 0; //kind of a little weird here.. the track number becomes the 'point' and put in the index instead. 0 is the track number here.
					sq.q_index = BCD2.FromDecimal(cct.Number);
					//not too sure about these yet
					sq.min = BCD2.FromDecimal(0);
					sq.sec = BCD2.FromDecimal(0);
					sq.frame = BCD2.FromDecimal(0);
					sq.AP_Timestamp = new Timestamp(OUT_Disc.Sectors.Count + 150); //its supposed to be an absolute timestamp
					OUT_Disc.RawTOCEntries.Add(new RawTOCEntry { QData = sq });
				}


			
				////now for the magic. Process commands in order
				//for (int i = 0; i < cue.Commands.Count; i++)
				//{
				//  var cmd = cue.Commands[i];

				//  //these commands get dealt with globally. nothing to be done here
				//  if (cmd is CueFile.Command.CATALOG || cmd is CueFile.Command.CDTEXTFILE) continue;

				//  //nothing to be done for comments
				//  if (cmd is CueFile.Command.REM) continue;
				//  if (cmd is CueFile.Command.COMMENT) continue;

				//  //handle cdtext and ISRC state updates, theyre kind of like little registers
				//  if (cmd is CueFile.Command.PERFORMER)
				//    cdtext_performer = (cmd as CueFile.Command.PERFORMER).Value;
				//  if (cmd is CueFile.Command.SONGWRITER)
				//    cdtext_songwriter = (cmd as CueFile.Command.SONGWRITER).Value;
				//  if (cmd is CueFile.Command.TITLE)
				//    cdtext_title = (cmd as CueFile.Command.TITLE).Value;
				//  if (cmd is CueFile.Command.ISRC)
				//    isrc = (cmd as CueFile.Command.ISRC).Value;

				//  //flags are also a kind of a register. but the flags value is reset by the track command
				//  if (cmd is CueFile.Command.FLAGS)
				//  {
				//    track_pendingFlags = (cmd as CueFile.Command.FLAGS).Flags;
				//  }

				//  if (cmd is CueFile.Command.TRACK)
				//  {
				//    var track = cmd as CueFile.Command.TRACK;

				//    //register the track for further processing when an GENERATION command appears
				//    track_pendingCommand = track;
				//    track_pendingFlags = CueFile.TrackFlags.None;
						
				//  }

				//  if (cmd is CueFile.Command.FILE)
				//  {
				//    ProcessFile(cmd as CueFile.Command.FILE);
				//  }

				//  if (cmd is CueFile.Command.INDEX)
				//  {
				//    ProcessIndex(cmd as CueFile.Command.INDEX);
				//  }
				//}

				//BurnToEOF();

				////add RawTOCEntries A0 A1 A2 to round out the TOC
				//var TOCMiscInfo = new Synthesize_A0A1A2_Job { 
				//  IN_FirstRecordedTrackNumber = sloshy_firstRecordedTrackNumber, 
				//  IN_LastRecordedTrackNumber = sloshy_lastRecordedTrackNumber,
				//  IN_Session1Format = sloshy_session1Format,
				//  IN_LeadoutTimestamp = new Timestamp(OUT_Disc.Sectors.Count)
				//};
				//TOCMiscInfo.Run(OUT_Disc.RawTOCEntries);

				////generate the TOCRaw from the RawTocEntries
				//var tocSynth = new DiscTOCRaw.SynthesizeFromRawTOCEntriesJob() { Entries = OUT_Disc.RawTOCEntries };
				//tocSynth.Run();
				//OUT_Disc.TOCRaw = tocSynth.Result;

				////generate lead-out track with some canned number of sectors
				////TODO - move this somewhere else and make it controllable depending on which console is loading up the disc
				////TODO - we're not doing this yet
				////var synthLeadoutJob = new Disc.SynthesizeLeadoutJob { Disc = disc, Length = 150 };
				////synthLeadoutJob.Run();

				////blech, old crap, maybe
				//OUT_Disc.Structure.Synthesize_TOCPointsFromSessions();

				//FinishLog();

			} //Run()
		} //class LoadCueJob
	} //partial class CUE_Format2
} //namespace BizHawk.Emulation.DiscSystem