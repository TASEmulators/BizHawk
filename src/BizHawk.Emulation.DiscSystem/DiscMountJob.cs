using System;
using System.IO;
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
		private readonly string IN_FromPath;

		private readonly DiscMountPolicy IN_DiscMountPolicy;

		private readonly DiscInterface IN_DiscInterface;

		private readonly int IN_SlowLoadAbortThreshold;

		/// <param name="fromPath">The filename to be loaded</param>
		/// <param name="discMountPolicy">Cryptic policies to be used when mounting the disc.</param>
		/// <param name="discInterface">
		/// The interface to be used for loading the disc.
		/// Usually you'll want DiscInterface.BizHawk, but others can be used for A/B testing
		/// </param>
		/// <param name="slowLoadAbortThreshold">
		/// Slow-loading cues won't finish loading if this threshold is exceeded.
		/// Set to 10 to always load a cue
		/// </param>
		public DiscMountJob(string fromPath, DiscMountPolicy discMountPolicy, DiscInterface discInterface = DiscInterface.BizHawk, int slowLoadAbortThreshold = 10)
		{
			IN_FromPath = fromPath;
			IN_DiscMountPolicy = discMountPolicy;
			IN_DiscInterface = discInterface;
			IN_SlowLoadAbortThreshold = slowLoadAbortThreshold;
		}

		/// <param name="fromPath">The filename to be loaded</param>
		/// <param name="discInterface">
		/// The interface to be used for loading the disc.
		/// Usually you'll want DiscInterface.BizHawk, but others can be used for A/B testing
		/// </param>
		/// <param name="slowLoadAbortThreshold">
		/// Slow-loading cues won't finish loading if this threshold is exceeded.
		/// Set to 10 to always load a cue
		/// </param>
		public DiscMountJob(string fromPath, DiscInterface discInterface = DiscInterface.BizHawk, int slowLoadAbortThreshold = 10)
			: this(fromPath, new(), discInterface, slowLoadAbortThreshold) {}

		/// <summary>
		/// The resulting disc
		/// </summary>
		public Disc OUT_Disc { get; private set; }

		/// <summary>
		/// Whether a mount operation was aborted due to being too slow
		/// </summary>
		public bool OUT_SlowLoadAborted { get; private set; }

		/// <exception cref="NotSupportedException"><see cref="IN_DiscInterface"/> is <see cref="DiscInterface.LibMirage"/></exception>
		public override void Run()
		{
			switch (IN_DiscInterface)
			{
				case DiscInterface.LibMirage:
					throw new NotSupportedException($"{nameof(DiscInterface.LibMirage)} not supported yet");
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
				var tocSynth = new Synthesize_DiscTOC_From_RawTOCEntries_Job(OUT_Disc.RawTOCEntries);
				tocSynth.Run();
				OUT_Disc.TOC = tocSynth.Result;
				//2. Structure from TOCRaw
				var structureSynth = new Synthesize_DiscStructure_From_DiscTOC_Job(OUT_Disc, OUT_Disc.TOC);
				structureSynth.Run();
				OUT_Disc.Structure = structureSynth.Result;

				//insert a synth provider to take care of the leadout track
				//currently, we let mednafen take care of its own leadout track (we'll make that controllable later)
				if (IN_DiscInterface != DiscInterface.MednaDisc)
				{
					var ss_leadout = new SS_Leadout
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
					var loadSbiJob = new SBI.LoadSBIJob(sbiPath);
					loadSbiJob.Run();
					var applySbiJob = new ApplySBIJob();
					applySbiJob.Run(OUT_Disc, loadSbiJob.OUT_Data, IN_DiscMountPolicy.SBI_As_Mednafen);
				}
			}

			FinishLog();
		}

		private void RunBizHawk()
		{
			void LoadCue(string cueDirPath, string cueContent)
			{
				//TODO major renovation of error handling needed

				//TODO make sure code is designed so no matter what happens, a disc is disposed in case of errors.
				// perhaps the CUE_Format2 (once renamed to something like Context) can handle that
				var cfr = new CueFileResolver();
				var cueContext = new CUE_Context { DiscMountPolicy = IN_DiscMountPolicy, Resolver = cfr };

				if (!cfr.IsHardcodedResolve) cfr.SetBaseDirectory(cueDirPath);

				// parse the cue file
				var parseJob = new ParseCueJob(cueContent);
				var okParse = true;
				try
				{
					parseJob.Run();
				}
				catch (DiscJobAbortException)
				{
					okParse = false;
					parseJob.FinishLog();
				}
				if (!string.IsNullOrEmpty(parseJob.OUT_Log)) Console.WriteLine(parseJob.OUT_Log);
				ConcatenateJobLog(parseJob);
				if (!okParse) return;

				// compile the cue file
				// includes resolving required bin files and finding out what would processing would need to happen in order to load the cue
				var compileJob = new CompileCueJob(parseJob.OUT_CueFile, cueContext);
				var okCompile = true;
				try
				{
					compileJob.Run();
				}
				catch (DiscJobAbortException)
				{
					okCompile = false;
					compileJob.FinishLog();
				}
				if (!string.IsNullOrEmpty(compileJob.OUT_Log)) Console.WriteLine(compileJob.OUT_Log);
				ConcatenateJobLog(compileJob);
				if (!okCompile || compileJob.OUT_ErrorLevel) return;

				// check slow loading threshold
				if (compileJob.OUT_LoadTime > IN_SlowLoadAbortThreshold)
				{
					Warn("Loading terminated due to slow load threshold");
					OUT_SlowLoadAborted = true;
					return;
				}

				// actually load it all up
				var loadJob = new LoadCueJob(compileJob);
				loadJob.Run();
				//TODO need better handling of log output
				if (!string.IsNullOrEmpty(loadJob.OUT_Log)) Console.WriteLine(loadJob.OUT_Log);
				ConcatenateJobLog(loadJob);

				OUT_Disc = loadJob.OUT_Disc;
//				OUT_Disc.DiscMountPolicy = IN_DiscMountPolicy; // NOT SURE WE NEED THIS (only makes sense for cue probably)
			}

			switch (Path.GetExtension(IN_FromPath).ToLowerInvariant())
			{
				case ".ccd":
					OUT_Disc = new CCD_Format().LoadCCDToDisc(IN_FromPath, IN_DiscMountPolicy);
					break;
				case ".cue":
					LoadCue(Path.GetDirectoryName(IN_FromPath), File.ReadAllText(IN_FromPath));
					break;
				case ".iso":
					{
						// make a fake .cue file to represent this .iso and mount that
						//however... to save many users from a stupid mistake, check if the size is NOT a multiple of 2048 (but IS a multiple of 2352) and in that case consider it a mode2 disc
						//TODO - try it both ways and check the disc type to use whichever one succeeds in identifying a disc type
						var len = new FileInfo(IN_FromPath).Length;
						string mode1cue = $@"
						FILE ""{Path.GetFileName(IN_FromPath)}"" BINARY
							TRACK 01 MODE1/2048
								INDEX 01 00:00:00";
						string mode2cue = $@"
						FILE ""{Path.GetFileName(IN_FromPath)}"" BINARY
							TRACK 01 MODE2/2352
								INDEX 01 00:00:00";
						if (len % 2048 != 0 && len % 2352 == 0)
							LoadCue(Path.GetDirectoryName(IN_FromPath), mode2cue);
						else
							LoadCue(Path.GetDirectoryName(IN_FromPath), mode1cue);
						break;
					}
				case ".mds":
					OUT_Disc = new MDS_Format().LoadMDSToDisc(IN_FromPath, IN_DiscMountPolicy);
					break;
			}

			// set up the lowest level synth provider
			if (OUT_Disc != null) OUT_Disc.SynthProvider = new ArraySectorSynthProvider { Sectors = OUT_Disc._Sectors, FirstLBA = -150 };
		}
	}
}
