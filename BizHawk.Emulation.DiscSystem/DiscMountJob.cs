using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	
	
	public partial class DiscMountJob : DiscJob
	{
		/// <summary>
		/// The filename to be loaded
		/// </summary>
		public string IN_FromPath;

		/// <summary>
		/// Slow-loading cues won't finish loading if this threshold is exceeded.
		/// Set to 10 to always load a cue
		/// </summary>
		public int IN_SlowLoadAbortThreshold = 10;

		/// <summary>
		/// Cryptic policies to be used when mounting the disc.
		/// </summary>
		public DiscMountPolicy IN_DiscMountPolicy = new DiscMountPolicy();

		/// <summary>
		/// The interface to be used for loading the disc.
		/// Usually you'll want DiscInterface.BizHawk, but others can be used for A/B testing
		/// </summary>
		public DiscInterface IN_DiscInterface = DiscInterface.BizHawk;

		/// <summary>
		/// The resulting disc
		/// </summary>
		public Disc OUT_Disc;

		public void Run()
		{
			switch (IN_DiscInterface)
			{
				case DiscInterface.LibMirage:
					throw new NotSupportedException("LibMirage not supported yet");
				case DiscInterface.BizHawk:
					RunBizHawk();
					break;
				case DiscInterface.MednaDisc:
					RunMednaDisc();
					break;
			}

			if (OUT_Disc != null)
			{
				//generate toc and structure:
				//1. TOCRaw from RawTOCEntries
				var tocSynth = new Synthesize_DiscTOC_From_RawTOCEntries_Job() { Entries = OUT_Disc.RawTOCEntries };
				tocSynth.Run();
				OUT_Disc.TOC = tocSynth.Result;
				//2. Structure frmo TOCRaw
				var structureSynth = new Synthesize_DiscStructure_From_DiscTOC_Job() { IN_Disc = OUT_Disc, TOCRaw = OUT_Disc.TOC };
				structureSynth.Run();
				OUT_Disc.Structure = structureSynth.Result;
			}

			FinishLog();
		}

		void RunBizHawk()
		{
			string infile = IN_FromPath;
			string cue_content = null;

			var cfr = new CUE_Context.CueFileResolver();

		RERUN:
			var ext = Path.GetExtension(infile).ToLowerInvariant();

			if (ext == ".iso")
			{
				//make a fake cue file to represent this iso file and rerun it as a cue
				string filebase = Path.GetFileName(infile);
				cue_content = string.Format(@"
						FILE ""{0}"" BINARY
							TRACK 01 MODE1/2048
								INDEX 01 00:00:00",
					filebase);
				infile = Path.ChangeExtension(infile, ".cue");
				goto RERUN;
			}
			if (ext == ".cue")
			{
				//TODO - make sure code is designed so no matter what happens, a disc is disposed in case of errors.
				//perhaps the CUE_Format2 (once renamed to something like Context) can handle that
				var cuePath = IN_FromPath;
				var cue2 = new CUE_Context();
				cue2.DiscMountPolicy = IN_DiscMountPolicy;

				cue2.Resolver = cfr;
				if (!cfr.IsHardcodedResolve) cfr.SetBaseDirectory(Path.GetDirectoryName(infile));

				//parse the cue file
				var parseJob = new CUE_Context.ParseCueJob();
				if (cue_content == null)
					cue_content = File.ReadAllText(cuePath);
				parseJob.IN_CueString = cue_content;
				cue2.ParseCueFile(parseJob);
				//TODO - need better handling of log output
				if (!string.IsNullOrEmpty(parseJob.OUT_Log)) Console.WriteLine(parseJob.OUT_Log);
				ConcatenateJobLog(parseJob);

				//compile the cue file:
				//includes this work: resolve required bin files and find out what it's gonna take to load the cue
				var compileJob = new CUE_Context.CompileCueJob();
				compileJob.IN_CueFormat = cue2;
				compileJob.IN_CueFile = parseJob.OUT_CueFile;
				compileJob.Run();
				//TODO - need better handling of log output
				if (!string.IsNullOrEmpty(compileJob.OUT_Log)) Console.WriteLine(compileJob.OUT_Log);
				ConcatenateJobLog(compileJob);

				//check slow loading threshold
				if (compileJob.OUT_LoadTime >= IN_SlowLoadAbortThreshold)
				{
					Warn("Loading terminated due to slow load threshold");
					goto DONE;
				}

				//actually load it all up
				var loadJob = new CUE_Context.LoadCueJob();
				loadJob.IN_CompileJob = compileJob;
				loadJob.Run();
				//TODO - need better handling of log output
				if (!string.IsNullOrEmpty(loadJob.OUT_Log)) Console.WriteLine(loadJob.OUT_Log);
				ConcatenateJobLog(loadJob);

				OUT_Disc = loadJob.OUT_Disc;
				//OUT_Disc.DiscMountPolicy = IN_DiscMountPolicy; //NOT SURE WE NEED THIS (only makes sense for cue probably)

				//apply SBI if it exists (TODO - for formats other than cue?)
				var sbiPath = Path.ChangeExtension(IN_FromPath, ".sbi");
				if (File.Exists(sbiPath) && SBI.SBIFormat.QuickCheckISSBI(sbiPath))
				{
					var sbiJob = new SBI.LoadSBIJob();
					sbiJob.IN_Path = sbiPath;
					sbiJob.Run();
					OUT_Disc.ApplySBI(sbiJob.OUT_Data, IN_DiscMountPolicy.SBI_As_Mednafen);
				}
			}
			else if (ext == ".ccd")
			{
				CCD_Format ccdLoader = new CCD_Format();
				OUT_Disc = ccdLoader.LoadCCDToDisc(IN_FromPath);
			}

		DONE: ;
		}
	}
	
}