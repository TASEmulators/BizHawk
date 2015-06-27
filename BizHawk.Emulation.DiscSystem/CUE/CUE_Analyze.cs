using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	partial class CUE_Format2
	{
		public class AnalyzeCueJob : LoggedJob
		{
			/// <summary>
			/// input: the CueFile to analyze
			/// </summary>
			public CueFile IN_CueFile;

			/// <summary>
			/// An integer between 0 and 10 indicating how costly it will be to load this disc completely.
			/// Activites like decoding non-seekable media will increase the load time.
			/// 0 - Requires no noticeable time
			/// 1 - Requires minimal processing (indexing ECM)
			/// 10 - Requires ages, decoding audio data, etc.
			/// </summary>
			public int OUT_LoadTime;

			/// <summary>
			/// Analyzed-information about a file member of a cue
			/// </summary>
			public class CueFileInfo
			{
				public string FullPath;
				public CueFileType Type;
				public override string ToString()
				{
					return string.Format("{0}: {1}", Type, Path.GetFileName(FullPath));
				}
			}

			/// <summary>
			/// What type of file we're looking at.. each one would require a different ingestion handler
			/// </summary>
			public enum CueFileType
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

			/// <summary>
			/// For each file referenced by the cue file, info about it
			/// </summary>
			public List<CueFileInfo> OUT_FileInfos;
		}

		public class CueFileResolver
		{
			public bool caseSensitive = false;
			public bool IsHardcodedResolve { get; private set; }
			string baseDir;

			/// <summary>
			/// Retrieving the FullName from a FileInfo can be slow (and probably other operations), so this will cache all the needed values
			/// TODO - could we treat it like an actual cache and only fill the FullName if it's null?
			/// </summary>
			struct MyFileInfo
			{
				public string FullName;
				public FileInfo FileInfo;
			}

			DirectoryInfo diBasedir;
			MyFileInfo[] fisBaseDir;

			/// <summary>
			/// sets the base directory and caches the list of files in the directory
			/// </summary>
			public void SetBaseDirectory(string baseDir)
			{
				this.baseDir = baseDir;
				diBasedir = new DirectoryInfo(baseDir);
				//list all files, so we dont scan repeatedly.
				fisBaseDir = MyFileInfosFromFileInfos(diBasedir.GetFiles());
			}

			/// <summary>
			/// TODO - doesnt seem like we're using this...
			/// </summary>
			public void SetHardcodeResolve(IDictionary<string, string> hardcodes)
			{
				IsHardcodedResolve = true;
				fisBaseDir = new MyFileInfo[hardcodes.Count];
				int i = 0;
				foreach (var kvp in hardcodes)
				{
					fisBaseDir[i++] = new MyFileInfo { FullName = kvp.Key, FileInfo = new FileInfo(kvp.Value) };
				}
			}

			MyFileInfo[] MyFileInfosFromFileInfos(FileInfo[] fis)
			{
				var myfis = new MyFileInfo[fis.Length];
				for (int i = 0; i < fis.Length; i++)
				{
					myfis[i].FileInfo = fis[i];
					myfis[i].FullName = fis[i].FullName;
				}
				return myfis;
			}

			/// <summary>
			/// Performs cue-intelligent logic to acquire a file requested by the cue.
			/// Returns the resulting full path(s).
			/// If there are multiple options, it returns them all
			/// </summary>
			public List<string> Resolve(string path)
			{
				string targetFile = Path.GetFileName(path);
				string targetFragment = Path.GetFileNameWithoutExtension(path);

				DirectoryInfo di = null;
				MyFileInfo[] fileInfos;
				if (!string.IsNullOrEmpty(Path.GetDirectoryName(path)))
				{
					di = new FileInfo(path).Directory;
					//fileInfos = di.GetFiles(Path.GetFileNameWithoutExtension(path)); //does this work?
					fileInfos = MyFileInfosFromFileInfos(di.GetFiles()); //we (probably) have to enumerate all the files to do a search anyway, so might as well do this
					//TODO - dont do the search until a resolve fails
				}
				else
				{
					di = diBasedir;
					fileInfos = fisBaseDir;
				}

				var results = new List<FileInfo>();
				foreach (var fi in fileInfos)
				{
					var ext = Path.GetExtension(fi.FullName).ToLowerInvariant();
					
					//some choices are always bad: (we're looking for things like .bin and .wav)
					//it's a little unclear whether we should go for a whitelist or a blacklist here. 
					//there's similar numbers of cases either way.
					//perhaps we could code both (and prefer choices from the whitelist)
					if (ext == ".cue" || ext == ".sbi" || ext == ".ccd" || ext == ".sub")
						continue;


					string fragment = Path.GetFileNameWithoutExtension(fi.FullName);
					//match files with differing extensions
					int cmp = string.Compare(fragment, targetFragment, !caseSensitive);
					if (cmp != 0)
						//match files with another extension added on (likely to be mygame.bin.ecm)
						cmp = string.Compare(fragment, targetFile, !caseSensitive);
					if (cmp == 0)
						results.Add(fi.FileInfo);

				}
				var ret = new List<string>();
				foreach (var fi in results)
					ret.Add(fi.FullName);
				return ret;
			}
		}

		/// <summary>
		/// Analyzes a cue file and its dependencies.
		/// This should run as fast as possible.. no deep inspection of files
		/// </summary>
		public void AnalyzeCueFile(AnalyzeCueJob job)
		{
			//TODO - handle CD text file

			job.OUT_FileInfos = new List<AnalyzeCueJob.CueFileInfo>();

			var cue = job.IN_CueFile;

			//first, collect information about all the input files
			foreach (var cmd in cue.Commands)
			{
				var f = cmd as CueFile.Command.FILE;
				if (f == null) continue;

				//TODO TODO TODO TODO
				//TODO TODO TODO TODO
				//TODO TODO TODO TODO
				//smart audio file resolving only for AUDIO types. not BINARY or MOTOROLA or AIFF or ECM or what have you

				var options = Resolver.Resolve(f.Path);
				string choice = null;
				if (options.Count == 0)
				{
					job.Error("Couldn't resolve referenced cue file: " + f.Path);
					continue;
				}
				else
				{
					choice = options[0];
					if (options.Count > 1)
						job.Warn("Multiple options resolving referenced cue file; choosing: " + Path.GetFileName(choice));
				}

				var cfi = new AnalyzeCueJob.CueFileInfo();
				job.OUT_FileInfos.Add(cfi);

				cfi.FullPath = choice;

				//determine the CueFileInfo's type, based on extension and extra checking
				//TODO - once we reorganize the file ID stuff, do legit checks here (this is completely redundant with the fileID system
				//TODO - decode vs stream vs unpossible policies in input policies object (including ffmpeg availability-checking callback (results can be cached))
				string blobPathExt = Path.GetExtension(choice).ToUpperInvariant();
				if (blobPathExt == ".BIN" || blobPathExt == ".IMG") cfi.Type = AnalyzeCueJob.CueFileType.BIN;
				else if (blobPathExt == ".ISO") cfi.Type = AnalyzeCueJob.CueFileType.BIN;
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
							cfi.Type = AnalyzeCueJob.CueFileType.WAVE;
						}
						catch
						{
							cfi.Type = AnalyzeCueJob.CueFileType.DecodeAudio;
						}
					}
				}
				else if (blobPathExt == ".APE") cfi.Type = AnalyzeCueJob.CueFileType.DecodeAudio;
				else if (blobPathExt == ".MP3") cfi.Type = AnalyzeCueJob.CueFileType.DecodeAudio;
				else if (blobPathExt == ".MPC") cfi.Type = AnalyzeCueJob.CueFileType.DecodeAudio;
				else if (blobPathExt == ".FLAC") cfi.Type = AnalyzeCueJob.CueFileType.DecodeAudio;
				else if (blobPathExt == ".ECM")
				{
					cfi.Type = AnalyzeCueJob.CueFileType.ECM;
					if (!Disc.Blob_ECM.IsECM(choice))
					{
						job.Error("an ECM file was specified or detected, but it isn't a valid ECM file: " + Path.GetFileName(choice));
						cfi.Type = AnalyzeCueJob.CueFileType.Unknown;
					}
				}
				else
				{
					job.Error("Unknown cue file type. Since it's likely an unsupported compression, this is an error: ", Path.GetFileName(choice));
					cfi.Type = AnalyzeCueJob.CueFileType.Unknown;
				}
			}

			//TODO - check for mismatches between track types and file types, or is that best done when interpreting the commands?

			//some quick checks:
			if (job.OUT_FileInfos.Count == 0)
				job.Error("Cue file doesn't specify any input files!");

			//we can't readily analyze the length of files here, because we'd have to be interpreting the commands to know the track types. Not really worth the trouble
			//we could check the format of the wav file here, though

			//score the cost of loading the file
			bool needsCodec = false;
			job.OUT_LoadTime = 0;
			foreach (var cfi in job.OUT_FileInfos)
			{
				if (cfi.Type == AnalyzeCueJob.CueFileType.DecodeAudio)
				{
					needsCodec = true;
					job.OUT_LoadTime = Math.Max(job.OUT_LoadTime, 10);
				}
				if (cfi.Type == AnalyzeCueJob.CueFileType.SeekAudio)
					needsCodec = true;
				if (cfi.Type == AnalyzeCueJob.CueFileType.ECM)
					job.OUT_LoadTime = Math.Max(job.OUT_LoadTime, 1);
			}

			//check whether processing was available
			if (needsCodec)
			{
				FFMpeg ffmpeg = new FFMpeg();
				if (!ffmpeg.QueryServiceAvailable())
					job.Warn("Decoding service will be required for further processing, but is not available");
			}

			job.FinishLog();
		}
	} //partial class
} //namespace 