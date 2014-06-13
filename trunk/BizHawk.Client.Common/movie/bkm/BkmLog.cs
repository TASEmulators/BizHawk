using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Client.Common
{
	// TODO: what is this object really trying to accomplish? COnsider making it a collection (ICollection, IEnumerable perhaps)

	/// <summary>
	/// Represents the controller key presses of a movie
	/// </summary>
	public class BkmLog : IEnumerable<string>
	{
		public IEnumerator<string> GetEnumerator()
		{
			return _movieRecords.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

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
				return _movieRecords.Count;
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
				return _state_records.Any() ? StateCount * _state_records[0].State.Length : 0;
			}
		}

		#endregion

		#region Public Methods

		public void Clear()
		{
			_movieRecords.Clear();
			_state_records.Clear();
		}

		public void ClearStates()
		{
			_state_records.Clear();
		}

		public void AppendFrame(string frame)
		{
			_movieRecords.Add(frame);
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
			if (frameNum < StateLastIndex && (frameNum < StateFirstIndex || frame != _movieRecords[frameNum]))
			{
				TruncateStates(frameNum + 1);
			}

			if (_movieRecords.Count > frameNum)
			{
				_movieRecords[frameNum] = frame;
			}
			else
			{
				_movieRecords.Add(frame);
			}
		}

		public void AddFrameAt(int frame, string record)
		{
			_movieRecords.Insert(frame, record);

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

		public byte[] GetState(int frame)
		{
			return _state_records[frame - StateFirstIndex].State;
		}

		public void DeleteFrame(int frame)
		{
			_movieRecords.RemoveAt(frame);
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

		public string this[int frame]
		{
			get
			{
				return _movieRecords[frame];
			}
		}

		public void TruncateMovie(int frame)
		{
			if (frame < _movieRecords.Count)
			{
				_movieRecords.RemoveRange(frame, _movieRecords.Count - frame);
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

		private sealed class StateRecord
		{
			public StateRecord(int index, byte[] state)
			{
				Index = index;
				State = state;
				Lagged = Global.Emulator.IsLagFrame;
			}

			public int Index { get; private set; }
			public byte[] State { get; private set; }
			public bool Lagged { get; private set; }
		}

		private readonly List<string> _movieRecords = new List<string>();
		private readonly List<StateRecord> _state_records = new List<StateRecord>();
		
		// TODO: Make this size limit configurable by the user
		private const int MAXSTATERECORDSIZE = 512 * 1024 * 1024; //To limit memory usage.

		#endregion
	}
}
