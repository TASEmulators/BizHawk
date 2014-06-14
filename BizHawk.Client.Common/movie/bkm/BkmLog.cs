using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Represents the controller key presses of a movie
	/// </summary>
	public class BkmLog : List<string>
	{
		public void SetFrameAt(int frameNum, string frame)
		{
			if (this.Count > frameNum)
			{
				this[frameNum] = frame;
			}
			else
			{
				this.Add(frame);
			}
		}

		public void Truncate(int frame)
		{
			if (frame < this.Count)
			{
				this.RemoveRange(frame, this.Count - frame);
			}
		}
	}
}
