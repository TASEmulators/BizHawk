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

		private List<byte[]> StateList = new List<byte[]>();
		private int StateLastValidIndex = -1;

		public MovieLog()
		{
			//Should this class initialize with an empty string to MovieRecords so that first frame is index 1?
			//MovieRecords.Add("");
		}

		public int Length()
		{
			return MovieRecords.Count;
		}

		public void Clear()
		{
			MovieRecords.Clear();
		}

		public void AddFrame(string frame)
		{
			MovieRecords.Add(frame);
		}

		public void AddState(byte[] state)
		{
			if (Global.Emulator.Frame >= StateList.Count)
			{
				StateList.Add(state);
				StateLastValidIndex = Global.Emulator.Frame;
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

			if (frameNum <= StateList.Count - 1)
			{
				StateList.RemoveRange(frameNum, StateList.Count - frameNum);
			}
			if (StateLastValidIndex >= frameNum)
			{
				StateLastValidIndex = frameNum - 1;
			}
		}

		public void CheckValidity()
		{
			byte[] state = Global.Emulator.SaveStateBinary();
			if (Global.Emulator.Frame < StateList.Count && (null == StateList[Global.Emulator.Frame] || !state.SequenceEqual((byte[])StateList[Global.Emulator.Frame])))
			{
				StateLastValidIndex = Global.Emulator.Frame;
			}
		}

		public int CapturedStateCount()
		{
			return StateList.Count;
		}

		public int LastValidState()
		{
			return StateLastValidIndex;
		}

		public byte[] GetState(int frame)
		{
			return StateList[frame];
		}

		public void DeleteFrame(int frame)
		{
			MovieRecords.RemoveAt(frame);
			if (frame < StateList.Count)
			{
				StateList.RemoveAt(frame);
			}
			if (StateLastValidIndex > frame)
			{
				StateLastValidIndex = frame;
			}
		}

		public void Truncate(int frame)
		{
			if (frame >= 0 && frame < Length())
			{ MovieRecords.RemoveRange(frame, Length() - frame); }
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
			int length = Length();
			for (int x = 0; x < length; x++)
			{
				sw.WriteLine(GetFrame(x));
			}
		}
	}
}
