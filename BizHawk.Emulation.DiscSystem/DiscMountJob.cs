using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.DiscSystem.CUE;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// A Job interface for mounting discs.
	/// This is publicly exposed because it's the main point of control for fine-tuning disc loading options.
	/// This would typically be used to load discs.
	/// </summary>
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

		/// <summary>
		/// Whether a mount operation was aborted due to being too slow
		/// </summary>
		public bool OUT_SlowLoadAborted;

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
				OUT_Disc.Name = Path.GetFileName(IN_FromPath);

				//generate toc and structure:
				//1. TOCRaw from RawTOCEntries
				var tocSynth = new Synthesize_DiscTOC_From_RawTOCEntries_Job() { Entries = OUT_Disc.RawTOCEntries };
				tocSynth.Run();
				OUT_Disc.TOC = tocSynth.Result;
				//2. Structure from TOCRaw
				var structureSynth = new Synthesize_DiscStructure_From_DiscTOC_Job() { IN_Disc = OUT_Disc, TOCRaw = OUT_Disc.TOC };
				structureSynth.Run();
				OUT_Disc.Structure = structureSynth.Result;

				//insert a synth provider to take care of the leadout track
				//currently, we let mednafen take care of its own leadout track (we'll make that controllable later)
				if (IN_DiscInterface != DiscInterface.MednaDisc)
				{
					var ss_leadout = new SS_Leadout()
					{
						SessionNumber = 1,
						Policy = IN_DiscMountPolicy
					};
					Func<int, bool> condition = (int lba) => lba >= OUT_Disc.Session1.LeadoutLBA;
					new ConditionalSectorSynthProvider().Install(OUT_Disc, condition, ss_leadout);
				}

				//apply SBI if it exists
				var sbiPath = Path.ChangeExtension(IN_FromPath, ".sbi");
				if (File.Exists(sbiPath) && SBI.SBIFormat.QuickCheckISSBI(sbiPath))
				{
					var loadSbiJob = new SBI.LoadSBIJob() { IN_Path = sbiPath };
					loadSbiJob.Run();
					var applySbiJob = new ApplySBIJob();
					applySbiJob.Run(OUT_Disc, loadSbiJob.OUT_Data, IN_DiscMountPolicy.SBI_As_Mednafen);
				}
			}

			FinishLog();
		}

		void RunBizHawk()
		{
			string infile = IN_FromPath;
			string cue_content = null;

			var cfr = new CueFileResolver();

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
				//TODO - major renovation of error handling needed

				//TODO - make sure code is designed so no matter what happens, a disc is disposed in case of errors.
				//perhaps the CUE_Format2 (once renamed to something like Context) can handle that
				var cuePath = IN_FromPath;
				var cueContext = new CUE_Context();
				cueContext.DiscMountPolicy = IN_DiscMountPolicy;

				cueContext.Resolver = cfr;
				if (!cfr.IsHardcodedResolve) cfr.SetBaseDirectory(Path.GetDirectoryName(infile));

				//parse the cue file
				var parseJob = new ParseCueJob();
				if (cue_content == null)
					cue_content = File.ReadAllText(cuePath);
				parseJob.IN_CueString = cue_content;
				bool okParse = true;
				try { parseJob.Run(parseJob); }
				catch (DiscJobAbortException) { okParse = false; parseJob.FinishLog(); }
				if (!string.IsNullOrEmpty(parseJob.OUT_Log)) Console.WriteLine(parseJob.OUT_Log);
				ConcatenateJobLog(parseJob);
				if (!okParse)
					goto DONE;

				//compile the cue file:
				//includes this work: resolve required bin files and find out what it's gonna take to load the cue
				var compileJob = new CompileCueJob();
				compileJob.IN_CueContext = cueContext;
				compileJob.IN_CueFile = parseJob.OUT_CueFile;
				bool okCompile = true;
				try { compileJob.Run(); }
				catch (DiscJobAbortException) { okCompile = false; compileJob.FinishLog();  }
				if (!string.IsNullOrEmpty(compileJob.OUT_Log)) Console.WriteLine(compileJob.OUT_Log);
				ConcatenateJobLog(compileJob);
				if (!okCompile || compileJob.OUT_ErrorLevel)
					goto DONE;

				//check slow loading threshold
				if (compileJob.OUT_LoadTime > IN_SlowLoadAbortThreshold)
				{
					Warn("Loading terminated due to slow load threshold");
					OUT_SlowLoadAborted = true;
					goto DONE;
				}

				//actually load it all up
				var loadJob = new LoadCueJob();
				loadJob.IN_CompileJob = compileJob;
				loadJob.Run();
				//TODO - need better handling of log output
				if (!string.IsNullOrEmpty(loadJob.OUT_Log)) Console.WriteLine(loadJob.OUT_Log);
				ConcatenateJobLog(loadJob);

				OUT_Disc = loadJob.OUT_Disc;
				//OUT_Disc.DiscMountPolicy = IN_DiscMountPolicy; //NOT SURE WE NEED THIS (only makes sense for cue probably)
			}
			else if (ext == ".ccd")
			{
				CCD_Format ccdLoader = new CCD_Format();
				OUT_Disc = ccdLoader.LoadCCDToDisc(IN_FromPath, IN_DiscMountPolicy);
			}


		DONE:

			//setup the lowest level synth provider
			if (OUT_Disc != null)
			{
				var sssp = new ArraySectorSynthProvider()
				{
					Sectors = OUT_Disc._Sectors,
					FirstLBA = -150
				};
				OUT_Disc.SynthProvider = sssp;
			}
		}
	}
	
}