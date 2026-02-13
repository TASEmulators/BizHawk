using System.Collections.Generic;
using System.Diagnostics;
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

#pragma warning disable CS0618 // this is the sanctioned call-site
		public virtual void Attach(IEmulator emulator)
		{
			Emulator = emulator;
		}

		public void CheckAttachedMatches(IEmulator/*?*/ passed)
			=> Debug.Assert(object.ReferenceEquals(passed, Emulator), $"Core instance doesn't match the object cached on the movie! (Missed call to {nameof(Attach)}?)");
#pragma warning restore CS0618

		[Obsolete("do not use")]
		protected IEmulator Emulator { get; private set; }

		public IMovieSession Session { get; }

		protected bool MakeBackup { get; set; } = true;

		public virtual string PreferredExtension => Extension;

		public const string Extension = "bk2";

		public virtual bool Changes { get; protected set; }
		public bool IsCountingRerecords { get; set; } = true;

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
			Log.Add(Bk2LogEntryGenerator.GenerateLogEntry(source));
			Changes = true;
		}

		public virtual void RecordFrame(int targetFrame, int currentFrame, IController source)
		{
			if (Session.Settings.VBAStyleMovieLoadState)
			{
				if (currentFrame < Log.Count)
				{
					Truncate(currentFrame);
				}
			}

			SetFrameAt(targetFrame, Bk2LogEntryGenerator.GenerateLogEntry(source));

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
			if (frame < FrameCount && frame >= -1)
			{
				_adapter ??= new Bk2Controller(Session.MovieController.Definition, LogKey);
				_adapter.SetFromMnemonic(frame >= 0 ? Log[frame] : Bk2LogEntryGenerator.EmptyEntry(_adapter));
				return _adapter;
			}

			return null;
		}

		public virtual void PokeFrame(int frame, IController source)
		{
			SetFrameAt(frame, Bk2LogEntryGenerator.GenerateLogEntry(source));
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
