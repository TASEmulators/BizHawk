//TODO:
//"The first index of a file must start at 00:00:00" - if this isn't the case, we'll be doing nonsense for sure. so catch it
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

using System.IO;
using System.Collections.Generic;
using BizHawk.Common;

namespace BizHawk.Emulation.DiscSystem.CUE
{
	/// <summary>
	/// Loads a cue file into a Disc.
	/// For this job, virtually all nonsense input is treated as errors, but the process will try to recover as best it can.
	/// The user should still reject any jobs which generated errors
	/// </summary>
	internal class LoadCueJob : DiscJob
	{
		private readonly CompileCueJob IN_CompileJob;

		/// <param name="compileJob">The results of the compile job, a prerequisite for this</param>
		public LoadCueJob(CompileCueJob compileJob) => IN_CompileJob = compileJob;

		/// <summary>
		/// The resulting disc
		/// </summary>
		public Disc OUT_Disc { get; private set; }

		private enum BurnType
		{
			Normal, Pregap, Postgap
		}

		private class BlobInfo
		{
			public IBlob Blob;
			public long Length;
		}

		/// <remarks>not sure if we need this...</remarks>
		private readonly struct TrackInfo
		{
			public readonly CompiledCueTrack CompiledCueTrack;

#if true
			public TrackInfo(CompiledCueTrack compiledCueTrack) => CompiledCueTrack = compiledCueTrack;
#else
			public readonly int Length;

			public TrackInfo(CompiledCueTrack compiledCueTrack, int length)
			{
				CompiledCueTrack = compiledCueTrack;
				Length = length;
			}
#endif
		}

		private List<BlobInfo> BlobInfos;
		private readonly List<TrackInfo> TrackInfos = new();

		private void MountBlobs()
		{
			BlobInfos = new();
			foreach (var ccf in IN_CompileJob.OUT_CompiledCueFiles)
			{
				var bi = new BlobInfo();
				BlobInfos.Add(bi);

				IBlob file_blob;
				switch (ccf.Type)
				{
					case CompiledCueFileType.BIN:
					case CompiledCueFileType.Unknown:
						{
							//raw files:
							var blob = new Blob_RawFile { PhysicalPath = ccf.FullPath };
							OUT_Disc.DisposableResources.Add(file_blob = blob);
							bi.Length = blob.Length;
							break;
						}
					case CompiledCueFileType.ECM:
						{
							var blob = new Blob_ECM();
							OUT_Disc.DisposableResources.Add(file_blob = blob);
							blob.Load(ccf.FullPath);
							bi.Length = blob.Length;
							break;
						}
					case CompiledCueFileType.WAVE:
						{
							var blob = new Blob_WaveFile();
							OUT_Disc.DisposableResources.Add(file_blob = blob);
							blob.Load(ccf.FullPath);
							bi.Length = blob.Length;
							break;
						}
					case CompiledCueFileType.DecodeAudio:
						{
							if (!FFmpegService.QueryServiceAvailable())
							{
								throw new DiscReferenceException(ccf.FullPath, "No decoding service was available (make sure ffmpeg.exe is available. Even though this may be a wav, ffmpeg is used to load oddly formatted wave files. If you object to this, please send us a note and we'll see what we can do. It shouldn't be too hard.)");
							}
							AudioDecoder dec = new();
							var buf = dec.AcquireWaveData(ccf.FullPath);
							var blob = new Blob_WaveFile();
							OUT_Disc.DisposableResources.Add(file_blob = blob);
							blob.Load(new MemoryStream(buf));
							bi.Length = buf.Length;
							break;
						}
					default:
						throw new InvalidOperationException();
				}

				//wrap all the blobs with zero padding
				bi.Blob = new Blob_ZeroPadAdapter(file_blob, bi.Length);
			}
		}

		private void AnalyzeTracks()
		{
			var compiledTracks = IN_CompileJob.OUT_CompiledCueTracks;

			foreach (var cct in compiledTracks)
			{
				TrackInfos.Add(new(cct));

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

		private DiscSession CurrentSession = new() { Number = 1 };

		private void EmitRawTOCEntry(CompiledCueTrack cct)
		{
			SubchannelQ toc_sq = default;
			//absent some kind of policy for how to set it, this is a safe assumption:
			const byte toc_ADR = 1;
			toc_sq.SetStatus(toc_ADR, (EControlQ)(int)cct.Flags);
			toc_sq.q_tno.BCDValue = 0; //kind of a little weird here.. the track number becomes the 'point' and put in the index instead. 0 is the track number here.
			toc_sq.q_index = BCD2.FromDecimal(cct.Number);
			//not too sure about these yet
			toc_sq.min = BCD2.FromDecimal(0);
			toc_sq.sec = BCD2.FromDecimal(0);
			toc_sq.frame = BCD2.FromDecimal(0);
			toc_sq.AP_Timestamp = OUT_Disc._Sectors.Count;
			CurrentSession.RawTOCEntries.Add(new() { QData = toc_sq });
		}

		private void CloseSession()
		{
			//add RawTOCEntries A0 A1 A2 to round out the TOC
			var sessionInfo = IN_CompileJob.OUT_CompiledSessionInfo[CurrentSession.Number];
			var TOCMiscInfo = new Synthesize_A0A1A2_Job(
				firstRecordedTrackNumber: sessionInfo.FirstRecordedTrackNumber,
				lastRecordedTrackNumber: sessionInfo.LastRecordedTrackNumber,
				sessionFormat: sessionInfo.SessionFormat,
				leadoutTimestamp: OUT_Disc._Sectors.Count);

			TOCMiscInfo.Run(CurrentSession.RawTOCEntries);
			OUT_Disc.Sessions.Add(CurrentSession);
			CurrentSession = null;
		}

		public override void Run()
		{
			//params
			var context = IN_CompileJob.IN_CueContext;
			OUT_Disc = new();

			//generation state
			var curr_blobIndex = -1;
			var curr_blobMSF = -1;
			BlobInfo curr_blobInfo = null;
			long curr_blobOffset = -1;

			//mount all input files
			MountBlobs();

			//unhappily, we cannot determine the length of all the tracks without knowing the length of the files
			//now that the files are mounted, we can figure the track lengths
			AnalyzeTracks();

			//loop from track 1 to 99
			//(track 0 isnt handled yet, that's way distant work)
			for (var t = 1; t < TrackInfos.Count; t++)
			{
				var ti = TrackInfos[t];
				var cct = ti.CompiledCueTrack;

				if (CurrentSession.Number != cct.Session)
				{
					if (cct.Session != CurrentSession.Number + 1)
					{
						// error in case a session isn't just incrementing
						// maybe we can allow this, but that ends up being complicated here
						// not like any actual rip is going to have nonsequential sessions
						// edit: actually this is enforced parsing side, so this is probably silly here
						throw new InvalidOperationException("Track session change was not incremental");
					}

					CloseSession();
					CurrentSession = new()
					{
						Number = cct.Session
					};
				}

				//---------------------------------
				//setup track pregap processing
				//per "Example 05" on digitalx.org, pregap can come from index specification and pregap command
				var specifiedPregapLength = cct.PregapLength.Sector;
				var impliedPregapLength = cct.Indexes[1].FileMSF.Sector - cct.Indexes[0].FileMSF.Sector;
				var totalPregapLength = specifiedPregapLength + impliedPregapLength;

				//from now on we'll track relative timestamp and increment it continually
				var relMSF = -totalPregapLength;

				//read more at policies declaration
				//if (!context.DiscMountPolicy.CUE_PauseContradictionModeA)
				//  relMSF += 1;
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
				var curr_index = 0;
				while (true)
				{
					var trackDone = false;
					var generateGap = false;

					if (specifiedPregapLength > 0)
					{
						//if burning through a specified pregap, count it down
						generateGap = true;
						specifiedPregapLength--;
					}
					else
					{
						//if burning through the file, select the appropriate index by inspecting the next index and seeing if we've reached it
						while (true)
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
					}

					//select the track type for the subQ
					//it's obviously the same as the main track type usually, but during a pregap it can be different
					var qTrack = ti;
					var qRelMSF = relMSF;
					if (curr_index == 0)
					{
						//tweak relMSF due to ambiguity/contradiction in yellowbook docs
						if (!context.DiscMountPolicy.CUE_PregapContradictionModeA)
							qRelMSF++;

						//[IEC10149] says there's two "intervals" of a pregap.
						//mednafen's pseudocode interpretation of this:
						//if this is a data track and the previous track was not data, the last 150 sectors of the pregap match this track and the earlier sectors (at least 75) math the previous track
						//I agree, so let's do it that way
						if (t != 1 && cct.TrackType != CueTrackType.Audio && TrackInfos[t - 1].CompiledCueTrack.TrackType == CueTrackType.Audio)
						{
							if (relMSF < -150)
							{
								qTrack = TrackInfos[t - 1];
							}
						}
					}

					//generate the right kind of sector synth for this track
					SS_Base ss;
					if (generateGap)
					{
						ss = new SS_Gap { TrackType = qTrack.CompiledCueTrack.TrackType };
					}
					else
					{
						int sectorSize;
						switch (qTrack.CompiledCueTrack.TrackType)
						{
							case CueTrackType.Audio:
							case CueTrackType.CDI_2352:
							case CueTrackType.Mode1_2352:
							case CueTrackType.Mode2_2352:
								ss = new SS_2352();
								sectorSize = 2352;
								break;

							case CueTrackType.Mode1_2048:
								ss = new SS_Mode1_2048();
								sectorSize = 2048;
								break;

							case CueTrackType.CDI_2336:
							case CueTrackType.Mode2_2336:
								ss = new SS_Mode2_2336();
								sectorSize = 2336;
								break;

							default:
								throw new InvalidOperationException($"Not supported: {cct.TrackType}");
						}

						ss.Blob = curr_blobInfo!.Blob;
						ss.BlobOffset = curr_blobOffset;
						curr_blobOffset += sectorSize;
						curr_blobMSF++;
					}

					ss.Policy = context.DiscMountPolicy;

					//setup subQ
					const byte ADR = 1; //absent some kind of policy for how to set it, this is a safe assumption:
					ss.sq.SetStatus(ADR, (EControlQ)(int)qTrack.CompiledCueTrack.Flags);
					ss.sq.q_tno = BCD2.FromDecimal(cct.Number);
					ss.sq.q_index = BCD2.FromDecimal(curr_index);
					ss.sq.AP_Timestamp = OUT_Disc._Sectors.Count;
					ss.sq.Timestamp = qRelMSF;

					//setup subP
					if (curr_index == 0)
						ss.Pause = true;

					OUT_Disc._Sectors.Add(ss);
					relMSF++;

					if (cct.IsFinalInFile)
					{
						//sometimes, break when the file is exhausted
						if (curr_blobOffset >= curr_blobInfo!.Length)
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
				var specifiedPostgapLength = cct.PostgapLength.Sector;
				for (var s = 0; s < specifiedPostgapLength; s++)
				{
					var ss = new SS_Gap
					{
						TrackType = cct.TrackType // TODO - old track type in some < -150 cases?
					};

					//-subq-
					const byte ADR = 1;
					ss.sq.SetStatus(ADR, (EControlQ)(int)cct.Flags);
					ss.sq.q_tno = BCD2.FromDecimal(cct.Number);
					ss.sq.q_index = BCD2.FromDecimal(curr_index);
					ss.sq.AP_Timestamp = OUT_Disc._Sectors.Count;
					ss.sq.Timestamp = relMSF;

					//-subP-
					//always paused--is this good enough?
					ss.Pause = true;

					OUT_Disc._Sectors.Add(ss);
					relMSF++;
				}
			}

			CloseSession();

			//TODO - generate leadout, or delegates at least

			//blech, old crap, maybe
			//OUT_Disc.Structure.Synthesize_TOCPointsFromSessions();

			//FinishLog();
		}
	}
}