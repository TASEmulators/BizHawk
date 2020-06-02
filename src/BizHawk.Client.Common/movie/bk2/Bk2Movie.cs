using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;

namespace BizHawk.Client.Common
{
	internal partial class Bk2Movie : IMovie
	{
		private Bk2Controller _adapter;

		internal Bk2Movie(string filename)
		{
			if (string.IsNullOrWhiteSpace(filename))
			{
				throw new ArgumentNullException($"{nameof(filename)} can not be null.");
			}

			Filename = filename;
			Header[HeaderKeys.MovieVersion] = "BizHawk v2.0.0";
		}

		public virtual void Attach(IMovieSession session, IEmulator emulator)
		{
			// TODO: this check would ideally happen
			// but is disabled for now because restarting a movie doesn't new one up
			// so the old one hangs around with old emulator until this point
			// maybe we should new it up, or have a detach method
			//if (!Emulator.IsNull())
			//{
			//	throw new InvalidOperationException("A core has already been attached!");
			//}

			Emulator = emulator;
			Session = session;
		}

		public IEmulator Emulator { get; private set; }
		public IMovieSession Session { get; private set; }

		protected bool MakeBackup { get; set; } = true;

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
		public bool IsCountingRerecords { get; set; } = true;

		public ILogEntryGenerator LogGeneratorInstance(IController source)
		{
			return new Bk2LogEntryGenerator(Emulator.SystemId, source);
		}

		public int FrameCount => Log.Count;
		public int InputLogLength => Log.Count;

		public ulong TimeLength
		{
			get
			{
				if (Header.ContainsKey(HeaderKeys.VBlankCount))
				{
					return Convert.ToUInt64(Header[HeaderKeys.VBlankCount]);
				}

				if (Header.ContainsKey(HeaderKeys.CycleCount) && Header[HeaderKeys.Core] == CoreNames.Gambatte)
				{
					return Convert.ToUInt64(Header[HeaderKeys.CycleCount]);
				}

				if (Header.ContainsKey(HeaderKeys.CycleCount) && Header[HeaderKeys.Core] == CoreNames.SubGbHawk)
				{
					return Convert.ToUInt64(Header[HeaderKeys.CycleCount]);
				}

				return (ulong)Log.Count;
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
			if (Global.Config.Movies.VBAStyleMovieLoadState)
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
			// This is a bad way to do multitrack logic, pass the info in instead of going to the global
			// and it is weird for Truncate to possibly not truncate
			if (!Session.MultiTrack.IsActive)
			{
				if (frame < Log.Count)
				{
					Log.RemoveRange(frame, Log.Count - frame);
					Changes = true;
				}
			}
		}

		public IMovieController GetInputState(int frame)
		{
			if (frame < FrameCount && frame >= 0)
			{
				_adapter ??= new Bk2Controller(Session.MovieController.Definition);
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

		public virtual void ClearFrame(int frame)
		{
			var lg = LogGeneratorInstance(Session.MovieController);
			SetFrameAt(frame, lg.EmptyEntry);
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
