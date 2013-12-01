using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class TasMovie : IMovie
	{

		public bool IsCountingRerecords
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public bool IsActive
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsPlaying
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsRecording
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsFinished
		{
			get { throw new NotImplementedException(); }
		}

		public bool Changes
		{
			get { throw new NotImplementedException(); }
		}

		public bool Loaded
		{
			get { throw new NotImplementedException(); }
		}

		public double FrameCount
		{
			get { throw new NotImplementedException(); }
		}

		public int InputLogLength
		{
			get { throw new NotImplementedException(); }
		}

		public ulong Rerecords
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public IMovieHeader Header
		{
			get { throw new NotImplementedException(); }
		}

		public string Filename
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
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

		public void ModifyFrame(string record, int frame)
		{
			throw new NotImplementedException();
		}

		public void AppendFrame(string record)
		{
			throw new NotImplementedException();
		}

		public void InsertFrame(string record, int frame)
		{
			throw new NotImplementedException();
		}

		public void InsertBlankFrame(int frame)
		{
			throw new NotImplementedException();
		}

		public void DeleteFrame(int frame)
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

		public string GetTime(bool preLoad)
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
