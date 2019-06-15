using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Captures savestates and manages the logic of adding, retrieving, 
	/// invalidating/clearing of states.  Also does memory management and limiting of states
	/// </summary>
	public class TasStateManager : IStateManager
	{
		// TODO: pass this in, and find a solution to a stale reference (this is instantiated BEFORE a new core instance is made, making this one stale if it is simply set in the constructor
		private IStatable Core => Global.Emulator.AsStatable();
		private readonly StateManagerDecay _decay;

		public Action<int> InvalidateCallback { get; set; }

		private void CallInvalidateCallback(int index)
		{
			InvalidateCallback?.Invoke(index);
		}

		private SortedList<int, StateManagerState> _states = new SortedList<int, StateManagerState>();

		private bool _isMountedForWrite;
		private readonly TasMovie _movie;

		private ulong _expectedStateSize;
		private int _stateFrequency;
		private readonly int _minFrequency = 1;
		private readonly int _maxFrequency = 16;
		private int MaxStates => (int)(Settings.Cap / _expectedStateSize) +
			(int)((ulong)Settings.DiskCapacitymb * 1024 * 1024 / _expectedStateSize);
		private int FileStateGap => 1 << Settings.FileStateGap;

		public TasStateManager(TasMovie movie)
		{
			_movie = movie;
			Settings = new TasStateManagerSettings(Global.Config.DefaultTasProjSettings);

			if (_movie.StartsFromSavestate)
			{
				SetState(0, _movie.BinarySavestate);
			}

			_decay = new StateManagerDecay(_movie, this);
		}

		public void UpdateStateFrequency()
		{
			_stateFrequency = ((int)_expectedStateSize / Settings.MemStateGapDivider / 1024)
				.Clamp(_minFrequency, _maxFrequency);

			_decay.UpdateSettings(MaxStates, _stateFrequency, 4);
			LimitStateCount();
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
				limit = MaxStates;
			}

			_states = new SortedList<int, StateManagerState>(limit);

			if (_expectedStateSize > int.MaxValue)
			{
				throw new InvalidOperationException();
			}
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

		private byte[] InitialState
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
		/// Unless force is true, the state may or may not be captured depending on the logic employed by "green-zone" management
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
				shouldCapture = true;
			}
			else if (frame == 0) // For now, long term, TasMovie should have a .StartState property, and a .tasproj file for the start state in non-savestate anchored movies
			{
				shouldCapture = true;
			}
			else if (IsMarkerState(frame))
			{
				shouldCapture = true; // Markers should always get priority
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
				_used += (ulong)state.Length;
				_states.Add(frame, new StateManagerState(state, frame));
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

		public bool IsMarkerState(int frame)
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

			StateManagerState state = _states.Values[index]; // TODO: remove .Values here?

			_used -= (ulong)state.Length;

			_states.RemoveAt(index);

			return true;
		}

		// Deletes states to follow the state storage size limits.
		// Used after changing the settings too.
		private void LimitStateCount()
		{
			if (Count + 1 > MaxStates)
			{
				_decay.Trigger(Count + 1 - MaxStates);
			}
		}

		private List<int> ExcludeStates()
		{
			List<int> ret = new List<int>();
			ulong saveUsed = _used;

			// respect state gap no matter how small the resulting size will be
			// still leave marker states
			for (int i = 1; i < _states.Count; i++)
			{
				int frame = GetStateFrameByIndex(i);

				if (IsMarkerState(frame) || frame % FileStateGap < _stateFrequency)
				{
					continue;
				}

				ret.Add(i);

				saveUsed -= (ulong)_states.Values[i].Length;
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
				while (IsMarkerState(GetStateFrameByIndex(index)));

				if (index >= _states.Count)
				{
					break;
				}

				ret.Add(index);
				saveUsed -= (ulong)_states.Values[index].Length;
			}

			// if there are enough markers to still be over the limit, remove marker frames
			index = 0;
			while (saveUsed > (ulong)Settings.DiskSaveCapacitymb * 1024 * 1024)
			{
				if (!ret.Contains(++index))
				{
					ret.Add(index);
				}

				saveUsed -= (ulong)_states.Values[index].Length;
			}

			return ret;
		}

		public void Clear()
		{
			if (_states.Any())
			{
				var tempState = _states.Values;
				var power = tempState[0].Frame == 0
					? _states.Values.First(s => s.Frame == 0)
					: _states.Values[0];
				
				_states.Clear();
				SetState(0, power.State);
				_used = (ulong)power.State.Length;
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
		public int GetStateIndexByFrame(int frame)
		{
			return _states.IndexOfKey(GetStateClosestToFrame(frame).Key);
		}

		/// <summary>
		/// Returns frame of the state at the given index
		/// </summary>
		public int GetStateFrameByIndex(int index)
		{
			return _states.Keys[index];
		}

		private ulong _used;

		public int Count => _states.Count;

		public bool Any()
		{
			if (_movie.StartsFromSavestate)
			{
				return _states.Count > 0;
			}

			return _states.Count > 1;
		}

		public int Last => _states.Count > 0
			? _states.Last().Key
			: 0;
	}
}
