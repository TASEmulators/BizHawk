using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class TasMovie : IMovie
	{
		private enum Moviemode { Inactive, Play, Record, Finished };

		private MovieRecordList _records;
		private Moviemode _mode;

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

		public bool Changes
		{
			get { throw new NotImplementedException(); }
		}

		public bool Loaded
		{
			get { throw new NotImplementedException(); }
		}

		public TimeSpan Time
		{
			get { throw new NotImplementedException(); }
		}

		public double FrameCount
		{
			get { throw new NotImplementedException(); }
		}

		public int InputLogLength
		{
			get { return _records.Count; }
		}

		public bool Load()
		{
			throw new NotImplementedException();
		}

		public void Save()
		{
			throw new NotImplementedException();
		}

		public void SaveAs()
		{
			throw new NotImplementedException();
		}

		public string GetInputLog()
		{
			throw new NotImplementedException();
		}

		public void StartNewRecording()
		{
			throw new NotImplementedException();
		}

		public void StartNewPlayback()
		{
			throw new NotImplementedException();
		}

		public void Stop(bool saveChanges = true)
		{
			throw new NotImplementedException();
		}

		public void SwitchToRecord()
		{
			throw new NotImplementedException();
		}

		public void SwitchToPlay()
		{
			throw new NotImplementedException();
		}

		public void ClearFrame(int frame)
		{
			throw new NotImplementedException();
		}

		public void AppendFrame(string record)
		{
			throw new NotImplementedException();
		}

		public void TruncateMovie(int frame)
		{
			throw new NotImplementedException();
		}

		public void CommitFrame(int frameNum, Emulation.Common.IController source)
		{
			throw new NotImplementedException();
		}

		public void PokeFrame(int frameNum, string input)
		{
			throw new NotImplementedException();
		}

		public LoadStateResult CheckTimeLines(System.IO.TextReader reader, bool onlyGuid, bool ignoreGuidMismatch, out string errorMessage)
		{
			throw new NotImplementedException();
		}

		public void ExtractInputLog(System.IO.TextReader reader, bool isMultitracking)
		{
			throw new NotImplementedException();
		}

		public string GetInput(int frame)
		{
			throw new NotImplementedException();
		}
	}
}
