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

			class BlobInfo
			{
				public IBlob Blob;
				public long Length;
			}

			//not sure if we need this...
			class TrackInfo
			{
				public int Length;

				public CompiledCueTrack CompiledCueTrack;
			}

			List<BlobInfo> BlobInfos;
			List<TrackInfo> TrackInfos = new List<TrackInfo>();


			void MountBlobs()
			{
				IBlob file_blob = null;

				BlobInfos = new List<BlobInfo>();
				foreach (var ccf in IN_CompileJob.OUT_CompiledCueFiles)
				{
					var bi = new BlobInfo();
					BlobInfos.Add(bi);

					switch (ccf.Type)
					{
						case CompiledCueFileType.BIN:
						case CompiledCueFileType.Unknown:
							{
								//raw files:
								var blob = new Disc.Blob_RawFile { PhysicalPath = ccf.FullPath };
								OUT_Disc.DisposableResources.Add(file_blob = blob);
								bi.Length = blob.Length;
								break;
							}
						case CompiledCueFileType.ECM:
							{
								var blob = new Disc.Blob_ECM();
								OUT_Disc.DisposableResources.Add(file_blob = blob);
								blob.Load(ccf.FullPath);
								bi.Length = blob.Length;
								break;
							}
						case CompiledCueFileType.WAVE:
							{
								var blob = new Disc.Blob_WaveFile();
								OUT_Disc.DisposableResources.Add(file_blob = blob);
								blob.Load(ccf.FullPath);
								bi.Length = blob.Length;
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
								bi.Length = buf.Length;
								break;
							}
						default:
							throw new InvalidOperationException();
					} //switch(file type)

					//wrap all the blobs with zero padding
					bi.Blob = new Disc.Blob_ZeroPadAdapter(file_blob, bi.Length);
				}
			}


			void AnalyzeTracks()
			{
				var compiledTracks = IN_CompileJob.OUT_CompiledCueTracks;

				for(int t=0;t<compiledTracks.Count;t++)
				{
					var cct = compiledTracks[t];

					var ti = new TrackInfo() { CompiledCueTrack = cct };
					TrackInfos.Add(ti);

					//OH NO! CANT DO THIS!
					//need to read sectors from file to reliably know its ending size.
					//could determine it from file mode.
					//do we really need this?
					//if (cct.IsFinalInFile)
					//{
					//  //length is determined from length of file
						
					//}
				}
			}

			void EmitRawTOCEntry(CompiledCueTrack cct)
			{
				SubchannelQ toc_sq = new SubchannelQ();
				//absent some kind of policy for how to set it, this is a safe assumption:
				byte toc_ADR = 1;
				toc_sq.SetStatus(toc_ADR, (EControlQ)(int)cct.Flags);
				toc_sq.q_tno.BCDValue = 0; //kind of a little weird here.. the track number becomes the 'point' and put in the index instead. 0 is the track number here.
				toc_sq.q_index = BCD2.FromDecimal(cct.Number);
				//not too sure about these yet
				toc_sq.min = BCD2.FromDecimal(0);
				toc_sq.sec = BCD2.FromDecimal(0);
				toc_sq.frame = BCD2.FromDecimal(0);
				toc_sq.AP_Timestamp = new Timestamp(OUT_Disc.Sectors.Count);
				OUT_Disc.RawTOCEntries.Add(new RawTOCEntry { QData = toc_sq });
			}

			public void Run()
			{
				//params
				var compiled = IN_CompileJob;
				var context = compiled.IN_CueFormat;
				OUT_Disc = new Disc();

				//generation state
				int curr_index;
				int curr_blobIndex = -1;
				int curr_blobMSF = -1;
				BlobInfo curr_blobInfo = null;
				long curr_blobOffset = -1;

				//mount all input files
				MountBlobs();

				//unhappily, we cannot determine the length of all the tracks without knowing the length of the files
				//now that the files are mounted, we can figure the track lengths
				AnalyzeTracks();

				//loop from track 1 to 99
				//(track 0 isnt handled yet, that's way distant work)
				for (int t = 1; t < TrackInfos.Count; t++)
				{
					TrackInfo ti = TrackInfos[t];
					CompiledCueTrack cct = ti.CompiledCueTrack;

					//---------------------------------
					//generate track pregap
					//per "Example 05" on digitalx.org, pregap can come from index specification and pregap command
					int specifiedPregapLength = cct.PregapLength.Sector;
					int impliedPregapLength = cct.Indexes[1].FileMSF.Sector - cct.Indexes[0].FileMSF.Sector;
					//total pregap is needed for subQ addressing of the entire pregap area (pregap + distinct index0)
					//during generating userdata sectors this is already handled
					//we just need to know the difference for when we generate a better subQ here
					int totalPregapLength = specifiedPregapLength + impliedPregapLength;
					for (int s = 0; s < specifiedPregapLength; s++)
					{
						//TODO - do a better job synthesizing Q
						var se_pregap = new SectorEntry(null);
						var ss_pregap = new SS_Gap();
						
						//pregaps set pause flag
						//TODO - do a better job synthesizing P
						ss_pregap.Pause = true;

						se_pregap.SectorSynth = ss_pregap;
						OUT_Disc.Sectors.Add(se_pregap);
					}

					//after this, pregap sectors are generated like a normal sector, but the subQ is specified as a pregap instead of a normal track (actually, TBD)
					//---------------------------------


					//---------------------------------
					//generate sectors for this track.

					//advance to the next file if needed
					if (curr_blobIndex != cct.BlobIndex)
					{
						curr_blobIndex = cct.BlobIndex;
						curr_blobOffset = 0;
						curr_blobMSF = 0;
						curr_blobInfo = BlobInfos[curr_blobIndex];
					}

					//work until the next track is reached, or the end of the current file is reached, depending on the track type
					curr_index = 0;
					for (; ; )
					{
						bool trackDone = false;

						//select the appropriate index by inspecting the next index and seeing if we've reached it
						for (; ; )
						{
							if (curr_index == cct.Indexes.Count - 1)
								break;
							if (curr_blobMSF >= cct.Indexes[curr_index + 1].FileMSF.Sector)
							{
								curr_index++;
								if (curr_index == 1)
								{
									//WE ARE NOW AT INDEX 1: generate the RawTOCEntry for this track
									EmitRawTOCEntry(cct);
								}
							}
							else break;
						}

						//generate a sector:
						SS_Base ss = null;
						switch (cct.TrackType)
						{
							case CueFile.TrackType.Mode1_2048:
								ss = new SS_Mode1_2048() { Blob = curr_blobInfo.Blob, BlobOffset = curr_blobOffset };
								curr_blobOffset += 2048;
								break;

							case CueFile.TrackType.Mode2_2352:
							case CueFile.TrackType.Audio:
								ss = new SS_2352() { Blob = curr_blobInfo.Blob, BlobOffset = curr_blobOffset };
								curr_blobOffset += 2352;
								break;
						}

						//make the subcode
						//TODO - according to policies, or better yet, defer this til it's needed (user delivers a policies object to disc reader apis)
						//at any rate, we'd paste this logic into there so let's go ahead and write it here
						var subcode = new BufferedSubcodeSector(); //(its lame that we have to use this; make a static method when we delete this class)
						SubchannelQ sq = new SubchannelQ();
						byte ADR = 1; //absent some kind of policy for how to set it, this is a safe assumption:
						sq.SetStatus(ADR, (EControlQ)(int)cct.Flags);
						sq.q_tno = BCD2.FromDecimal(cct.Number);
						sq.q_index = BCD2.FromDecimal(curr_index);
						int LBA = OUT_Disc.Sectors.Count;
						sq.ap_min = BCD2.FromDecimal(new Timestamp(LBA).MIN);
						sq.ap_sec = BCD2.FromDecimal(new Timestamp(LBA).SEC);
						sq.ap_frame = BCD2.FromDecimal(new Timestamp(LBA).FRAC);
						int track_relative_msf = curr_blobMSF - cct.Indexes[1].FileMSF.Sector;

						//for index 0, negative MSF required and encoded oppositely. Read more at Policies declaration
						if (curr_index == 0)
						{
							if (!context.DiscMountPolicy.CUE_PauseContradictionModeA)
								track_relative_msf += 1;

							if (track_relative_msf > 0) throw new InvalidOperationException("Severe error generating cue subQ (positive relMSF for pregap)");
							track_relative_msf = -track_relative_msf;
						}
						else
							if (track_relative_msf < 0) throw new InvalidOperationException("Severe error generating cue subQ (negative relMSF for non-pregap)");

						sq.min = BCD2.FromDecimal(new Timestamp(track_relative_msf).MIN);
						sq.sec = BCD2.FromDecimal(new Timestamp(track_relative_msf).SEC);
						sq.frame = BCD2.FromDecimal(new Timestamp(track_relative_msf).FRAC);
						//finally we're done: synthesize subchannel
						subcode.Synthesize_SubchannelQ(ref sq, true);

						//generate subP
						if (curr_index == 0)
							ss.Pause = true;

						//make the SectorEntry (some temporary bullshit here)
						var se = new SectorEntry(null);
						se.SectorSynth = ss;
						ss.sq = sq;
						OUT_Disc.Sectors.Add(se);
						curr_blobMSF++;

						if (cct.IsFinalInFile)
						{
							//sometimes, break when the file is exhausted
							if (curr_blobOffset >= curr_blobInfo.Length)
								trackDone = true;
						}
						else
						{
							//other times, break when the track is done
							//(this check is safe because it's not the final track overall if it's not the final track in a file)
							if (curr_blobMSF >= TrackInfos[t + 1].CompiledCueTrack.Indexes[0].FileMSF.Sector)
								trackDone = true;
						}

						if (trackDone)
							break;
					}

					//---------------------------------
					//gen postgap sectors
					int specifiedPostgapLength = cct.PostgapLength.Sector;
					for (int s = 0; s < specifiedPostgapLength; s++)
					{
						//TODO - do a better job synthesizing Q
						var se_pregap = new SectorEntry(null);
						var ss_pregap = new SS_Gap();

						//postgaps set pause flag. is this good enough?
						ss_pregap.Pause = true;

						se_pregap.SectorSynth = ss_pregap;
						OUT_Disc.Sectors.Add(se_pregap);
					}


				} //end track loop


				//add RawTOCEntries A0 A1 A2 to round out the TOC
				var TOCMiscInfo = new Synthesize_A0A1A2_Job { 
				  IN_FirstRecordedTrackNumber = IN_CompileJob.OUT_CompiledDiscInfo.FirstRecordedTrackNumber,
					IN_LastRecordedTrackNumber = IN_CompileJob.OUT_CompiledDiscInfo.LastRecordedTrackNumber,
					IN_Session1Format = IN_CompileJob.OUT_CompiledDiscInfo.SessionFormat,
				  IN_LeadoutTimestamp = new Timestamp(OUT_Disc.Sectors.Count) //do we need a +150?
				};
				TOCMiscInfo.Run(OUT_Disc.RawTOCEntries);

				//generate the TOCRaw from the RawTocEntries
				var tocSynth = new DiscTOCRaw.SynthesizeFromRawTOCEntriesJob() { Entries = OUT_Disc.RawTOCEntries };
				tocSynth.Run();
				OUT_Disc.TOCRaw = tocSynth.Result;

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




//TODO:
				//if (index_num == 0 || type == SectorWriteType.Pregap)
				//{
				//  //PREGAP:
				//  //things are negative here.
				//  if (track_relative_msf > 0) throw new InvalidOperationException("Perplexing internal error with non-negative pregap MSF");
				//  track_relative_msf = -track_relative_msf;

				//  //now for something special.
				//  //yellow-book says:
				//  //pre-gap for "first part of a digital data track not containing user data and encoded as a pause"
				//  //first interval: at least 75 sectors coded as preceding track
				//  //second interval: at least 150 sectors coded as user data track.
				//  //so... we ASSUME the 150 sector pregap is more important. so if thats all there is, theres no 75 sector pregap like the old track
				//  //if theres a longer pregap, then we generate weird old track pregap to contain the rest.
				//  if (track_relative_msf > 150)
				//  {
				//    //only if we're burning a data track now
				//    if((track_flags & CueFile.TrackFlags.DATA)!=0)
				//      sq.q_status = priorSubchannelQ.q_status;
				//  }
				//}