using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Captures savestates and manages the logic of adding, retrieving, 
	/// invalidating/clearing of states.  Also does memory management and limiting of states
	/// </summary>
	public class TasStateManager : IDisposable
	{
		// TODO: pass this in, and find a solution to a stale reference (this is instantiated BEFORE a new core instance is made, making this one stale if it is simply set in the constructor
		private IStatable Core => Global.Emulator.AsStatable();

		public Action<int> InvalidateCallback { get; set; }

		private void CallInvalidateCallback(int index)
		{
			InvalidateCallback?.Invoke(index);
		}

		internal NDBDatabase NdbDatabase { get; set; }
		private Guid _guid = Guid.NewGuid();
		private SortedList<int, StateManagerState> _states = new SortedList<int, StateManagerState>();

		private string StatePath
		{
			get
			{
				var basePath = PathManager.MakeAbsolutePath(Global.Config.PathEntries["Global", "TAStudio states"].Path, null);
				return Path.Combine(basePath, _guid.ToString());
			}
		}

		private bool _isMountedForWrite;
		private readonly TasMovie _movie;

		private StateManagerDecay _decay;
		private ulong _expectedStateSize;
		private int _stateFrequency;
		private readonly int _minFrequency = 1;
		private readonly int _maxFrequency = 16;
		private int _maxStates => (int)(Settings.Cap / _expectedStateSize) +
			(int)((ulong)Settings.DiskCapacitymb * 1024 * 1024 / _expectedStateSize);
		private int _fileStateGap => 1 << Settings.FileStateGap;

		public TasStateManager(TasMovie movie)
		{
			_movie = movie;
			Settings = new TasStateManagerSettings(Global.Config.DefaultTasProjSettings);

			if (_movie.StartsFromSavestate)
			{
				SetState(0, _movie.BinarySavestate);
			}

			_decay = new StateManagerDecay(this);
		}

		public void Dispose()
		{
			// States and BranchStates don't need cleaning because they would only contain an ndbdatabase entry which was demolished by the below
			NdbDatabase?.Dispose();
		}

		public void UpdateStateFrequency()
		{
			_stateFrequency = NumberExtensions.Clamp(
					((int)_expectedStateSize / Settings.MemStateGapDivider / 1024),
					_minFrequency, _maxFrequency);

			_decay.UpdateSettings(_maxStates, _stateFrequency, 4);
		}

		/// <summary>
		/// Mounts this instance for write access. Prior to that it's read-only
		/// </summary>
		public void MountWriteAccess()
		{
			if (_isMountedForWrite)
			{
				return;
			}

			int limit = 0;
			_isMountedForWrite = true;
			_expectedStateSize = (ulong)Core.SaveStateBinary().Length;
			UpdateStateFrequency();

			if (_expectedStateSize > 0)
			{
				limit = _maxStates;
			}

			_states = new SortedList<int, StateManagerState>(limit);

			if (_expectedStateSize > int.MaxValue)
			{
				throw new InvalidOperationException();
			}

			NdbDatabase = new NDBDatabase(StatePath, Settings.DiskCapacitymb * 1024 * 1024, (int)_expectedStateSize);
		}
		
		public TasStateManagerSettings Settings { get; set; }

		/// <summary>
		/// Retrieves the savestate for the given frame,
		/// If this frame does not have a state currently, will return an empty array
		/// </summary>
		/// <returns>A savestate for the given frame or an empty array if there isn't one</returns>
		public KeyValuePair<int, byte[]> this[int frame]
		{
			get
			{
				if (frame == 0)
				{
					return new KeyValuePair<int, byte[]>(0, InitialState);
				}

				if (_states.ContainsKey(frame))
				{
					return new KeyValuePair<int, byte[]>(frame, _states[frame].State);
				}

				return new KeyValuePair<int, byte[]>(-1, new byte[0]);
			}
		}

		public byte[] InitialState
		{
			get
			{
				if (_movie.StartsFromSavestate)
				{
					return _movie.BinarySavestate;
				}

				return _states[0].State;
			}
		}

		/// <summary>
		/// Requests that the current emulator state be captured 
		/// Unless force is true, the state may or may not be captured depending on the logic employed by "greenzone" management
		/// </summary>
		public void Capture(bool force = false)
		{
			bool shouldCapture;
			int frame = Global.Emulator.Frame;

			if (_movie.StartsFromSavestate && frame == 0) // Never capture frame 0 on savestate anchored movies since we have it anyway
			{
				shouldCapture = false;
			}
			else if (force)
			{
				shouldCapture = force;
			}
			else if (frame == 0) // For now, long term, TasMovie should have a .StartState property, and a tasproj file for the start state in non-savestate anchored movies
			{
				shouldCapture = true;
			}
			else if (StateIsMarker(frame))
			{
				shouldCapture = true; // Markers shoudl always get priority
			}
			else
			{
				shouldCapture = frame % _stateFrequency == 0;
			}

			if (shouldCapture)
			{
				SetState(frame, (byte[])Core.SaveStateBinary().Clone(), skipRemoval: false);
			}
		}

		private void MoveStateToDisk(int index)
		{
			Used -= (ulong)_states[index].Length;
			_states[index].MoveToDisk();
		}

		private void MoveStateToMemory(int index)
		{
			_states[index].MoveToRAM();
			Used += (ulong)_states[index].Length;
		}

		internal void SetState(int frame, byte[] state, bool skipRemoval = true)
		{
			if (!skipRemoval) // skipRemoval: false only when capturing new states
			{
				LimitStateCount(); // Remove before adding so this state won't be removed.
			}

			if (_states.ContainsKey(frame))
			{
				_states[frame].State = state;
			}
			else
			{
				Used += (ulong)state.Length;
				_states.Add(frame, new StateManagerState(this, state, frame));
			}
		}

		public bool HasState(int frame)
		{
			if (_movie.StartsFromSavestate && frame == 0)
			{
				return true;
			}

			return _states.ContainsKey(frame);
		}

		/// <summary>
		/// Clears out all savestates after the given frame number
		/// </summary>
		public bool Invalidate(int frame)
		{
			bool anyInvalidated = false;

			if (Any())
			{
				if (frame == 0) // Never invalidate frame 0
				{
					frame = 1;
				}

				List<KeyValuePair<int, StateManagerState>> statesToRemove = _states.Where(s => s.Key >= frame).ToList();
				anyInvalidated = statesToRemove.Any();

				foreach (var state in statesToRemove)
				{
					RemoveState(state.Key);
				}

				CallInvalidateCallback(frame);
			}

			return anyInvalidated;
		}

		public bool StateIsMarker(int frame)
		{
			if (frame == -1)
			{
				return false;
			}

			return _movie.Markers.IsMarker(frame + 1);
		}

		public bool RemoveState(int frame)
		{
			int index = _states.IndexOfKey(frame);

			if (frame < 1 || index < 1)
			{
				return false;
			}

			StateManagerState state = _states.Values[index];

			if (state.IsOnDisk)
			{
				state.Dispose();
			}
			else
			{
				Used -= (ulong)state.Length;
			}

			_states.RemoveAt(index);

			return true;
		}

		/// <summary>
		/// Deletes states to follow the state storage size limits.
		/// Used after changing the settings too.
		/// </summary>
		public void LimitStateCount()
		{
			if (StateCount + 1 > _maxStates || DiskUsed > (ulong)Settings.DiskCapacitymb * 1024 * 1024)
			{
				_decay.Trigger(StateCount + 1 - _maxStates);
			}
		}

		private List<int> ExcludeStates()
		{
			List<int> ret = new List<int>();
			ulong saveUsed = Used + DiskUsed;

			// respect state gap no matter how small the resulting size will be
			// still leave marker states
			for (int i = 1; i < _states.Count; i++)
			{
				int frame = GetStateFrameByIndex(i);

				if (StateIsMarker(frame) || frame % _fileStateGap < _stateFrequency)
				{
					continue;
				}

				ret.Add(i);

				if (_states.Values[i].IsOnDisk)
				{
					saveUsed -= _expectedStateSize;
				}
				else
				{
					saveUsed -= (ulong)_states.Values[i].Length;
				}
			}

			// if the size is still too big, exclude states form the beginning
			// still leave marker states
			int index = 0;
			while (saveUsed > (ulong)Settings.DiskSaveCapacitymb * 1024 * 1024)
			{
				do
				{
					if (++index >= _states.Count)
					{
						break;
					}
				}
				while (StateIsMarker(GetStateFrameByIndex(index)));

				if (index >= _states.Count)
				{
					break;
				}

				ret.Add(index);

				if (_states.Values[index].IsOnDisk)
				{
					saveUsed -= _expectedStateSize;
				}
				else
				{
					saveUsed -= (ulong)_states.Values[index].Length;
				}
			}

			// if there are enough markers to still be over the limit, remove marker frames
			index = 0;
			while (saveUsed > (ulong)Settings.DiskSaveCapacitymb * 1024 * 1024)
			{
				if (!ret.Contains(++index))
				{
					ret.Add(index);
				}

				if (_states.Values[index].IsOnDisk)
				{
					saveUsed -= _expectedStateSize;
				}
				else
				{
					saveUsed -= (ulong)_states.Values[index].Length;
				}
			}

			return ret;
		}

		public void ClearStateHistory()
		{
			if (_states.Any())
			{
				StateManagerState power = _states.Values.First(s => s.Frame == 0);
				_states.Clear();
				SetState(0, power.State);
				Used = (ulong)power.State.Length;
				NdbDatabase?.Clear();
			}
		}

		// Map:
		// 4 bytes - total savestate count
		// [Foreach state]
		// 4 bytes - frame
		// 4 bytes - length of savestate
		// 0 - n savestate
		public void Save(BinaryWriter bw)
		{
			List<int> noSave = ExcludeStates();
			bw.Write(_states.Count - noSave.Count);

			for (int i = 0; i < _states.Count; i++)
			{
				if (noSave.Contains(i))
				{
					continue;
				}
				
				bw.Write(_states.Keys[i]);
				bw.Write(_states.Values[i].Length);
				bw.Write(_states.Values[i].State);
			}
		}

		public void Load(BinaryReader br)
		{
			_states.Clear();

			try
			{
				int nstates = br.ReadInt32();

				for (int i = 0; i < nstates; i++)
				{
					int frame = br.ReadInt32();
					int len = br.ReadInt32();
					byte[] data = br.ReadBytes(len);

					// whether we should allow state removal check here is an interesting question
					// nothing was edited yet, so it might make sense to show the project untouched first
					SetState(frame, data);
				}
			}
			catch (EndOfStreamException)
			{
			}
		}

		public KeyValuePair<int, byte[]> GetStateClosestToFrame(int frame)
		{
			var s = _states.LastOrDefault(state => state.Key < frame);

			return this[s.Key];
		}

		/// <summary>
		/// Returns index of the state right above the given frame
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public int GetStateIndexByFrame(int frame)
		{
			return _states.IndexOfKey(GetStateClosestToFrame(frame).Key);
		}

		/// <summary>
		/// Returns frame of the state at the given index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int GetStateFrameByIndex(int index)
		{
			// feos: this is called super often by decay
			// this method is hundred times faster than _states.ElementAt(index).Key
			return _states.Keys[index];
		}

		private ulong _used;
		private ulong Used
		{
			get
			{
				return _used;
			}

			set
			{
				// TODO: Shouldn't we throw an exception? Debug.Fail only runs in debug mode?
				if (value > 0xf000000000000000)
				{
					System.Diagnostics.Debug.Fail("ulong Used underfow!");
				}
				else
				{
					_used = value;
				}
			}
		}

		private ulong DiskUsed
		{
			get
			{
				if (NdbDatabase == null)
				{
					return 0;
				}

				return (ulong)NdbDatabase.Consumed;
			}
		}

		public int StateCount => _states.Count;
		public int LastEditedFrame => _movie.LastEditedFrame;

		public bool Any()
		{
			if (_movie.StartsFromSavestate)
			{
				return _states.Count > 0;
			}

			return _states.Count > 1;
		}

		public int LastKey
		{
			get
			{
				if (_states.Count == 0)
				{
					return 0;
				}

				return _states.Last().Key;
			}
		}

		public int LastStatedFrame
		{
			get
			{
				if (StateCount > 0)
				{
					return LastKey;
				}

				return 0;
			}
		}

		private int FindState(StateManagerState s)
		{
			if (!_states.ContainsValue(s))
			{
				return -1;
			}

			return s.Frame;
		}
	}
}
