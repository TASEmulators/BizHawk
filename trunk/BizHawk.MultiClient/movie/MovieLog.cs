using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// Represents the controller key presses of a movie
	/// </summary>
	public class MovieLog
	{
		//TODO: Insert(int frame) not useful for convenctional tasing but TAStudio will want it

		List<string> MovieRecords = new List<string>();

		private List<byte[]> StateRecords = new List<byte[]>();

		public MovieLog()
		{
		}

		public int MovieLength()
		{
			return MovieRecords.Count;
		}

		public int StateLength()
		{
			return StateRecords.Count;
		}

		public void Clear()
		{
			MovieRecords.Clear();
			StateRecords.Clear();
		}

		public void AddFrame(string frame)
		{
			MovieRecords.Add(frame);
		}

		public void AddState(byte[] state)
		{
			if (Global.Emulator.Frame >= StateRecords.Count)
			{
				StateRecords.Add(state);
			}
		}

		public void SetFrameAt(int frameNum, string frame)
		{
			if (MovieRecords.Count > frameNum)
				MovieRecords[frameNum] = frame;
			else
				MovieRecords.Add(frame);
		}
		public void AddFrameAt(string frame, int frameNum)
		{
			MovieRecords.Insert(frameNum, frame);

			if (frameNum <= StateRecords.Count - 1)
			{
				StateRecords.RemoveRange(frameNum, StateRecords.Count - frameNum);
			}
		}

		public void CheckValidity()
		{
			byte[] state = Global.Emulator.SaveStateBinary();
			if (Global.Emulator.Frame < StateRecords.Count && !state.SequenceEqual((byte[])StateRecords[Global.Emulator.Frame]))
			{
				TruncateStates(Global.Emulator.Frame);
			}
		}

		public int CapturedStateCount()
		{
			return StateRecords.Count;
		}

		public int ValidStateCount()
		{
			return StateRecords.Count;
		}

		public byte[] GetState(int frame)
		{
			return StateRecords[frame];
		}

		public void DeleteFrame(int frame)
		{
			MovieRecords.RemoveAt(frame);
			if (frame < StateRecords.Count)
			{
				StateRecords.RemoveAt(frame);
			}
		}

		public void ClearStates()
		{
			StateRecords.Clear();
		}

		public void TruncateFrames(int frame)
		{
			if (frame >= 0 && frame < MovieLength())
			{
				MovieRecords.RemoveRange(frame, MovieLength() - frame);
			}
		}

		public void TruncateStates(int frame)
		{
			if (frame >= 0 && frame < StateLength())
			{
				StateRecords.RemoveRange(frame, StateLength() - frame);
			}
		}

		public string GetFrame(int frameCount) //Frame count is 0 based here, should it be?
		{
			if (frameCount >= 0)
			{
				if (frameCount < MovieRecords.Count)
					return MovieRecords[frameCount];
				else
					return "";
			}
			else
				return "";  //TODO: throw an exception?
		}

		public void WriteText(StreamWriter sw)
		{
			int length = MovieLength();
			for (int x = 0; x < length; x++)
			{
				sw.WriteLine(GetFrame(x));
			}
		}
	}
}
