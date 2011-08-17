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
