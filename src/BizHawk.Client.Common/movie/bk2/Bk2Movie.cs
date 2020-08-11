using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie : IMovie
	{
		private Bk2Controller _adapter;

		public Bk2Movie(IMovieSession session, string filename)
		{
			if (string.IsNullOrWhiteSpace(filename))
			{
				throw new ArgumentNullException($"{nameof(filename)} can not be null.");
			}

			Session = session;
			Filename = filename;
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

		private string _filename;

		public string Filename
		{
			get => _filename;
			set
			{
				_filename = value;
				Name = Path.GetFileName(Filename);
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

		public TimeSpan TimeLength
		{
			get
			{
				double dblSeconds;
				var core = Header[HeaderKeys.Core];

				if (Header.ContainsKey(HeaderKeys.CycleCount) && (core == CoreNames.Gambatte || core == CoreNames.SubGbHawk))
				{
					ulong numCycles = Convert.ToUInt64(Header[HeaderKeys.CycleCount]);
					double cyclesPerSecond = PlatformFrameRates.GetFrameRate("GB_Clock", IsPal);
					dblSeconds = numCycles / cyclesPerSecond;
				}
				else
				{
					ulong numFrames = (ulong) FrameCount;
					if (Header.ContainsKey(HeaderKeys.VBlankCount))
					{
						numFrames = Convert.ToUInt64(Header[HeaderKeys.VBlankCount]);
					}
					dblSeconds = numFrames / FrameRate;
				}

				var seconds = (int)(dblSeconds % 60);
				var days = seconds / 86400;
				var hours = seconds / 3600;
				var minutes = (seconds / 60) % 60;
				var milliseconds = (int)((dblSeconds - seconds) * 1000);
				return new TimeSpan(days, hours, minutes, seconds, milliseconds);
			}
		}

		public double FrameRate => PlatformFrameRates.GetFrameRate(SystemID, IsPal);

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
