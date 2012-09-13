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
		#region Properties

		public byte[] InitState { get; private set; }
		
		public int StateCount
		{
			get
			{
				return StateRecords.Count;
			}
		}

		public int Length
		{
			get
			{
				return MovieRecords.Count;
			}
		}

		public int StateFirstIndex
		{
			get
			{
				return (StateRecords.Count == 0) ? -1 : StateRecords[0].Index;
			}
		}

		public int StateLastIndex
		{
			get
			{
				return (StateRecords.Count == 0) ? -1 : StateRecords[StateRecords.Count - 1].Index;
			}
		}

		public int StateSizeInBytes
		{
			get
			{
				if (StateRecords.Count > 0)
				{
					return StateCount * StateRecords[0].State.Length;
				}
				else
				{
					return 0;
				}
			}
		}

		#endregion

		#region Public Methods

		public void Clear()
		{
			MovieRecords.Clear();
			StateRecords.Clear();
		}

		public void ClearStates()
		{
			StateRecords.Clear();
		}

		public void AppendFrame(string frame)
		{
			MovieRecords.Add(frame);
		}

		public void AddState(byte[] state)
		{
			if (Global.Emulator.Frame == 0)
			{
				InitState = state;
			}
			if (Global.Emulator.Frame < StateFirstIndex)
			{
				StateRecords.Clear();
				StateRecords.Add(new StateRecord(Global.Emulator.Frame, state));
			}
			if (Global.Emulator.Frame > StateLastIndex)
			{
				if (StateSizeInBytes + state.Length > MaxStateRecordSize)
				{
					// Discard the oldest state to save space.
					StateRecords.RemoveAt(0);
				}
				StateRecords.Add(new StateRecord(Global.Emulator.Frame,state));
			}
		}

		public void SetFrameAt(int frameNum, string frame)
		{
			if (frameNum < StateLastIndex && (frameNum < StateFirstIndex || frame != GetFrame(frameNum)))
			{
				TruncateStates(frameNum+1);
			}

			if (MovieRecords.Count > frameNum)
			{
				MovieRecords[frameNum] = frame;
			}
			else
			{
				MovieRecords.Add(frame);
			}
		}

		public void AddFrameAt(int frame, string record)
		{
			MovieRecords.Insert(frame, record);

			if (frame <= StateLastIndex)
			{
				if (frame <= StateFirstIndex)
				{
					StateRecords.Clear();
					Global.MovieSession.Movie.RewindToFrame(0);
				}
				else
				{
					StateRecords.RemoveRange(frame - StateFirstIndex, StateLastIndex - frame + 1);
					Global.MovieSession.Movie.RewindToFrame(frame);
				}
			}
		}

		public byte[] GetState(int frame)
		{
			return StateRecords[frame - StateFirstIndex].State;
		}

		public void DeleteFrame(int frame)
		{
			MovieRecords.RemoveAt(frame);
			if (frame <= StateLastIndex)
			{
				if (frame <= StateFirstIndex)
				{
					StateRecords.Clear();
				}
				else
				{
					StateRecords.RemoveRange(frame - StateFirstIndex, StateLastIndex - frame + 1);
				}
			}
		}

		public void TruncateStates(int frame)
		{
			if (frame >= 0)
			{
				if (frame < StateFirstIndex)
				{
					StateRecords.Clear();
				}
				else if (frame <= StateLastIndex)
				{
					StateRecords.RemoveRange(frame - StateFirstIndex, StateLastIndex - frame + 1);
				}
			}
		}

		public string GetFrame(int frame)
		{
			if (frame >= 0 && frame < MovieRecords.Count)
			{
				return MovieRecords[frame];
			}
			else
			{
				return "";  //TODO: throw an exception?
			}
		}

		public void WriteText(StreamWriter sw)
		{
			for (int i = 0; i < MovieRecords.Count; i++)
			{
				sw.WriteLine(GetFrame(i));
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

		public bool FrameLagged(int frame)
		{
			if (frame >= StateFirstIndex && frame <= StateLastIndex && frame <= StateRecords.Count)
			{
				return StateRecords[frame].Lagged;
			}
			else
			{
				return false;
			}
		}

		#endregion

		#region private fields

		private class StateRecord
		{
			public StateRecord(int index, byte[] state)
			{
				Index = index;
				State = state;
				Lagged = Global.Emulator.IsLagFrame;
			}

			public int Index;
			public byte[] State;
			public bool Lagged;
		}

		private List<string> MovieRecords = new List<string>();
		private List<StateRecord> StateRecords = new List<StateRecord>();
		
		//TODO: Make this size limit configurable by the user
		private int MaxStateRecordSize = 1024 * 1024 * 1024; //To limit memory usage.

		#endregion
	}
}
