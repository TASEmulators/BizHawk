using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;



namespace BizHawk.Client.EmuHawk
{
	public class BatchRunner
	{
		public class ProgressEventArgs
		{
			public int Completed { get; private set; }
			public int Total { get; private set; }
			public bool ShouldCancel { get; set; }
			public ProgressEventArgs(int Completed, int Total)
			{
				this.Completed = Completed;
				this.Total = Total;
			}
		}
		public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
		public event ProgressEventHandler OnProgress;

		List<string> files;
		RomLoader ldr;
		CoreComm Comm;
		int numframes = 0;
		int multiindex = 0;
		bool multihasnext = false;

		List<Result> Results = new List<Result>();
		Result current;

		public class Result
		{
			public string Filename; // name of file
			public string Fullname; // filename + subfilename
			public GameInfo GI;

			public Type CoreType; // actual type of the core that was returned
			public enum EStatus
			{
				ExceptOnLoad, // exception thrown on load
				ErrorOnLoad, // error method thrown on load
				FalseOnLoad, // romloader returned false with no other information
				ExceptOnAdv, // exception thrown on frame advance
				Success, // load fully complete
			};
			public EStatus Status; // what happened
			public List<string> Messages = new List<string>();

			public int Frames; // number of frames successfully run
			public int LaggedFrames; // number of those that were lagged

			public string BoardName; // iemulator's board name return (could be null!)

			public void DumpToTW(System.IO.TextWriter tw)
			{
				tw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", Filename, Fullname, CoreType, Status, Frames, LaggedFrames, GI.Hash, BoardName);
			}
		}

		public BatchRunner(IEnumerable<string> files, int numframes)
		{
			this.files = new List<string>(files);
			this.numframes = numframes;

			ldr = new RomLoader();
			ldr.OnLoadError += OnLoadError;
			ldr.ChooseArchive = ChooseArchive;
			Comm = new CoreComm(CommMessage, CommMessage);
			CoreFileProvider.SyncCoreCommInputSignals(Comm);
		}

		void OnLoadError(object sender, RomLoader.RomErrorArgs e)
		{
			current.Status = Result.EStatus.ErrorOnLoad;
			current.Messages.Add(string.Format("OnLoadError: {0}, {1}, {2}", e.AttemptedCoreLoad, e.Message, e.Type.ToString()));
		}

		void CommMessage(string msg)
		{
			current.Messages.Add(string.Format("CommMessage: {0}", msg));
		}

		int? ChooseArchive(HawkFile hf)
		{
			int ret = multiindex;
			multiindex++;
			multihasnext = multiindex < hf.ArchiveItems.Count;
			return ret;
		}

		public List<Result> Run()
		{
			Results.Clear();
			current = null;
			RunInternal();
			return new List<Result>(Results);
		}

		void RunInternal()
		{
			for (int i = 0; i < files.Count; i++)
			{
				string f = files[i];
				multihasnext = false;
				multiindex = 0;
				do
				{
					LoadOne(f);
				} while (multihasnext);
				if (OnProgress != null)
				{
					var e = new ProgressEventArgs(i + 1, files.Count);
					OnProgress(this, e);
					if (e.ShouldCancel)
						return;
				}
			}
		}


		void LoadOne(string f)
		{
			current = new Result { Filename = f };
			bool result = false;
			try
			{
				result = ldr.LoadRom(f, Comm);
			}
			catch (Exception e)
			{
				current.Status = Result.EStatus.ExceptOnLoad;
				current.Messages.Add(e.ToString());
				Results.Add(current);
				current = null;
				return;
			}
			current.Fullname = ldr.CanonicalFullPath;
			if (current.Status == Result.EStatus.ErrorOnLoad)
			{
				Results.Add(current);
				current = null;
				return;
			}
			if (result == false)
			{
				current.Status = Result.EStatus.FalseOnLoad;
				Results.Add(current);
				current = null;
				return;
			}

			using (IEmulator emu = ldr.LoadedEmulator)
			{
				current.GI = ldr.Game;
				current.CoreType = emu.GetType();
				emu.Controller = new Controller(emu.ControllerDefinition);
				current.BoardName = emu.BoardName;
				// hack
				if (emu is Emulation.Cores.Nintendo.GBA.VBANext)
				{
					current.BoardName = (emu as Emulation.Cores.Nintendo.GBA.VBANext).GameCode;
				}

				current.Frames = 0;
				current.LaggedFrames = 0;

				for (int i = 0; i < numframes; i++)
				{
					try
					{
						int nsamp;
						short[] samp;
						emu.FrameAdvance(true, true);
						
						// some cores really really really like it if you drain their audio every frame
						if (emu.HasSoundProvider())
						{
							emu.AsSoundProvider().GetSamplesSync(out samp, out nsamp);
						}

						current.Frames++;
						if (emu.CanPollInput() && emu.AsInputPollable().IsLagFrame)
							current.LaggedFrames++;
					}
					catch (Exception e)
					{
						current.Messages.Add(e.ToString());
						current.Status = Result.EStatus.ExceptOnAdv;
						Results.Add(current);
						current = null;
						return;
					}
				}
			}
			current.Status = Result.EStatus.Success;
			Results.Add(current);
			current = null;
			return;
		}
	}
}
