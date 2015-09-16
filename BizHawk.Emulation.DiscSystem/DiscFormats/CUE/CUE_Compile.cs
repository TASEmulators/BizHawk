using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

//this would be a good place for structural validation
//after this step, we won't want to have to do stuff like that (it will gunk up already sticky code)

namespace BizHawk.Emulation.DiscSystem.CUE
{
	internal class CompiledCDText
	{
		public string Songwriter;
		public string Performer;
		public string Title;
		public string ISRC;
	}

	internal class CompiledCueIndex
	{
		public int Number;

		/// <summary>
		/// this is annoying, it should just be an integer
		/// </summary>
		public Timestamp FileMSF;

		public override string ToString()
		{
			return string.Format("I#{0:D2} {1}", Number, FileMSF);
		}
	}

	/// <summary>
	/// What type of file we're looking at.. each one would require a different ingestion handler
	/// </summary>
	public enum CompiledCueFileType
	{
		Unknown,

		/// <summary>
		/// a raw BIN that can be mounted directly
		/// </summary>
		BIN,

		/// <summary>
		/// a raw WAV that can be mounted directly
		/// </summary>
		WAVE,

		/// <summary>
		/// an ECM file that can be mounted directly (once the index is generated)
		/// </summary>
		ECM,

		/// <summary>
		/// An encoded audio file which can be seeked on the fly, therefore roughly mounted on the fly
		/// THIS ISN'T SUPPORTED YET
		/// </summary>
		SeekAudio,

		/// <summary>
		/// An encoded audio file which can't be seeked on the fly. It must be decoded to a temp buffer, or pre-discohawked
		/// </summary>
		DecodeAudio,
	}

	internal class CompiledCueFile
	{
		public string FullPath;
		public CompiledCueFileType Type;
		public override string ToString()
		{
			return string.Format("{0}: {1}", Type, Path.GetFileName(FullPath));
		}
	}

	internal class CompiledDiscInfo
	{
		public int FirstRecordedTrackNumber, LastRecordedTrackNumber;
		public SessionFormat SessionFormat;
	}

	internal class CompiledCueTrack
	{
		public int BlobIndex;
		public int Number;
			
		/// <summary>
		/// A track that's final in a file gets its length from the length of the file; other tracks lengths are determined from the succeeding track
		/// </summary>
		public bool IsFinalInFile;

		/// <summary>
		/// A track that's first in a file has an implicit index 0 at 00:00:00
		/// Otherwise it has an implicit index 0 at the placement of the index 1
		/// </summary>
		public bool IsFirstInFile;

		public CompiledCDText CDTextData = new CompiledCDText();
		public Timestamp PregapLength, PostgapLength;
		public CueTrackFlags Flags = CueTrackFlags.None;
		public CueTrackType TrackType = CueTrackType.Unknown;

		public List<CompiledCueIndex> Indexes = new List<CompiledCueIndex>();

		public override string ToString()
		{
			var idx = Indexes.Find((i) => i.Number == 1);
			if (idx == null)
				return string.Format("T#{0:D2} NO INDEX 1", Number);
			else
			{
				var indexlist = string.Join("|", Indexes);
				return string.Format("T#{0:D2} {1}:{2} ({3})", Number, BlobIndex, idx.FileMSF, indexlist);
			}
		}
	}

	internal class CompileCueJob : DiscJob
	{
		/// <summary>
		/// input: the CueFile to analyze
		/// </summary>
		public CUE_File IN_CueFile;

		/// <summary>
		/// The context used for this compiling job
		/// TODO - rename something like context
		/// </summary>
		public CUE_Context IN_CueContext;

		/// <summary>
		/// output: high level disc info
		/// </summary>
		public CompiledDiscInfo OUT_CompiledDiscInfo;

		/// <summary>
		/// output: CD-Text set at the global level (before any track commands)
		/// </summary>
		public CompiledCDText OUT_GlobalCDText;

		/// <summary>
		/// output: The compiled file info
		/// </summary>
		public List<CompiledCueFile> OUT_CompiledCueFiles;

		/// <summary>
		/// output: The compiled track info
		/// </summary>
		public List<CompiledCueTrack> OUT_CompiledCueTracks;

		/// <summary>
		/// output: An integer between 0 and 10 indicating how costly it will be to load this disc completely.
		/// Activites like decoding non-seekable media will increase the load time.
		/// 0 - Requires no noticeable time
		/// 1 - Requires minimal processing (indexing ECM)
		/// 10 - Requires ages, decoding audio data, etc.
		/// </summary>
		public int OUT_LoadTime;

		//-----------------------------------------------------------------

		CompiledCDText curr_cdtext;
		int curr_blobIndex = -1;
		CompiledCueTrack curr_track = null;
		CompiledCueFile curr_file = null;
		bool discinfo_session1Format_determined = false;
		bool curr_fileHasTrack = false;

		void UpdateDiscInfo(CUE_File.Command.TRACK trackCommand)
		{
			if (OUT_CompiledDiscInfo.FirstRecordedTrackNumber == 0)
				OUT_CompiledDiscInfo.FirstRecordedTrackNumber = trackCommand.Number;
			OUT_CompiledDiscInfo.LastRecordedTrackNumber = trackCommand.Number;
			if (!discinfo_session1Format_determined)
			{
				switch (trackCommand.Type)
				{
					case CueTrackType.Mode2_2336:
					case CueTrackType.Mode2_2352:
						OUT_CompiledDiscInfo.SessionFormat = SessionFormat.Type20_CDXA;
						discinfo_session1Format_determined = true;
						break;

					case CueTrackType.CDI_2336:
					case CueTrackType.CDI_2352:
						OUT_CompiledDiscInfo.SessionFormat = SessionFormat.Type10_CDI;
						discinfo_session1Format_determined = true;
						break;

					default:
						break;
				}
			}
		}

		void CloseFile()
		{
			if (curr_track != null)
			{
				//flag this track as the final one in the file
				curr_track.IsFinalInFile = true;
			}

			curr_file = null;
		}

		void OpenFile(CUE_File.Command.FILE f)
		{
			if (curr_file != null)
				CloseFile();

			curr_blobIndex++;
			curr_fileHasTrack = false;

			var Resolver = IN_CueContext.Resolver;

			//TODO - smart audio file resolving only for AUDIO types. not BINARY or MOTOROLA or AIFF or ECM or what have you

			var options = Resolver.Resolve(f.Path);
			string choice = null;
			if (options.Count == 0)
			{
				Error(string.Format("Couldn't resolve referenced cue file: {0} ; you can commonly repair the cue file yourself, or a file might be missing", f.Path));
				//add a null entry to keep the count from being wrong later (quiets a warning)
				OUT_CompiledCueFiles.Add(null);
				return;
			}
			else
			{
				choice = options[0];
				if (options.Count > 1)
					Warn("Multiple options resolving referenced cue file; choosing: " + Path.GetFileName(choice));
			}

			var cfi = new CompiledCueFile();
			curr_file = cfi;
			OUT_CompiledCueFiles.Add(cfi);

			cfi.FullPath = choice;

			//determine the CueFileInfo's type, based on extension and extra checking
			//TODO - once we reorganize the file ID stuff, do legit checks here (this is completely redundant with the fileID system
			//TODO - decode vs stream vs unpossible policies in input policies object (including ffmpeg availability-checking callback (results can be cached))
			string blobPathExt = Path.GetExtension(choice).ToUpperInvariant();
			if (blobPathExt == ".BIN" || blobPathExt == ".IMG") cfi.Type = CompiledCueFileType.BIN;
			else if (blobPathExt == ".ISO") cfi.Type = CompiledCueFileType.BIN;
			else if (blobPathExt == ".WAV")
			{
				//quickly, check the format. turn it to DecodeAudio if it can't be supported
				//TODO - fix exception-throwing inside
				//TODO - verify stream-disposing semantics
				var fs = File.OpenRead(choice);
				using (var blob = new Disc.Blob_WaveFile())
				{
					try
					{
						blob.Load(fs);
						cfi.Type = CompiledCueFileType.WAVE;
					}
					catch
					{
						cfi.Type = CompiledCueFileType.DecodeAudio;
					}
				}
			}
			else if (blobPathExt == ".APE") cfi.Type = CompiledCueFileType.DecodeAudio;
			else if (blobPathExt == ".MP3") cfi.Type = CompiledCueFileType.DecodeAudio;
			else if (blobPathExt == ".MPC") cfi.Type = CompiledCueFileType.DecodeAudio;
			else if (blobPathExt == ".FLAC") cfi.Type = CompiledCueFileType.DecodeAudio;
			else if (blobPathExt == ".ECM")
			{
				cfi.Type = CompiledCueFileType.ECM;
				if (!Disc.Blob_ECM.IsECM(choice))
				{
					Error("an ECM file was specified or detected, but it isn't a valid ECM file: " + Path.GetFileName(choice));
					cfi.Type = CompiledCueFileType.Unknown;
				}
			}
			else
			{
				Error("Unknown cue file type. Since it's likely an unsupported compression, this is an error: ", Path.GetFileName(choice));
				cfi.Type = CompiledCueFileType.Unknown;
			}

			//TODO - check for mismatches between track types and file types, or is that best done when interpreting the commands?
		}

		void CreateTrack1Pregap()
		{
			if (OUT_CompiledCueTracks[1].PregapLength.Sector == 0) { }
			else if (OUT_CompiledCueTracks[1].PregapLength.Sector == 150) { }
			else
			{
				Error("Track 1 specified an illegal pregap. It's being ignored and replaced with a 00:02:00 pregap");
			}
			OUT_CompiledCueTracks[1].PregapLength = new Timestamp(150);
		}

		void FinalAnalysis()
		{
			//some quick checks:
			if (OUT_CompiledCueFiles.Count == 0)
				Error("Cue file doesn't specify any input files!");

			//we can't reliably analyze the length of files here, because we might have to be decoding to get lengths (VBR mp3s)
			//REMINDER: we could actually scan the mp3 frames in software
			//So, it's not really worth the trouble. We'll cope with lengths later
			//we could check the format of the wav file here, though

			//score the cost of loading the file
			bool needsCodec = false;
			OUT_LoadTime = 0;
			foreach (var cfi in OUT_CompiledCueFiles)
			{
				if (cfi == null)
					continue;
				if (cfi.Type == CompiledCueFileType.DecodeAudio)
				{
					needsCodec = true;
					OUT_LoadTime = Math.Max(OUT_LoadTime, 10);
				}
				if (cfi.Type == CompiledCueFileType.SeekAudio)
					needsCodec = true;
				if (cfi.Type == CompiledCueFileType.ECM)
					OUT_LoadTime = Math.Max(OUT_LoadTime, 1);
			}

			//check whether processing was available
			if (needsCodec)
			{
				FFMpeg ffmpeg = new FFMpeg();
				if (!ffmpeg.QueryServiceAvailable())
					Warn("Decoding service will be required for further processing, but is not available");
			}
		}


		void CloseTrack()
		{
			if (curr_track == null)
				return;
				
			//normalize: if an index 0 is missing, add it here
			if (curr_track.Indexes[0].Number != 0)
			{
				var index0 = new CompiledCueIndex();
				var index1 = curr_track.Indexes[0];
				index0.Number = 0;
				index0.FileMSF = index1.FileMSF; //same MSF as index 1 will make it effectively nonexistent

				//well now, if it's the first in the file, an implicit index will take its value from 00:00:00 in the file
				//this is the kind of thing I sought to solve originally by 'interpreting' the file, but it seems easy enough to handle this way
				//my carlin.cue tests this but test cases shouldnt be hard to find
				if (curr_track.IsFirstInFile)
					index0.FileMSF = new Timestamp(0);

				curr_track.Indexes.Insert(0, index0);
			}

			OUT_CompiledCueTracks.Add(curr_track);
			curr_track = null;
		}

		void OpenTrack(CUE_File.Command.TRACK trackCommand)
		{
			//assert that a file is open
			if(curr_file == null)
			{
				Error("Track command encountered with no active file");
				throw new DiscJobAbortException();
			}

			curr_track = new CompiledCueTrack();

			//spill cdtext data into this track
			curr_cdtext = curr_track.CDTextData;
		
			curr_track.BlobIndex = curr_blobIndex;
			curr_track.Number = trackCommand.Number;
			curr_track.TrackType = trackCommand.Type;

			//default flags
			if (curr_track.TrackType != CueTrackType.Audio)
				curr_track.Flags = CueTrackFlags.DATA;

			if (!curr_fileHasTrack)
			{
				curr_fileHasTrack = curr_track.IsFirstInFile = true;
			}

			UpdateDiscInfo(trackCommand);
		}

		void AddIndex(CUE_File.Command.INDEX indexCommand)
		{
			var newindex = new CompiledCueIndex();
			newindex.FileMSF = indexCommand.Timestamp;
			newindex.Number = indexCommand.Number;
			curr_track.Indexes.Add(newindex);
		}

		public void Run()
		{
			//in params
			var cue = IN_CueFile;

			//output state
			OUT_GlobalCDText = new CompiledCDText();
			OUT_CompiledDiscInfo = new CompiledDiscInfo();
			OUT_CompiledCueFiles = new List<CompiledCueFile>();
			OUT_CompiledCueTracks = new List<CompiledCueTrack>();

			//add a track 0, for addressing convenience. 
			//note: for future work, track 0 may need emulation (accessible by very negative LBA--the TOC is stored there)
			var track0 = new CompiledCueTrack() {
				Number = 0,
			};
			OUT_CompiledCueTracks.Add(track0); 

			//global cd text will acquire the cdtext commands set before track commands
			curr_cdtext  = OUT_GlobalCDText;

			for (int i = 0; i < cue.Commands.Count; i++)
			{
				var cmd = cue.Commands[i];

				//these commands get dealt with globally. nothing to be done here
				//(but in the future we need to accumulate them into the compile pass output)
				if (cmd is CUE_File.Command.CATALOG || cmd is CUE_File.Command.CDTEXTFILE) continue;

				//nothing to be done for comments
				if (cmd is CUE_File.Command.REM) continue;
				if (cmd is CUE_File.Command.COMMENT) continue;

				//CD-text and related
				if (cmd is CUE_File.Command.PERFORMER) curr_cdtext.Performer = (cmd as CUE_File.Command.PERFORMER).Value;
				if (cmd is CUE_File.Command.SONGWRITER) curr_cdtext.Songwriter = (cmd as CUE_File.Command.SONGWRITER).Value;
				if (cmd is CUE_File.Command.TITLE) curr_cdtext.Title = (cmd as CUE_File.Command.TITLE).Value;
				if (cmd is CUE_File.Command.ISRC) curr_cdtext.ISRC = (cmd as CUE_File.Command.ISRC).Value;

				//flags can only be set when a track command is running
				if (cmd is CUE_File.Command.FLAGS)
				{
					if (curr_track == null)
						Warn("Ignoring invalid flag commands outside of a track command");
					else
						//take care to |= it here, so the data flag doesn't get cleared
						curr_track.Flags |= (cmd as CUE_File.Command.FLAGS).Flags;
				}

				if (cmd is CUE_File.Command.TRACK)
				{
					CloseTrack();
					OpenTrack(cmd as CUE_File.Command.TRACK);
				}

				if (cmd is CUE_File.Command.FILE)
				{
					CloseFile();
					OpenFile(cmd as CUE_File.Command.FILE);
				}

				if (cmd is CUE_File.Command.INDEX)
				{
					//todo - validate no postgap specified
					AddIndex(cmd as CUE_File.Command.INDEX);
				}

				if (cmd is CUE_File.Command.PREGAP)
				{
					//validate track open
					//validate no indexes
					curr_track.PregapLength = (cmd as CUE_File.Command.PREGAP).Length;
				}

				if (cmd is CUE_File.Command.POSTGAP)
				{
					curr_track.PostgapLength = (cmd as CUE_File.Command.POSTGAP).Length;
				}

			}

			//it's a bit odd to close the file before closing the track, but...
			//we need to be sure to CloseFile first to make sure the track is marked as the final one in the file
			CloseFile();
			CloseTrack();

			CreateTrack1Pregap();
			FinalAnalysis();

			FinishLog();
			
		} //Run()

	} //class CompileCueJob

} //namespace BizHawk.Emulation.DiscSystem