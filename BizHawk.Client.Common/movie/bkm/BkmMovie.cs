using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public partial class BkmMovie : IMovie
	{
		private bool _makeBackup = true;
		private bool _changes;
		private int? _loopOffset;

		public BkmMovie(string filename)
			: this()
		{
			Rerecords = 0;
			Filename = filename;
			Loaded = !string.IsNullOrWhiteSpace(filename);
		}

		public BkmMovie()
		{
			Header = new BkmHeader();
			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v0.0.1";
			Filename = string.Empty;
			_preloadFramecount = 0;

			IsCountingRerecords = true;
			_mode = Moviemode.Inactive;
			_makeBackup = true;
		}

		#region Properties

		public ILogEntryGenerator LogGeneratorInstance()
		{
			return new BkmLogEntryGenerator();
		}

		public string PreferredExtension { get { return Extension; } }
		public const string Extension = "bkm";

		public BkmHeader Header { get; private set; }
		public string Filename { get; set; }
		public bool IsCountingRerecords { get; set; }
		public bool Loaded { get; private set; }
		
		public int InputLogLength
		{
			get { return _log.Count; }
		}

		public double FrameCount
		{
			get
			{
				if (_loopOffset.HasValue)
				{
					return double.PositiveInfinity;
				}
				
				if (Loaded)
				{
					return _log.Count;
				}

				return _preloadFramecount;
			}
		}

		public bool Changes
		{
			get { return _changes; }
		}

		#endregion

		#region Public Log Editing

		public IController GetInputState(int frame)
		{
			if (frame < FrameCount && frame >= 0)
			{

				int getframe;

				if (_loopOffset.HasValue)
				{
					if (frame < _log.Count)
					{
						getframe = frame;
					}
					else
					{
						getframe = ((frame - _loopOffset.Value) % (_log.Count - _loopOffset.Value)) + _loopOffset.Value;
					}
				}
				else
				{
					getframe = frame;
				}

				var adapter = new BkmControllerAdapter
				{
					Definition = Global.MovieSession.MovieControllerAdapter.Definition
				};
				adapter.SetControllersAsMnemonic(_log[getframe]);
				return adapter;
			}

			return null;
		}

		public void ClearFrame(int frame)
		{
			var lg = LogGeneratorInstance();
			SetFrameAt(frame, lg.EmptyEntry);
			_changes = true;
		}

		public void AppendFrame(IController source)
		{
			var lg = LogGeneratorInstance();
			lg.SetSource(source);
			_log.Add(lg.GenerateLogEntry());
			_changes = true;
		}

		public void Truncate(int frame)
		{
			if (frame < _log.Count)
			{
				_log.RemoveRange(frame, _log.Count - frame);
				_changes = true;
			}
		}

		public void PokeFrame(int frame, IController source)
		{
			var lg = LogGeneratorInstance();
			lg.SetSource(source);

			_changes = true;
			SetFrameAt(frame, lg.GenerateLogEntry());
		}

		public void RecordFrame(int frame, IController source)
		{
			// Note: Truncation here instead of loadstate will make VBA style loadstates
			// (Where an entire movie is loaded then truncated on the next frame
			// this allows users to restore a movie with any savestate from that "timeline"
			if (Global.Config.VBAStyleMovieLoadState)
			{
				if (Global.Emulator.Frame < _log.Count)
				{
					Truncate(Global.Emulator.Frame);
				}
			}

			var lg = LogGeneratorInstance();
			lg.SetSource(source);
			SetFrameAt(frame, lg.GenerateLogEntry());

			_changes = true;
		}

		#endregion

		private void SetFrameAt(int frameNum, string frame)
		{
			if (_log.Count > frameNum)
			{
				_log[frameNum] = frame;
			}
			else
			{
				_log.Add(frame);
			}
		}
	}
}