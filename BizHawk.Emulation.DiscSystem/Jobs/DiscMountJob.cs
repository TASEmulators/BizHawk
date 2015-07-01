using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	public partial class DiscMountJob : LoggedJob
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
			FinishLog();
		}

		void RunBizHawk()
		{
			string infile = IN_FromPath;
			string cue_content = null;

			var cfr = new CUE_Format2.CueFileResolver();

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
				var cue2 = new CUE_Format2();

				cue2.Resolver = cfr;
				if (!cfr.IsHardcodedResolve) cfr.SetBaseDirectory(Path.GetDirectoryName(infile));

				//parse the cue file
				var parseJob = new CUE_Format2.ParseCueJob();
				if (cue_content == null)
					cue_content = File.ReadAllText(cuePath);
				parseJob.IN_CueString = cue_content;
				cue2.ParseCueFile(parseJob);
				if (parseJob.OUT_Log != "") Console.WriteLine(parseJob.OUT_Log);
				ConcatenateJobLog(parseJob);

				//compile the cue file:
				//includes this work: resolve required bin files and find out what it's gonna take to load the cue
				var compileJob = new CUE_Format2.CompileCueJob();
				compileJob.IN_CueFormat = new CUE_Format2() { Resolver = cfr };
				compileJob.IN_CueFile = parseJob.OUT_CueFile;
				compileJob.Run();
				if (compileJob.OUT_Log != "") Console.WriteLine(compileJob.OUT_Log);
				ConcatenateJobLog(compileJob);

				//check slow loading threshold
				if (compileJob.OUT_LoadTime >= IN_SlowLoadAbortThreshold)
				{
					Warn("Loading terminated due to slow load threshold");
					goto DONE;
				}

				//actually load it all up
				var loadJob = new CUE_Format2.LoadCueJob();
				loadJob.IN_CompileJob = compileJob;
				loadJob.Run();
				if (loadJob.OUT_Log != "") Console.WriteLine(loadJob.OUT_Log);
				ConcatenateJobLog(loadJob);

				OUT_Disc = loadJob.OUT_Disc;

				//apply SBI if it exists (TODO - for formats other than cue?)
				var sbiPath = Path.ChangeExtension(IN_FromPath, ".sbi");
				if (File.Exists(sbiPath) && SBI.SBIFormat.QuickCheckISSBI(sbiPath))
				{
					var sbiJob = new SBI.LoadSBIJob();
					sbiJob.IN_Path = sbiPath;
					sbiJob.Run();
					OUT_Disc.ApplySBI(sbiJob.OUT_Data, true);
				}
			}
			else if (ext == ".ccd")
			{
				CCD_Format ccdLoader = new CCD_Format();
				OUT_Disc = ccdLoader.LoadCCDToDisc(IN_FromPath);
			}

		DONE:
			;
		}
	}
	
}