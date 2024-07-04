using System.Collections.Generic;
using System.IO;
using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class BatchRunner
	{
		public class ProgressEventArgs
		{
			public int Completed { get; }
			public int Total { get; }
			public bool ShouldCancel { get; set; }
			public ProgressEventArgs(int completed, int total)
			{
				Completed = completed;
				Total = total;
			}
		}

		public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
		public event ProgressEventHandler OnProgress;

		private readonly List<string> _files;
		private readonly List<Result> _results = new List<Result>();
		private readonly RomLoader _ldr;
		private readonly CoreComm _comm;
		private readonly int _numFrames;

		private int _multiIndex;
		private bool _multiHasNext;
		private Result _current;

		public class Result
		{
			public string Filename { get; set; } // name of file
			public string Fullname { get; set; } // filename + subfilename
			public GameInfo Game { get; set; }

			public Type CoreType { get; set; } // actual type of the core that was returned
			public enum EStatus
			{
				ExceptOnLoad, // exception thrown on load
				ErrorOnLoad, // error method thrown on load
				FalseOnLoad, // RomLoader returned false with no other information
				ExceptOnAdv, // exception thrown on frame advance
				Success // load fully complete
			}

			public EStatus Status { get; set; } // what happened
			public List<string> Messages { get; set; } = new List<string>();

			public int Frames { get; set; } // number of frames successfully run
			public int LaggedFrames { get; set; } // number of those that were lagged

			public string BoardName { get; set; } // IEmulator's board name return (could be null!)

			public void DumpTo(TextWriter tw)
			{
				tw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", Filename, Fullname, CoreType, Status, Frames, LaggedFrames, Game.Hash, BoardName);
			}
		}

		public BatchRunner(Config config, CoreComm comm, IEnumerable<string> files, int numFrames)
		{
			_files = new List<string>(files);
			_numFrames = numFrames;

			_ldr = new RomLoader(config);
			_ldr.OnLoadError += OnLoadError;
			_ldr.ChooseArchive = ChooseArchive;
			_comm = comm;
		}

		private void OnLoadError(object sender, RomLoader.RomErrorArgs e)
		{
			_current.Status = Result.EStatus.ErrorOnLoad;
			_current.Messages.Add($"{nameof(OnLoadError)}: {e.AttemptedCoreLoad}, {e.Message}, {e.Type}");
		}

		private int? ChooseArchive(HawkFile hf)
		{
			int ret = _multiIndex;
			_multiIndex++;
			_multiHasNext = _multiIndex < hf.ArchiveItems.Count;
			return ret;
		}

		public List<Result> Run()
		{
			_results.Clear();
			_current = null;
			RunInternal();
			return new List<Result>(_results);
		}

		private void RunInternal()
		{
			for (int i = 0; i < _files.Count; i++)
			{
				string f = _files[i];
				_multiHasNext = false;
				_multiIndex = 0;
				do
				{
					LoadOne(f);
				} while (_multiHasNext);
				if (OnProgress != null)
				{
					var e = new ProgressEventArgs(i + 1, _files.Count);
					OnProgress(this, e);
					if (e.ShouldCancel)
					{
						return;
					}
				}
			}
		}

		private void LoadOne(string f)
		{
			_current = new Result { Filename = f };
			bool result;
			try
			{
				result = _ldr.LoadRom(f, _comm, null);
			}
			catch (Exception e)
			{
				_current.Status = Result.EStatus.ExceptOnLoad;
				_current.Messages.Add(e.ToString());
				_results.Add(_current);
				_current = null;
				return;
			}

			_current.Fullname = _ldr.CanonicalFullPath;
			if (_current.Status == Result.EStatus.ErrorOnLoad)
			{
				_results.Add(_current);
				_current = null;
				return;
			}

			if (!result)
			{
				_current.Status = Result.EStatus.FalseOnLoad;
				_results.Add(_current);
				_current = null;
				return;
			}

			using (IEmulator emu = _ldr.LoadedEmulator)
			{
				_current.Game = _ldr.Game;
				_current.CoreType = emu.GetType();
				var controller = new Controller(emu.ControllerDefinition);
				_current.BoardName = emu.HasBoardInfo() ? emu.AsBoardInfo().BoardName : null;

				_current.Frames = 0;
				_current.LaggedFrames = 0;

				for (int i = 0; i < _numFrames; i++)
				{
					try
					{
						emu.FrameAdvance(controller, true);
						
						// some cores really really really like it if you drain their audio every frame
						if (emu.HasSoundProvider())
						{
							emu.AsSoundProvider().GetSamplesSync(out _, out _);
						}

						_current.Frames++;
						if (emu.CanPollInput() && emu.AsInputPollable().IsLagFrame)
							_current.LaggedFrames++;
					}
					catch (Exception e)
					{
						_current.Messages.Add(e.ToString());
						_current.Status = Result.EStatus.ExceptOnAdv;
						_results.Add(_current);
						_current = null;
						return;
					}
				}
			}
			_current.Status = Result.EStatus.Success;
			_results.Add(_current);
			_current = null;
		}
	}
}
