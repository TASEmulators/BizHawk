using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class TasMovie : IMovie
	{
		// TODO: preloading, or benchmark and see how much of a performaance gain it really is
		// TODO: support loop Offset
		#region Implementation

		public TasMovie(string filename, bool startsFromSavestate = false)
			: this(startsFromSavestate)
		{
			Filename = filename;
		}

		public TasMovie(bool startsFromSavestate = false)
		{
			Filename = String.Empty;
			Header = new MovieHeader();
			Header.StartsFromSavestate = startsFromSavestate;
			_records = new MovieRecordList();
			_mode = Moviemode.Inactive;
			IsCountingRerecords = true;
		}

		public string Filename { get; set; }

		public IMovieHeader Header { get; private set; }

		public bool IsActive
		{
			get { return _mode != Moviemode.Inactive; }
		}

		public bool IsPlaying
		{
			get { return _mode == Moviemode.Play || _mode == Moviemode.Finished; }
		}

		public bool IsRecording
		{
			get { return _mode == Moviemode.Record; }
		}

		public bool IsFinished
		{
			get { return _mode == Moviemode.Finished; }
		}

		public bool IsCountingRerecords { get; set; }

		public bool Changes { get; private set; }

		public bool Loaded
		{
			get { throw new NotImplementedException(); }
		}

		public TimeSpan Time
		{
			get
			{
				return new TimeSpan();
				double dblseconds = GetSeconds(_records.Count);
				int seconds = (int)(dblseconds % 60);
				int days = seconds / 86400;
				int hours = seconds / 3600;
				int minutes = (seconds / 60) % 60;
				int milliseconds = (int)((dblseconds - (double)seconds) * 1000);
				return new TimeSpan(days, hours, minutes, seconds, milliseconds);
			}
		}

		public double FrameCount
		{
			get { return _records.Count; }
		}

		public int InputLogLength
		{
			get { return _records.Count; }
		}

		public string GetInput(int frame)
		{
			if (frame < _records.Count)
			{
				return _records[frame].Input;
			}
			else
			{
				_mode = Moviemode.Finished;
				return String.Empty;
			}
		}

		public string GetInputLog()
		{
			return _records.ToString();
		}

		public void SwitchToRecord()
		{
			_mode = Moviemode.Record;
		}

		public void SwitchToPlay()
		{
			_mode = Moviemode.Play;
			Save();
		}

		public void StartNewPlayback()
		{
			_mode = Moviemode.Play;
			Global.Emulator.ClearSaveRam();
		}

		public void Stop(bool saveChanges = true)
		{
			if (saveChanges)
			{
				if (_mode == Moviemode.Record || Changes)
				{
					Save();
				}
			}

			_mode = Moviemode.Inactive;
		}

		public void Truncate(int frame)
		{
			_records.Truncate(frame);
		}

		// TODO: 

		public void StartNewRecording()
		{
			SwitchToRecord();
			if (Global.Config.EnableBackupMovies && true/*TODO*/ && _records.Any())
			{
				// TODO
			}
		}

		public bool Load()
		{
			throw new NotImplementedException();
		}

		public void Save()
		{
			Changes = false;
			throw new NotImplementedException();
		}

		public void SaveAs()
		{
			Changes = false;
			throw new NotImplementedException();
		}

		public void ClearFrame(int frame)
		{
			if (frame < _records.Count)
			{
				Changes = true;
				_records[frame].Input = MnemonicsGenerator.GetEmptyMnemonic;
			}
		}

		public void AppendFrame(MnemonicsGenerator mg)
		{
			Changes = true;
			_records.Add(new MovieRecord()
			{
				Input = mg.GetControllersAsMnemonic(),
			});
		}

		public void RecordFrame(int frame, MnemonicsGenerator mg)
		{
			if (_mode == Moviemode.Record)
			{
				Changes = true;
				if (Global.Config.VBAStyleMovieLoadState)
				{
					if (Global.Emulator.Frame < _records.Count)
					{
						_records.Truncate(Global.Emulator.Frame);
					}
				}

				PokeFrame(frame, mg);
			}
		}

		public void PokeFrame(int frame, MnemonicsGenerator mg)
		{
			if (frame < _records.Count)
			{
				Changes = true;
				_records[frame].Input = mg.GetControllersAsMnemonic();
			}
		}

		public LoadStateResult CheckTimeLines(System.IO.TextReader reader, bool onlyGuid, bool ignoreGuidMismatch, out string errorMessage)
		{
			throw new NotImplementedException();
		}

		public void ExtractInputLog(System.IO.TextReader reader, bool isMultitracking)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private

		private enum Moviemode { Inactive, Play, Record, Finished };
		private MovieRecordList _records;
		private Moviemode _mode;
		private readonly PlatformFrameRates _frameRates = new PlatformFrameRates();

		private double GetSeconds(int frameCount)
		{
			double frames = frameCount;

			if (frames < 1)
			{
				return 0;
			}

			var system = Header[HeaderKeys.PLATFORM];
			var pal = Header.ContainsKey(HeaderKeys.PAL) && Header[HeaderKeys.PAL] == "1";

			return frames / _frameRates[system, pal];
		}

		#endregion
	}
}
