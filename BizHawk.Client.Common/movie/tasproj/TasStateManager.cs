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
		private const int MinFrequency = 1;
		private const int MaxFrequency = 16;

		// TODO: pass this in, and find a solution to a stale reference (this is instantiated BEFORE a new core instance is made, making this one stale if it is simply set in the constructor
		private IStatable Core => Global.Emulator.AsStatable();
		private readonly StateManagerDecay _decay;
		private readonly TasMovie _movie;

		private readonly SortedList<int, byte[]> _states;
		private readonly ulong _expectedStateSize;

		private ulong _used;
		private int _stateFrequency;
		
		private int MaxStates => (int)(Settings.Cap / _expectedStateSize) +
			(int)((ulong)Settings.DiskCapacityMb * 1024 * 1024 / _expectedStateSize);
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

			_expectedStateSize = (ulong)Core.SaveStateBinary().Length;
			if (_expectedStateSize == 0)
			{
				throw new InvalidOperationException("Savestate size can not be zero!");
			}

			_states = new SortedList<int, byte[]>(MaxStates);

			UpdateStateFrequency();
		}

		public Action<int> InvalidateCallback { get; set; }

		public TasStateManagerSettings Settings { get; set; }

		public byte[] this[int frame]
		{
			get
			{
				if (frame == 0)
				{
					return InitialState;
				}

				if (_states.ContainsKey(frame))
				{
					return _states[frame];
				}

				return new byte[0];
			}
		}

		public int Count => _states.Count;

		public int Last => _states.Count > 0
			? _states.Last().Key
			: 0;

		private byte[] InitialState =>
			_movie.StartsFromSavestate
				? _movie.BinarySavestate
				: _states[0];

		public bool Any()
		{
			if (_movie.StartsFromSavestate)
			{
				return _states.Count > 0;
			}

			return _states.Count > 1;
		}

		public void UpdateStateFrequency()
		{
			_stateFrequency = ((int)_expectedStateSize / Settings.MemStateGapDivider / 1024)
				.Clamp(MinFrequency, MaxFrequency);

			_decay.UpdateSettings(MaxStates, _stateFrequency, 4);
			LimitStateCount();
		}

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

		public void Clear()
		{
			if (_states.Any())
			{
				// For power-on movies, we can't lose frame 0;
				byte[] power = null;
				if (!_movie.StartsFromSavestate)
				{
					power = _states[0];
				}

				_states.Clear();

				if (power != null)
				{
					SetState(0, power);
					_used = (ulong)power.Length;
				}
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

		public bool Invalidate(int frame)
		{
			bool anyInvalidated = false;

			if (Any())
			{
				if (frame == 0) // Never invalidate frame 0
				{
					frame = 1;
				}

				List<KeyValuePair<int, byte[]>> statesToRemove = _states.Where(s => s.Key >= frame).ToList();
				anyInvalidated = statesToRemove.Any();

				foreach (var state in statesToRemove)
				{
					Remove(state.Key);
				}

				InvalidateCallback?.Invoke(frame);
			}

			return anyInvalidated;
		}

		public bool Remove(int frame)
		{
			int index = _states.IndexOfKey(frame);

			if (frame < 1 || index < 1)
			{
				return false;
			}

			var state = _states[frame];

			_used -= (ulong)state.Length;

			_states.RemoveAt(index);

			return true;
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
				bw.Write(_states.Values[i]);
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
			if (s.Key > 0)
			{
				return s;
			}

			return new KeyValuePair<int, byte[]>(0, InitialState);
		}

		public int GetStateIndexByFrame(int frame)
		{
			return _states.IndexOfKey(GetStateClosestToFrame(frame).Key);
		}

		public int GetStateFrameByIndex(int index)
		{
			return _states.Keys[index];
		}

		private bool IsMarkerState(int frame)
		{
			return _movie.Markers.IsMarker(frame + 1);
		}

		private void SetState(int frame, byte[] state, bool skipRemoval = true)
		{
			if (!skipRemoval) // skipRemoval: false only when capturing new states
			{
				LimitStateCount(); // Remove before adding so this state won't be removed.
			}

			if (_states.ContainsKey(frame))
			{
				_states[frame] = state;
			}
			else
			{
				_used += (ulong)state.Length;
				_states.Add(frame, state);
			}
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
			while (saveUsed > (ulong)Settings.DiskSaveCapacityMb * 1024 * 1024)
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
			while (saveUsed > (ulong)Settings.DiskSaveCapacityMb * 1024 * 1024)
			{
				if (!ret.Contains(++index))
				{
					ret.Add(index);
				}

				saveUsed -= (ulong)_states.Values[index].Length;
			}

			return ret;
		}
	}
}
