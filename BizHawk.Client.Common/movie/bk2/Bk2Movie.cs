using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie : IMovie
	{
		private Bk2ControllerAdapter _adapter;

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

			Filename = "";
			IsCountingRerecords = true;
			Mode = Moviemode.Inactive;
			MakeBackup = true;

			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v2.0.0";

			Log = StringLogUtil.MakeStringLog();
		}

		protected bool MakeBackup { get; set; }

		private string _filename;

		public string Filename
		{
			get => _filename;
			set
			{
				_filename = value;
				int index = Filename.LastIndexOf("\\");
				Name = Filename.Substring(index + 1, Filename.Length - index - 1);
			}
		}

		public string Name { get; private set; }

		public virtual string PreferredExtension => Extension;

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
				
				return Log.Count;
			}
		}

		public int InputLogLength => Log.Count;

		public int TimeLength
		{
			get
			{
				if (Header.ContainsKey("VBlankCount")) { return Convert.ToInt32(Header["VBlankCount"]); }
				return Log.Count;
			}
		}

		#region Log Editing

		public void AppendFrame(IController source)
		{
			var lg = LogGeneratorInstance();
			lg.SetSource(source);
			Log.Add(lg.GenerateLogEntry());
			Changes = true;
		}

		public virtual void RecordFrame(int frame, IController source)
		{
			if (Global.Config.VBAStyleMovieLoadState)
			{
				if (Global.Emulator.Frame < Log.Count)
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
				if (frame < Log.Count)
				{
					Log.RemoveRange(frame, Log.Count - frame);
					Changes = true;
				}
			}
		}

		public virtual IController GetInputState(int frame)
		{
			if (frame < FrameCount && frame >= 0)
			{
				if (_adapter == null)
				{
					_adapter = new Bk2ControllerAdapter
					{
						Definition = Global.MovieSession.MovieControllerAdapter.Definition
					};
				}

				int getFrame;

				if (LoopOffset.HasValue)
				{
					if (frame < Log.Count)
					{
						getFrame = frame;
					}
					else
					{
						getFrame = ((frame - LoopOffset.Value) % (Log.Count - LoopOffset.Value)) + LoopOffset.Value;
					}
				}
				else
				{
					getFrame = frame;
				}

				_adapter.SetControllersAsMnemonic(Log[getFrame]);
				return _adapter;
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
			if (Log.Count > frameNum)
			{
				Log[frameNum] = frame;
			}
			else
			{
				Log.Add(frame);
			}
		}
	}
}
