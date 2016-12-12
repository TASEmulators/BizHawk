using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie : IMovie
	{
		protected bool MakeBackup = true;

		public Bk2Movie(string filename)
			: this()
		{
			Rerecords = 0;
			Filename = filename;
		}

		public Bk2Movie()
		{
			Subtitles = new SubtitleList();
			Comments = new List<string>();

			Filename = string.Empty;
			IsCountingRerecords = true;
			_mode = Moviemode.Inactive;
			MakeBackup = true;

			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v2.0.0";

			_log = StringLogUtil.MakeStringLog();
		}

		private string _filename;

		public string Filename
		{
			get { return _filename; }
			set
			{
				_filename = value;
				int index = Filename.LastIndexOf("\\");
				Name = Filename.Substring(index + 1, Filename.Length - index - 1);
			}
		}

		public string Name { get; private set; }

		public virtual string PreferredExtension { get { return Extension; } }
		public const string Extension = "bk2";

		public virtual bool Changes { get; protected set; }
		public bool IsCountingRerecords { get; set; }

		public ILogEntryGenerator LogGeneratorInstance()
		{
			return new Bk2LogEntryGenerator(LogKey);
		}

		public double FrameCount
		{
			get
			{
				if (LoopOffset.HasValue)
				{
					return double.PositiveInfinity;
				}

				return _log.Count;
			}
		}

		public int InputLogLength
		{
			get { return _log.Count; }
		}

		#region Log Editing

		public void AppendFrame(IController source)
		{
			var lg = LogGeneratorInstance();
			lg.SetSource(source);
			_log.Add(lg.GenerateLogEntry());
			Changes = true;
		}

		public virtual void RecordFrame(int frame, IController source)
		{
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

			Changes = true;
		}

		public virtual void Truncate(int frame)
		{
			// This is a bad way to do multitrack logic, pass the info in instead of going to the global
			// and it is weird for Truncate to possibly not truncate
			if (!Global.MovieSession.MultiTrack.IsActive)
			{
				if (frame < _log.Count)
				{
					_log.RemoveRange(frame, _log.Count - frame);
					Changes = true;

					
				}
			}
		}

		public virtual IController GetInputState(int frame)
		{
			if (frame < FrameCount && frame >= 0)
			{

				int getframe;

				if (LoopOffset.HasValue)
				{
					if (frame < _log.Count)
					{
						getframe = frame;
					}
					else
					{
						getframe = ((frame - LoopOffset.Value) % (_log.Count - LoopOffset.Value)) + LoopOffset.Value;
					}
				}
				else
				{
					getframe = frame;
				}

				var adapter = new Bk2ControllerAdapter
				{
					Definition = Global.MovieSession.MovieControllerAdapter.Definition
				};

				adapter.SetControllersAsMnemonic(_log[getframe]);
				return adapter;
			}

			return null;
		}

		public virtual void PokeFrame(int frame, IController source)
		{
			var lg = LogGeneratorInstance();
			lg.SetSource(source);

			Changes = true;
			SetFrameAt(frame, lg.GenerateLogEntry());
		}

		public virtual void ClearFrame(int frame)
		{
			var lg = LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerInstance());
			SetFrameAt(frame, lg.EmptyEntry);
			Changes = true;
		}

		#endregion

		protected void SetFrameAt(int frameNum, string frame)
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
