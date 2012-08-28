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
		
		private struct StateRecordStruct
		{
			public StateRecordStruct(int index, byte[] state)
			{
				this.index = index;
				this.state = state;
			}

			public int index;
			public byte[] state;
		}

		private List<string> MovieRecords = new List<string>();
		private List<StateRecordStruct> StateRecords = new List<StateRecordStruct>();
		private byte[] InitState;
		//TODO: Make this size limit configurable by the user
		private int MaxStateRecordSize = 1024 * 1024 * 1024; //To limit memory usage.
		public int StateCount { get { return StateRecords.Count; } }

		public MovieLog()
		{
		}

		public int MovieLength()
		{
			return MovieRecords.Count;
		}

		public int StateFirstIndex()
		{
			return (0 == StateRecords.Count) ? -1 : StateRecords[0].index;
		}

		public int StateLastIndex()
		{
			return (0 == StateRecords.Count) ? -1 : StateRecords[StateRecords.Count-1].index;
		}

		public int StateSizeInFrames()
		{
			return StateRecords.Count;
		}

		public int StateSizeInBytes()
		{
			return (0 == StateRecords.Count) ? 0 : StateRecords.Count * StateRecords[0].state.Length;
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
			if (0 == Global.Emulator.Frame)
			{
				InitState = state;
			}
			if (Global.Emulator.Frame < StateFirstIndex())
			{
				StateRecords.Clear();
				StateRecords.Add(new StateRecordStruct(Global.Emulator.Frame, state));
			}
			if (Global.Emulator.Frame > StateLastIndex())
			{
				if (StateSizeInBytes() + state.Length > MaxStateRecordSize)
				{
					// Discard the oldest state to save space.
					StateRecords.RemoveAt(0);
				}
				StateRecords.Add(new StateRecordStruct(Global.Emulator.Frame,state));
			}
		}

		public void SetFrameAt(int frameNum, string frame)
		{
			if (frameNum < StateLastIndex() && (frameNum < StateFirstIndex() || frame != GetFrame(frameNum)))
			{
				TruncateStates(frameNum+1);
			}

			if (MovieRecords.Count > frameNum)
				MovieRecords[frameNum] = frame;
			else
				MovieRecords.Add(frame);
		}
		public void AddFrameAt(string frame, int frameNum)
		{
			MovieRecords.Insert(frameNum, frame);

			if (frameNum <= StateLastIndex())
			{
				if (frameNum <= StateFirstIndex())
				{
					StateRecords.Clear();
					Global.MovieSession.Movie.RewindToFrame(0);
				}
				else
				{
					StateRecords.RemoveRange(frameNum - StateFirstIndex(), StateLastIndex() - frameNum + 1);
					Global.MovieSession.Movie.RewindToFrame(frameNum);
				}
			}
		}

		public byte[] GetState(int frame)
		{
			return StateRecords[frame-StateFirstIndex()].state;
		}

		public byte[] GetInitState()
		{
			return InitState;
		}

		public void DeleteFrame(int frame)
		{
			MovieRecords.RemoveAt(frame);
			if (frame <= StateLastIndex())
			{
				if (frame <= StateFirstIndex())
				{
					StateRecords.Clear();
				}
				else
				{
					StateRecords.RemoveRange(frame - StateFirstIndex(), StateLastIndex() - frame + 1);
				}
			}
		}

		public void ClearStates()
		{
			StateRecords.Clear();
		}

		public void TruncateStates(int frame)
		{
			if (frame >= 0)
			{
				if (frame < StateFirstIndex())
				{
					StateRecords.Clear();
				}
				else if (frame <= StateLastIndex())
				{
					StateRecords.RemoveRange(frame - StateFirstIndex(), StateLastIndex() - frame + 1);
				}
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

		public void TruncateMovie(int frame)
		{
			if (frame < MovieRecords.Count)
			{
				MovieRecords.RemoveRange(frame, MovieRecords.Count - frame);
				TruncateStates(frame);
			}
		}

	}
}
