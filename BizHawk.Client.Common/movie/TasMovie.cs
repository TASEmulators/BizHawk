using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class TasMovie : IMovie
	{
		// TODO: preloading, or benchmark and see how much of a performaance gain it really is
		// TODO: support loop Offset

		public MovieRecord this[int index]
		{
			get
			{
				return _records[index];
			}
		}

		#region Implementation

		public TasMovie(string filename, bool startsFromSavestate = false)
			: this(startsFromSavestate)
		{
			Filename = filename;
		}

		public TasMovie(bool startsFromSavestate = false)
		{
			Filename = String.Empty;
			Header = new MovieHeader { StartsFromSavestate = startsFromSavestate };
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
				double dblseconds = GetSeconds(_records.Count);
				int seconds = (int)(dblseconds % 60);
				int days = seconds / 86400;
				int hours = seconds / 3600;
				int minutes = (seconds / 60) % 60;
				int milliseconds = (int)((dblseconds - seconds) * 1000);
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
				if (frame >= 0)
				{
					return _records[frame].Input;
				}
				else
				{
					return String.Empty;
				}
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

		public void ClearFrame(int frame)
		{
			if (frame < _records.Count)
			{
				Changes = true;
				_records[frame].ClearInput();
			}
		}

		public void AppendFrame(IController source)
		{
			Changes = true;
			_records.Add(new MovieRecord(source, true));
		}

		public void RecordFrame(int frame, IController source)
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

				if (frame < _records.Count)
				{
					PokeFrame(frame, source);
				}
				else
				{
					AppendFrame(source);
				}
			}
		}

		public void PokeFrame(int frame, IController source)
		{
			if (frame < _records.Count)
			{
				Changes = true;
				_records[frame].SetInput(source);
			}
		}

		// TODO:
		public double Fps
		{
			get
			{
				throw new NotImplementedException();
			}
		}

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

		public bool CheckTimeLines(TextReader reader, out string errorMessage)
		{
			throw new NotImplementedException();
		}

		public bool ExtractInputLog(TextReader reader, out string errorMessage)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private

		private enum Moviemode { Inactive, Play, Record, Finished };
		private readonly MovieRecordList _records;
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
