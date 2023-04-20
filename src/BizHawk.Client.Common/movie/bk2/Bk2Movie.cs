using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie : BasicMovieInfo, IMovie
	{
		private Bk2Controller _adapter;

		public Bk2Movie(IMovieSession session, string filename) : base(filename)
		{
			Session = session;
			Header[HeaderKeys.MovieVersion] = "BizHawk v2.0.0";
		}

		public virtual void Attach(IEmulator emulator)
		{
			Emulator = emulator;
		}

		protected bool IsAttached() => Emulator != null;

		public IEmulator Emulator { get; private set; }
		public IMovieSession Session { get; }

		protected bool MakeBackup { get; set; } = true;

		public virtual string PreferredExtension => Extension;

		public const string Extension = "bk2";

		public virtual bool Changes { get; protected set; }
		public bool IsCountingRerecords { get; set; } = true;

		public ILogEntryGenerator LogGeneratorInstance(IController source)
		{
			return new Bk2LogEntryGenerator(Emulator?.SystemId ?? SystemID, source);
		}

		public override int FrameCount => Log.Count;
		public int InputLogLength => Log.Count;

		public IStringLog GetLogEntries() => Log;

		public void CopyLog(IEnumerable<string> log)
		{
			Log.Clear();
			foreach (var entry in log)
			{
				Log.Add(entry);
			}
		}

		public void AppendFrame(IController source)
		{
			var lg = LogGeneratorInstance(source);
			Log.Add(lg.GenerateLogEntry());
			Changes = true;
		}

		public virtual void RecordFrame(int frame, IController source)
		{
			if (Session.Settings.VBAStyleMovieLoadState)
			{
				if (Emulator.Frame < Log.Count)
				{
					Truncate(Emulator.Frame);
				}
			}

			var lg = LogGeneratorInstance(source);
			SetFrameAt(frame, lg.GenerateLogEntry());

			Changes = true;
		}

		public virtual void Truncate(int frame)
		{
			if (frame < Log.Count)
			{
				Log.RemoveRange(frame, Log.Count - frame);
				Changes = true;
			}
		}

		public IMovieController GetInputState(int frame)
		{
			if (frame < FrameCount && frame >= 0)
			{
				_adapter ??= new Bk2Controller(LogKey, Session.MovieController.Definition);
				_adapter.SetFromMnemonic(Log[frame]);
				return _adapter;
			}

			return null;
		}

		public virtual void PokeFrame(int frame, IController source)
		{
			var lg = LogGeneratorInstance(source);
			SetFrameAt(frame, lg.GenerateLogEntry());
			Changes = true;
		}

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
