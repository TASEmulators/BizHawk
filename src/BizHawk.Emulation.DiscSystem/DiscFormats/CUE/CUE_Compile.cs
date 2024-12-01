using System.IO;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

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

	internal readonly struct CompiledCueIndex
	{
		/// <remarks>this is annoying, it should just be an integer</remarks>
		public readonly Timestamp FileMSF;

		public readonly int Number;

		public CompiledCueIndex(int number, Timestamp fileMSF)
		{
			Number = number;
			FileMSF = fileMSF;
		}

		public override string ToString() => $"I#{Number:D2} {FileMSF}";
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
			return $"{Type}: {Path.GetFileName(FullPath)}";
		}
	}

	internal class CompiledSessionInfo
	{
		public int FirstRecordedTrackNumber, LastRecordedTrackNumber;
		public SessionFormat SessionFormat;
	}

	internal class CompiledCueTrack
	{
		public int BlobIndex;
		public int Number;
		public int Session;
			
		/// <summary>
		/// A track that's final in a file gets its length from the length of the file; other tracks lengths are determined from the succeeding track
		/// </summary>
		public bool IsFinalInFile;

		/// <summary>
		/// A track that's first in a file has an implicit index 0 at 00:00:00
		/// Otherwise it has an implicit index 0 at the placement of the index 1
		/// </summary>
		public bool IsFirstInFile;

		public readonly CompiledCDText CDTextData = new();
		public Timestamp PregapLength, PostgapLength;
		public CueTrackFlags Flags = CueTrackFlags.None;
		public CueTrackType TrackType = CueTrackType.Unknown;

		public readonly IList<CompiledCueIndex> Indexes = new List<CompiledCueIndex>();

		public override string ToString()
		{
			var idx = Indexes.FirstOrNull(static cci => cci.Number is 1);
			if (idx is null) return $"T#{Number:D2} NO INDEX 1";
			var indexlist = string.Join("|", Indexes);
			return $"T#{Number:D2} {BlobIndex}:{idx.Value.FileMSF} ({indexlist})";
		}
	}

	internal class CompileCueJob : DiscJob
	{
		private readonly CUE_File IN_CueFile;

		internal readonly CUE_Context IN_CueContext;

		/// <param name="cueFile">the CueFile to analyze</param>
		/// <param name="cueContext">The context used for this compiling job</param>
		public CompileCueJob(CUE_File cueFile, CUE_Context cueContext)
		{
			IN_CueFile = cueFile;
			IN_CueContext = cueContext;
		}

		/// <summary>
		/// output: high level session info (most of the time, this only has 1 session)
		/// </summary>
		public List<CompiledSessionInfo> OUT_CompiledSessionInfo { get; private set; }

		/// <summary>
		/// output: CD-Text set at the global level (before any track commands)
		/// </summary>
		public CompiledCDText OUT_GlobalCDText { get; private set; }

		/// <summary>
		/// output: The compiled file info
		/// </summary>
		public List<CompiledCueFile> OUT_CompiledCueFiles { get; private set; }

		/// <summary>
		/// output: The compiled track info
		/// </summary>
		public List<CompiledCueTrack> OUT_CompiledCueTracks { get; private set; }

		/// <summary>
		/// output: An integer between 0 and 10 indicating how costly it will be to load this disc completely.
		/// Activites like decoding non-seekable media will increase the load time.
		/// 0 - Requires no noticeable time
		/// 1 - Requires minimal processing (indexing ECM)
		/// 10 - Requires ages, decoding audio data, etc.
		/// </summary>
		public int OUT_LoadTime { get; private set; }

		private CompiledCDText curr_cdtext;
		private int curr_blobIndex = -1;
		private int curr_session = 1;
		private CompiledCueTrack curr_track;
		private CompiledCueFile curr_file;
		private bool sessionFormatDetermined;
		private bool curr_fileHasTrack;

		private void UpdateDiscInfo(CUE_File.Command.TRACK trackCommand)
		{
			var sessionInfo = OUT_CompiledSessionInfo[curr_session];
			if (sessionInfo.FirstRecordedTrackNumber == 0)
				sessionInfo.FirstRecordedTrackNumber = trackCommand.Number;
			sessionInfo.LastRecordedTrackNumber = trackCommand.Number;
			if (!sessionFormatDetermined)
			{
				switch (trackCommand.Type)
				{
					case CueTrackType.Mode2_2336:
					case CueTrackType.Mode2_2352:
						sessionInfo.SessionFormat = SessionFormat.Type20_CDXA;
						sessionFormatDetermined = true;
						break;
					case CueTrackType.CDI_2336:
					case CueTrackType.CDI_2352:
						sessionInfo.SessionFormat = SessionFormat.Type10_CDI;
						sessionFormatDetermined = true;
						break;
				}
			}
		}

		private void CloseFile()
		{
			if (curr_track != null)
			{
				//flag this track as the final one in the file
				curr_track.IsFinalInFile = true;
			}

			curr_file = null;
		}

		private void OpenFile(CUE_File.Command.FILE f)
		{
			if (curr_file != null)
				CloseFile();

			curr_blobIndex++;
			curr_fileHasTrack = false;

			var Resolver = IN_CueContext.Resolver;

			//TODO - smart audio file resolving only for AUDIO types. not BINARY or MOTOROLA or AIFF or ECM or what have you

			var options = Resolver.Resolve(f.Path);
			if (options.Count == 0)
			{
				Error($"Couldn't resolve referenced cue file: {f.Path} ; you can commonly repair the cue file yourself, or a file might be missing");
				//add a null entry to keep the count from being wrong later (quiets a warning)
				OUT_CompiledCueFiles.Add(null);
				return;
			}

			var choice = options[0];
			if (options.Count > 1)
				Warn($"Multiple options resolving referenced cue file; choosing: {Path.GetFileName(choice)}");

			var cfi = new CompiledCueFile();
			curr_file = cfi;
			OUT_CompiledCueFiles.Add(cfi);

			cfi.FullPath = choice;

			//determine the CueFileInfo's type, based on extension and extra checking
			//TODO - once we reorganize the file ID stuff, do legit checks here (this is completely redundant with the fileID system
			//TODO - decode vs stream vs unpossible policies in input policies object (including ffmpeg availability-checking callback (results can be cached))
			var blobPathExt = Path.GetExtension(choice).ToUpperInvariant();
			switch (blobPathExt)
			{
				case ".BIN" or ".IMG" or ".RAW":
				case ".ISO":
					cfi.Type = CompiledCueFileType.BIN;
					break;
				case ".WAV":
				{
					//quickly, check the format. turn it to DecodeAudio if it can't be supported
					//TODO - fix exception-throwing inside
					//TODO - verify stream-disposing semantics
					var fs = File.OpenRead(choice);
					using var blob = new Blob_WaveFile();
					try
					{
						blob.Load(fs);
						cfi.Type = CompiledCueFileType.WAVE;
					}
					catch
					{
						cfi.Type = CompiledCueFileType.DecodeAudio;
					}

					break;
				}
				case ".APE":
				case ".MP3":
				case ".MPC":
				case ".FLAC":
					cfi.Type = CompiledCueFileType.DecodeAudio;
					break;
				case ".ECM":
				{
					cfi.Type = CompiledCueFileType.ECM;
					if (!Blob_ECM.IsECM(choice))
					{
						Error($"an ECM file was specified or detected, but it isn't a valid ECM file: {Path.GetFileName(choice)}");
						cfi.Type = CompiledCueFileType.Unknown;
					}

					break;
				}
				default:
					Error($"Unknown cue file type. Since it's likely an unsupported compression, this is an error: {Path.GetFileName(choice)}");
					cfi.Type = CompiledCueFileType.Unknown;
					break;
			}

			//TODO - check for mismatches between track types and file types, or is that best done when interpreting the commands?
		}

		private void CreateTrack1Pregap()
		{
			if (OUT_CompiledCueTracks[1].PregapLength.Sector is not (0 or 150)) Error("Track 1 specified an illegal pregap. It's being ignored and replaced with a 00:02:00 pregap");
			OUT_CompiledCueTracks[1].PregapLength = new(150);
		}

		private void FinalAnalysis()
		{
			//some quick checks:
			if (OUT_CompiledCueFiles.Count == 0)
				Error("Cue file doesn't specify any input files!");

			//we can't reliably analyze the length of files here, because we might have to be decoding to get lengths (VBR mp3s)
			//REMINDER: we could actually scan the mp3 frames in software
			//So, it's not really worth the trouble. We'll cope with lengths later
			//we could check the format of the wav file here, though

			//score the cost of loading the file
			var needsCodec = false;
			OUT_LoadTime = 0;
			foreach (var cfi in OUT_CompiledCueFiles.Where(cfi => cfi is not null))
			{
				switch (cfi.Type)
				{
					case CompiledCueFileType.DecodeAudio:
						needsCodec = true;
						OUT_LoadTime = Math.Max(OUT_LoadTime, 10);
						break;
					case CompiledCueFileType.SeekAudio:
						needsCodec = true;
						break;
					case CompiledCueFileType.ECM:
						OUT_LoadTime = Math.Max(OUT_LoadTime, 1);
						break;
				}
			}

			//check whether processing was available
			if (needsCodec)
			{
				if (!FFmpegService.QueryServiceAvailable()) Warn("Decoding service will be required for further processing, but is not available");
			}
		}

		private void CloseTrack()
		{
			if (curr_track == null)
				return;
				
			//normalize: if an index 0 is missing, add it here
			if (curr_track.Indexes[0].Number != 0)
			{
				//well now, if it's the first in the file, an implicit index will take its value from 00:00:00 in the file
				//this is the kind of thing I sought to solve originally by 'interpreting' the file, but it seems easy enough to handle this way
				//my carlin.cue tests this but test cases shouldn't be hard to find
				var fileMSF = curr_track.IsFirstInFile
					? new(0)
					: curr_track.Indexes[0].FileMSF; // else, same MSF as index 1 will make it effectively nonexistent

				curr_track.Indexes.Insert(0, new(0, fileMSF));
			}

			OUT_CompiledCueTracks.Add(curr_track);
			curr_track = null;
		}

		private void OpenTrack(CUE_File.Command.TRACK trackCommand)
		{
			//assert that a file is open
			if (curr_file == null)
			{
				Error("Track command encountered with no active file");
				throw new DiscJobAbortException();
			}

			curr_track = new();

			//spill cdtext data into this track
			curr_cdtext = curr_track.CDTextData;
		
			curr_track.BlobIndex = curr_blobIndex;
			curr_track.Session = curr_session;
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

		private void AddIndex(CUE_File.Command.INDEX indexCommand)
		{
			curr_track.Indexes.Add(new(indexCommand.Number, indexCommand.Timestamp));
		}

		public override void Run()
		{
			//output state
			OUT_GlobalCDText = new();
			OUT_CompiledCueFiles = new();
			OUT_CompiledCueTracks = new();

			//add a track 0, for addressing convenience.
			//note: for future work, track 0 may need emulation (accessible by very negative LBA--the TOC is stored there)
			var track0 = new CompiledCueTrack
			{
				Number = 0,
			};
			OUT_CompiledCueTracks.Add(track0);

			// similarly, session 0 is added as a null entry, with session 1 added in for the actual first entry
			OUT_CompiledSessionInfo = new() { null, new() };

			//global cd text will acquire the cdtext commands set before track commands
			curr_cdtext  = OUT_GlobalCDText;

			foreach (var cmd in IN_CueFile.Commands) switch (cmd)
			{
				case CUE_File.Command.CATALOG:
				case CUE_File.Command.CDTEXTFILE:
					// these commands get dealt with globally. nothing to be done here
					// (but in the future we need to accumulate them into the compile pass output)
					continue;
				case CUE_File.Command.REM:
				case CUE_File.Command.COMMENT:
					// nothing to be done for comments
					continue;
				case CUE_File.Command.PERFORMER performerCmd:
					curr_cdtext.Performer = performerCmd.Value;
					break;
				case CUE_File.Command.SONGWRITER songwriterCmd:
					curr_cdtext.Songwriter = songwriterCmd.Value;
					break;
				case CUE_File.Command.TITLE titleCmd:
					curr_cdtext.Title = titleCmd.Value;
					break;
				case CUE_File.Command.ISRC isrcCmd:
					curr_cdtext.ISRC = isrcCmd.Value;
					break;
				case CUE_File.Command.FLAGS flagsCmd:
					// flags can only be set when a track command is running
					if (curr_track == null) Warn("Ignoring invalid flag commands outside of a track command");
					else curr_track.Flags |= flagsCmd.Flags; // take care to |= it here, so the data flag doesn't get cleared
					break;
				case CUE_File.Command.SESSION session:
					if (session.Number == curr_session) break; // this may occur for SESSION 1 at the beginning, so we'll silence warnings from this
					if (session.Number != curr_session + 1) Warn("Ignoring non-sequential session commands"); // TODO: should this be allowed? doesn't make sense here...
					else
					{
						curr_session = session.Number;
						OUT_CompiledSessionInfo.Add(new());
						sessionFormatDetermined = false;
					}
					break;
				case CUE_File.Command.TRACK trackCmd:
					CloseTrack();
					OpenTrack(trackCmd);
					break;
				case CUE_File.Command.FILE fileCmd:
					CloseFile();
					OpenFile(fileCmd);
					break;
				case CUE_File.Command.INDEX indexCmd:
					//TODO validate no postgap specified
					AddIndex(indexCmd);
					break;
				case CUE_File.Command.PREGAP pregapCmd:
					//TODO validate track open
					//TODO validate no indexes
					curr_track.PregapLength = pregapCmd.Length;
					break;
				case CUE_File.Command.POSTGAP postgapCmd:
					curr_track.PostgapLength = postgapCmd.Length;
					break;
			}

			//it's a bit odd to close the file before closing the track, but...
			//we need to be sure to CloseFile first to make sure the track is marked as the final one in the file
			CloseFile();
			CloseTrack();

			CreateTrack1Pregap();
			FinalAnalysis();

			FinishLog();
		}

	}
}