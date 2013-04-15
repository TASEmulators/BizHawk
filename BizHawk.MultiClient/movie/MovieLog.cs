using System.Collections.Generic;
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
				return _state_records.Count;
			}
		}

		public int Length
		{
			get
			{
				return _movie_records.Count;
			}
		}

		public int StateFirstIndex
		{
			get
			{
				return (_state_records.Count == 0) ? -1 : _state_records[0].Index;
			}
		}

		public int StateLastIndex
		{
			get
			{
				return (_state_records.Count == 0) ? -1 : _state_records[_state_records.Count - 1].Index;
			}
		}

		public int StateSizeInBytes
		{
			get
			{
				if (_state_records.Count > 0)
				{
					return StateCount * _state_records[0].State.Length;
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
			_movie_records.Clear();
			_state_records.Clear();
		}

		public void ClearStates()
		{
			_state_records.Clear();
		}

		public void AppendFrame(string frame)
		{
			_movie_records.Add(frame);
		}

		public void AddState(byte[] state)
		{
			if (Global.Emulator.Frame == 0)
			{
				InitState = state;
			}
			if (Global.Emulator.Frame < StateFirstIndex)
			{
				_state_records.Clear();
				_state_records.Add(new StateRecord(Global.Emulator.Frame, state));
			}
			if (Global.Emulator.Frame > StateLastIndex)
			{
				if (StateSizeInBytes + state.Length > MAXSTATERECORDSIZE)
				{
					// Discard the oldest state to save space.
					_state_records.RemoveAt(0);
				}
				_state_records.Add(new StateRecord(Global.Emulator.Frame,state));
			}
		}

		public void SetFrameAt(int frameNum, string frame)
		{
			if (frameNum < StateLastIndex && (frameNum < StateFirstIndex || frame != GetFrame(frameNum)))
			{
				TruncateStates(frameNum+1);
			}

			if (_movie_records.Count > frameNum)
			{
				_movie_records[frameNum] = frame;
			}
			else
			{
				_movie_records.Add(frame);
			}
		}

		public void AddFrameAt(int frame, string record)
		{
			_movie_records.Insert(frame, record);

			if (frame <= StateLastIndex)
			{
				if (frame <= StateFirstIndex)
				{
					_state_records.Clear();
					Global.MovieSession.Movie.RewindToFrame(0);
				}
				else
				{
					_state_records.RemoveRange(frame - StateFirstIndex, StateLastIndex - frame + 1);
					Global.MovieSession.Movie.RewindToFrame(frame);
				}
			}
		}

		public byte[] GetState(int frame)
		{
			return _state_records[frame - StateFirstIndex].State;
		}

		public void DeleteFrame(int frame)
		{
			_movie_records.RemoveAt(frame);
			if (frame <= StateLastIndex)
			{
				if (frame <= StateFirstIndex)
				{
					_state_records.Clear();
				}
				else
				{
					_state_records.RemoveRange(frame - StateFirstIndex, StateLastIndex - frame + 1);
				}
			}
		}

		public void TruncateStates(int frame)
		{
			if (frame >= 0)
			{
				if (frame < StateFirstIndex)
				{
					_state_records.Clear();
				}
				else if (frame <= StateLastIndex)
				{
					_state_records.RemoveRange(frame - StateFirstIndex, StateLastIndex - frame + 1);
				}
			}
		}

		public string GetFrame(int frame)
		{
			if (frame >= 0 && frame < _movie_records.Count)
			{
				return _movie_records[frame];
			}
			else
			{
				return "";  //TODO: throw an exception?
			}
		}

		public void WriteText(StreamWriter sw)
		{
			for (int i = 0; i < _movie_records.Count; i++)
			{
				sw.WriteLine(GetFrame(i));
			}
		}

		public void TruncateMovie(int frame)
		{
			if (frame < _movie_records.Count)
			{
				_movie_records.RemoveRange(frame, _movie_records.Count - frame);
				TruncateStates(frame);
			}
		}

		public bool FrameLagged(int frame)
		{
			if (frame >= StateFirstIndex && frame <= StateLastIndex && frame <= _state_records.Count)
			{
				if (frame < _state_records.Count)
				{
					return _state_records[frame].Lagged;
				}
				else
				{
					return false;
				}
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

			public readonly int Index;
			public readonly byte[] State;
			public readonly bool Lagged;
		}

		private readonly List<string> _movie_records = new List<string>();
		private readonly List<StateRecord> _state_records = new List<StateRecord>();
		
		//TODO: Make this size limit configurable by the user
		private const int MAXSTATERECORDSIZE = 512*1024*1024; //To limit memory usage.

		#endregion
	}
}
