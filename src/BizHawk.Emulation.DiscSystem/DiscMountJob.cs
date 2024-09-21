using System.IO;
using BizHawk.Common.PathExtensions;
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

				//generate toc and session tracks:
				for (var i = 1; i < OUT_Disc.Sessions.Count; i++)
				{
					var session = OUT_Disc.Sessions[i];
					//1. TOC from RawTOCEntries
					var tocSynth = new Synthesize_DiscTOC_From_RawTOCEntries_Job(session.RawTOCEntries);
					tocSynth.Run();
					session.TOC = tocSynth.Result;
					//2. DiscTracks from TOC
					var tracksSynth = new Synthesize_DiscTracks_From_DiscTOC_Job(OUT_Disc, session);
					tracksSynth.Run();
				}
				
				//insert a synth provider to take care of the leadout track
				//currently, we let mednafen take care of its own leadout track (we'll make that controllable later)
				//TODO: This currently doesn't work well with multisessions (only the last session can have a leadout read with the current model)
				//(although note only VirtualJaguar currently deals with multisession discs and it doesn't care about the leadout so far)
				if (IN_DiscInterface != DiscInterface.MednaDisc)
				{
					var ss_leadout = new SS_Leadout
					{
						SessionNumber = OUT_Disc.Sessions.Count - 1,
						Policy = IN_DiscMountPolicy
					};
					bool Condition(int lba) => lba >= OUT_Disc.Sessions[OUT_Disc.Sessions.Count - 1].LeadoutLBA;
					new ConditionalSectorSynthProvider().Install(OUT_Disc, Condition, ss_leadout);
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

				cfr.SetBaseDirectory(cueDirPath);

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
				if (!okParse || parseJob.OUT_ErrorLevel) return;

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

			var (dir, file, ext) = IN_FromPath.SplitPathToDirFileAndExt();
			switch (ext.ToLowerInvariant())
			{
				case ".ccd":
					OUT_Disc = CCD_Format.LoadCCDToDisc(IN_FromPath, IN_DiscMountPolicy);
					break;
				case ".cdi":
					OUT_Disc = CDI_Format.LoadCDIToDisc(IN_FromPath, IN_DiscMountPolicy);
					break;
				case ".chd":
					OUT_Disc = CHD_Format.LoadCHDToDisc(IN_FromPath, IN_DiscMountPolicy);
					break;
				case ".cue":
					LoadCue(dir, File.ReadAllText(IN_FromPath));
					break;
				case ".iso":
					// make a fake .cue file to represent this .iso and mount that
					// however... to save many users from a stupid mistake, check if the size is NOT a multiple of 2048 (but IS a multiple of 2352) and in that case consider it a mode2 disc
					//TODO try it both ways and check the disc type to use whichever one succeeds in identifying a disc type
					LoadCue(cueDirPath: dir, cueContent: GenerateCue(binFilename: file, binFilePath: IN_FromPath));
					break;
				case ".toc":
					throw new NotSupportedException(".TOC not supported yet");
				case ".mds":
					OUT_Disc = MDS_Format.LoadMDSToDisc(IN_FromPath, IN_DiscMountPolicy);
					break;
				case ".nrg":
					OUT_Disc = NRG_Format.LoadNRGToDisc(IN_FromPath, IN_DiscMountPolicy);
					break;
			}

			// set up the lowest level synth provider
			if (OUT_Disc != null) OUT_Disc.SynthProvider = new ArraySectorSynthProvider { Sectors = OUT_Disc._Sectors, FirstLBA = -150 };
		}

		public static string GenerateCue(string binFilename, bool isMode2)
			=> $@"FILE ""{binFilename}"" BINARY
  TRACK 01 {(isMode2 ? "MODE2/2352" : "MODE1/2048")}
    INDEX 01 00:00:00";

		public static string GenerateCue(string binFilename, string binFilePath)
		{
			var len = new FileInfo(binFilePath).Length;
			return GenerateCue(binFilename, isMode2: len % 2048 is not 0 && len % 2352 is 0);
		}
	}
}
