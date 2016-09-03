using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;

using BizHawk.Common;
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
		private IStatable Core
		{
			get
			{
				return Global.Emulator.AsStatable();
			}
		}

		public Action<int> InvalidateCallback { get; set; }

		private void CallInvalidateCallback(int index)
		{
			if (InvalidateCallback != null)
			{
				InvalidateCallback(index);
			}
		}

		private List<StateManagerState> lowPriorityStates = new List<StateManagerState>();
		internal NDBDatabase ndbdatabase;
		private Guid guid = Guid.NewGuid();
		private SortedList<int, StateManagerState> States = new SortedList<int, StateManagerState>();

		private string statePath
		{
			get
			{
				var basePath = PathManager.MakeAbsolutePath(Global.Config.PathEntries["Global", "TAStudio states"].Path, null);
				return Path.Combine(basePath, guid.ToString());
			}
		}

		private bool _isMountedForWrite;
		private readonly TasMovie _movie;
		private ulong _expectedStateSize = 0;

		private int _minFrequency = VersionInfo.DeveloperBuild ? 2 : 1;
		private const int _maxFrequency = 16;

		private int StateFrequency
		{
			get
			{
				int freq = (int)(_expectedStateSize / 65536);

				if (freq < _minFrequency)
				{
					return _minFrequency;
				}

				if (freq > _maxFrequency)
				{
					return _maxFrequency;
				}

				return freq;
			}
		}

		private int maxStates
		{
			get { return (int)(Settings.Cap / _expectedStateSize) + (int)((ulong)Settings.DiskCapacitymb * 1024 * 1024 / _expectedStateSize); }
		}

		private int _stateGap
		{
			get { return 1 << Settings.StateGap; }
		}

		public TasStateManager(TasMovie movie)
		{
			_movie = movie;

			Settings = new TasStateManagerSettings(Global.Config.DefaultTasProjSettings);

			accessed = new List<StateManagerState>();

			if (_movie.StartsFromSavestate)
				SetState(0, _movie.BinarySavestate);
		}

		public void Dispose()
		{
			if (ndbdatabase != null)
				ndbdatabase.Dispose();

			//States and BranchStates don't need cleaning because they would only contain an ndbdatabase entry which was demolished by the above
		}

		/// <summary>
		/// Mounts this instance for write access. Prior to that it's read-only
		/// </summary>
		public void MountWriteAccess()
		{
			if (_isMountedForWrite)
				return;

			_isMountedForWrite = true;

			int limit = 0;

			_expectedStateSize = (ulong)Core.SaveStateBinary().Length;

			if (_expectedStateSize > 0)
			{
				limit = maxStates;
			}

			States = new SortedList<int, StateManagerState>(limit);

			if (_expectedStateSize > int.MaxValue)
				throw new InvalidOperationException();
			ndbdatabase = new NDBDatabase(statePath, Settings.DiskCapacitymb * 1024 * 1024, (int)_expectedStateSize);
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

				if (States.ContainsKey(frame))
				{
					StateAccessed(frame);
					return new KeyValuePair<int, byte[]>(frame, States[frame].State);
				}

				return new KeyValuePair<int, byte[]>(-1, new byte[0]);
			}
		}

		private List<StateManagerState> accessed;

		public byte[] InitialState
		{
			get
			{
				if (_movie.StartsFromSavestate)
				{
					return _movie.BinarySavestate;
				}

				return States[0].State;
			}
		}

		/// <summary>
		/// Requests that the current emulator state be captured 
		/// Unless force is true, the state may or may not be captured depending on the logic employed by "greenzone" management
		/// </summary>
		public void Capture(bool force = false)
		{
			bool shouldCapture = false;

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
			else if (_movie.Markers.IsMarker(frame + 1))
			{
				shouldCapture = true; // Markers shoudl always get priority
			}
			else
			{
				shouldCapture = frame % StateFrequency == 0;
			}

			if (shouldCapture)
			{
				SetState(frame, (byte[])Core.SaveStateBinary().Clone(), skipRemoval: false);
			}
		}

		private void MaybeRemoveStates()
		{
			// Loop, because removing a state that has a duplicate won't save any space
			while (Used + _expectedStateSize > Settings.Cap || DiskUsed > (ulong)Settings.DiskCapacitymb * 1024 * 1024)
			{
				Point shouldRemove = StateToRemove();
				RemoveState(shouldRemove.X, shouldRemove.Y);
			}

			if (Used > Settings.Cap)
			{
				int lastMemState = -1;
				do { lastMemState++; } while (States[accessed[lastMemState].Frame] == null);
				MoveStateToDisk(accessed[lastMemState].Frame);
			}
		}

		/// <summary>
		/// X is the frame of the state, Y is the branch (-1 for current).
		/// </summary>
		private Point StateToRemove()
		{
			// X is frame, Y is branch
			Point shouldRemove = new Point(-1, -1);

			if (BranchStates.Any() && Settings.EraseBranchStatesFirst)
			{
				var kvp = BranchStates.Count() > 1 ? BranchStates.ElementAt(1) : BranchStates.ElementAt(0);
				shouldRemove.X = kvp.Key;
				shouldRemove.Y = kvp.Value.Keys[0];

				return shouldRemove;
			}

			int i = 0;
			int markerSkips = maxStates / 2;
			// lowPrioritySates (e.g. states with only lag frames between them)
			do
			{
				if (lowPriorityStates.Count > i)
					shouldRemove = findState(lowPriorityStates[i]);
				else
					break;

				// Keep marker states
				markerSkips--;
				if (markerSkips < 0)
					shouldRemove.X = -1;
				i++;
			} while (StateIsMarker(shouldRemove.X, shouldRemove.Y) && markerSkips > -1 || shouldRemove.X == 0);

			// by last accessed
			markerSkips = maxStates / 2;
			if (shouldRemove.X < 1)
			{
				i = 0;
				do
				{
					if (accessed.Count > i)
						shouldRemove = findState(accessed[i]);
					else
						break;

					// Keep marker states
					markerSkips--;
					if (markerSkips < 0)
						shouldRemove.X = -1;
					i++;
				} while (StateIsMarker(shouldRemove.X, shouldRemove.Y) && markerSkips > -1 || shouldRemove.X == 0);
			}

			if (shouldRemove.X < 1) // only found marker states above
			{
				if (BranchStates.Any() && !Settings.EraseBranchStatesFirst)
				{
					var kvp = BranchStates.Count() > 1 ? BranchStates.ElementAt(1) : BranchStates.ElementAt(0);
					shouldRemove.X = kvp.Key;
					shouldRemove.Y = kvp.Value.Keys[0];
				}
				else
				{
					StateManagerState s = States.Values[1];
					shouldRemove.X = s.Frame;
					shouldRemove.Y = -1;
				}
			}

			return shouldRemove;
		}

		private bool StateIsMarker(int frame, int branch)
		{
			if (frame == -1)
				return false;

			if (branch == -1)
				return _movie.Markers.IsMarker(States[frame].Frame + 1);
			else
			{
				if (_movie.GetBranch(_movie.BranchIndexByHash(branch)).Markers == null)
					return _movie.Markers.IsMarker(States[frame].Frame + 1);
				else
					return _movie.GetBranch(branch).Markers.Any(m => m.Frame + 1 == frame);
			}
		}

		private bool AllLag(int from, int upTo)
		{
			if (upTo >= Global.Emulator.Frame)
			{
				upTo = Global.Emulator.Frame - 1;
				if (!Global.Emulator.AsInputPollable().IsLagFrame)
					return false;
			}

			for (int i = from; i < upTo; i++)
			{
				if (_movie[i].Lagged == false)
					return false;
			}

			return true;
		}

		private void MoveStateToDisk(int index)
		{
			Used -= (ulong)States[index].Length;
			States[index].MoveToDisk();
		}

		private void MoveStateToMemory(int index)
		{
			States[index].MoveToRAM();
			Used += (ulong)States[index].Length;
		}

		internal void SetState(int frame, byte[] state, bool skipRemoval = true)
		{
			if (!skipRemoval) // skipRemoval: false only when capturing new states
				MaybeRemoveStates(); // Remove before adding so this state won't be removed.

			if (States.ContainsKey(frame))
			{
				if (stateHasDuplicate(frame, -1) != -2)
					Used += (ulong)state.Length;
				States[frame].State = state;
			}
			else
			{
				Used += (ulong)state.Length;
				States.Add(frame, new StateManagerState(this, state, frame));
			}

			StateAccessed(frame);

			int i = States.IndexOfKey(frame);
			if (i > 0 && AllLag(States.Keys[i - 1], States.Keys[i]))
			{
				lowPriorityStates.Add(States[frame]);
			}
		}

		private void RemoveState(int frame, int branch = -1)
		{
			if (branch == -1)
				accessed.Remove(States[frame]);
			else if (accessed.Contains(BranchStates[frame][branch]) && !Settings.EraseBranchStatesFirst)
				accessed.Remove(BranchStates[frame][branch]);

			StateManagerState state;
			bool hasDuplicate = stateHasDuplicate(frame, branch) != -2;
			if (branch == -1)
			{
				state = States[frame];
				if (States[frame].IsOnDisk)
					States[frame].Dispose();
				else
					Used -= (ulong)States[frame].Length;
				States.RemoveAt(States.IndexOfKey(frame));
			}
			else
			{
				state = BranchStates[frame][branch];
				if (BranchStates[frame][branch].IsOnDisk)
					BranchStates[frame][branch].Dispose();
				else
					//Used -= (ulong)BranchStates[frame][branch].Length;
				BranchStates[frame].RemoveAt(BranchStates[frame].IndexOfKey(branch));

				if (BranchStates[frame].Count == 0)
					BranchStates.Remove(frame);
			}

			if (!hasDuplicate)
				lowPriorityStates.Remove(state);
		}

		private void StateAccessed(int frame)
		{
			if (frame == 0 && _movie.StartsFromSavestate)
				return;

			StateManagerState state = States[frame];
			bool removed = accessed.Remove(state);
			accessed.Add(state);

			if (States[frame].IsOnDisk)
			{
				if (!States[accessed[0].Frame].IsOnDisk)
					MoveStateToDisk(accessed[0].Frame);
				MoveStateToMemory(frame);
			}

			if (!removed && accessed.Count > maxStates)
				accessed.RemoveAt(0);
		}

		public bool HasState(int frame)
		{
			if (_movie.StartsFromSavestate && frame == 0)
			{
				return true;
			}

			return States.ContainsKey(frame);
		}

		/// <summary>
		/// Clears out all savestates after the given frame number
		/// </summary>
		public bool Invalidate(int frame)
		{
			bool anyInvalidated = false;

			if (Any())
			{
				if (!_movie.StartsFromSavestate && frame == 0) // Never invalidate frame 0 on a non-savestate-anchored movie
				{
					frame = 1;
				}

				List<KeyValuePair<int, StateManagerState>> statesToRemove =
					States.Where(x => x.Key >= frame).ToList();

				anyInvalidated = statesToRemove.Any();

				foreach (KeyValuePair<int, StateManagerState> state in statesToRemove)
					RemoveState(state.Key);

				CallInvalidateCallback(frame);
			}

			return anyInvalidated;
		}

		/// <summary>
		/// Clears all state information
		/// </summary>
		/// 
		public void Clear()
		{
			States.Clear();
			accessed.Clear();
			Used = 0;
			clearDiskStates();
		}

		public void ClearStateHistory()
		{
			if (States.Any())
			{
				StateManagerState power = States.Values.FirstOrDefault(s => s.Frame == 0);
				StateAccessed(power.Frame);

				States.Clear();
				accessed.Clear();

				SetState(0, power.State);
				Used = (ulong)power.State.Length;

				clearDiskStates();
			}
		}

		private void clearDiskStates()
		{
			if (ndbdatabase != null)
				ndbdatabase.Clear();
		}

		/// <summary>
		/// Deletes/moves states to follow the state storage size limits.
		/// Used after changing the settings.
		/// </summary>
		public void LimitStateCount()
		{
			while (Used + DiskUsed > Settings.CapTotal)
			{
				Point s = StateToRemove();
				RemoveState(s.X, s.Y);
			}

			int index = -1;
			while (DiskUsed > (ulong)Settings.DiskCapacitymb * 1024uL * 1024uL)
			{
				do { index++; } while (!accessed[index].IsOnDisk);
				accessed[index].MoveToRAM();
			}

			if (Used > Settings.Cap)
				MaybeRemoveStates();
		}

		private List<int> ExcludeStates()
		{
			List<int> ret = new List<int>();
			ulong saveUsed = Used + DiskUsed;

			// respect state gap no matter how small the resulting size will be
			// still leave marker states
			for (int i = 1; i < States.Count; i++)
			{
				if (_movie.Markers.IsMarker(States.ElementAt(i).Key + 1) ||
					States.ElementAt(i).Key % _stateGap == 0)
					continue;

				ret.Add(i);

				if (States.ElementAt(i).Value.IsOnDisk)
					saveUsed -= _expectedStateSize;
				else
					saveUsed -= (ulong)States.ElementAt(i).Value.Length;
			}

			// if the size is still too big, exclude states form the beginning
			// still leave marker states
			int index = 0;
			while (saveUsed > (ulong)Settings.DiskSaveCapacitymb * 1024 * 1024)
			{
				do
				{
					index++;
					if (index >= States.Count)
						break;
				}
				while (_movie.Markers.IsMarker(States.ElementAt(index).Key + 1));

				if (index >= States.Count)
					break;

				ret.Add(index);

				if (States.ElementAt(index).Value.IsOnDisk)
					saveUsed -= _expectedStateSize;
				else
					saveUsed -= (ulong)States.ElementAt(index).Value.Length;
			}

			// if there are enough markers to still be over the limit, remove marker frames
			index = 0;
			while (saveUsed > (ulong)Settings.DiskSaveCapacitymb * 1024 * 1024)
			{
				index++;
				if (!ret.Contains(index))
					ret.Add(index);
				if (States.ElementAt(index).Value.IsOnDisk)
					saveUsed -= _expectedStateSize;
				else
					saveUsed -= (ulong)States.ElementAt(index).Value.Length;
			}

			return ret;
		}

		public void Save(BinaryWriter bw)
		{
			List<int> noSave = ExcludeStates();

			bw.Write(States.Count - noSave.Count);
			for (int i = 0; i < States.Count; i++)
			{
				if (noSave.Contains(i))
					continue;

				StateAccessed(States.ElementAt(i).Key);
				KeyValuePair<int, StateManagerState> kvp = States.ElementAt(i);
				bw.Write(kvp.Key);
				bw.Write(kvp.Value.Length);
				bw.Write(kvp.Value.State);
				//_movie.ReportProgress(100d / States.Count * i);
			}
		}

		public void Load(BinaryReader br)
		{
			States.Clear();
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
					//States.Add(frame, data);
					//Used += len;
				}
			}
			catch (EndOfStreamException) { }
		}

		public void SaveBranchStates(BinaryWriter bw)
		{
			bw.Write(BranchStates.Count);
			foreach (var s in BranchStates)
			{
				bw.Write(s.Key);
				bw.Write(s.Value.Count);
				foreach (var t in s.Value)
				{
					bw.Write(t.Key);
					t.Value.Write(bw);
				}
			}
		}

		public void LoadBranchStates(BinaryReader br)
		{
			try
			{
				int c = br.ReadInt32();
				BranchStates = new SortedList<int, SortedList<int, StateManagerState>>(c);
				while (c > 0)
				{
					int key = br.ReadInt32();
					int c2 = br.ReadInt32();
					var list = new SortedList<int, StateManagerState>(c2);
					while (c2 > 0)
					{
						int key2 = br.ReadInt32();
						var state = StateManagerState.Read(br, this);
						list.Add(key2, state);
						c2--;
					}
					BranchStates.Add(key, list);
					c--;
				}
			}
			catch (EndOfStreamException) { }
		}


		public KeyValuePair<int, byte[]> GetStateClosestToFrame(int frame)
		{
			var s = States.LastOrDefault(state => state.Key < frame);

			return this[s.Key];
		}

		// Map:
		// 4 bytes - total savestate count
		//[Foreach state]
		// 4 bytes - frame
		// 4 bytes - length of savestate
		// 0 - n savestate

		private ulong _used;
		private ulong Used
		{
			get
			{
				return _used;
			}
			set
			{
				if (value > 0xf000000000000000)
					System.Diagnostics.Debug.Fail("ulong Used underfow!");
				else
					_used = value;
			}
		}

		private ulong DiskUsed
		{
			get
			{
				if (ndbdatabase == null) return 0;
				else return (ulong)ndbdatabase.Consumed;
			}
		}

		public int StateCount
		{
			get
			{
				return States.Count;
			}
		}

		public bool Any()
		{
			if (_movie.StartsFromSavestate)
			{
				return States.Count > 0;
			}

			return States.Count > 1;
		}

		public int LastKey
		{
			get
			{
				if (States.Count == 0)
				{
					return 0;
				}

				return States.Last().Key;
			}
		}

		public int LastEmulatedFrame
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

		#region "Branches"

		private SortedList<int, SortedList<int, StateManagerState>> BranchStates = new SortedList<int, SortedList<int, StateManagerState>>();

		/// <summary>
		/// Checks if the state at frame in the given branch (-1 for current) has any duplicates.
		/// </summary>
		/// <returns>Index of the branch (-1 for current) of the first match. If no match, returns -2.</returns>
		private int stateHasDuplicate(int frame, int branchHash)
		{
			StateManagerState stateToMatch;

			// Get the state instance
			if (branchHash == -1)
				stateToMatch = States[frame];
			else
			{
				if (!BranchStates[frame].ContainsKey(branchHash))
					return -2;
				stateToMatch = BranchStates[frame][branchHash];
				// Check the current branch for duplicate.
				if (States.ContainsKey(frame) && States[frame] == stateToMatch)
					return -1;
			}

			// Check if there are any branch states for the given frame.
			if (!BranchStates.ContainsKey(frame) || BranchStates[frame] == null)
				return -2;

			// Loop through branch states for the given frame.
			SortedList<int, StateManagerState> stateList = BranchStates[frame];
			for (int i = 0; i < stateList.Count(); i++)
			{
				// Don't check the branch containing the state to match.
				if (i == _movie.BranchIndexByHash(branchHash))
					continue;

				if (stateList.Values[i] == stateToMatch)
					return i;
			}

			return -2; // No match.
		}

		private Point findState(StateManagerState s)
		{
			Point ret = new Point(0, -1);
			ret.X = s.Frame;
			if (!States.ContainsValue(s))
			{
				if (BranchStates.ContainsKey(s.Frame))
				{
					int index = BranchStates[s.Frame].Values.IndexOf(s);
					ret.Y = BranchStates[s.Frame].Keys.ElementAt(index);
				}
				if (ret.Y == -1)
					return new Point(-1, -2);
			}

			return ret;
		}

		public void AddBranch()
		{
			int branchHash = _movie.BranchHashByIndex(_movie.BranchCount - 1);

			foreach (KeyValuePair<int, StateManagerState> kvp in States)
			{
				if (!BranchStates.ContainsKey(kvp.Key))
					BranchStates.Add(kvp.Key, new SortedList<int, StateManagerState>());

				SortedList<int, StateManagerState> stateList = BranchStates[kvp.Key];

				if (stateList == null) // when does this happen?
				{
					stateList = new SortedList<int, StateManagerState>();
					BranchStates[kvp.Key] = stateList;
				}
				stateList.Add(branchHash, kvp.Value);
				// We aren't creating any new states, just adding a reference to an already-existing one; so Used doesn't need to be updated.
			}
		}

		public void RemoveBranch(int index)
		{
			int branchHash = _movie.BranchHashByIndex(index);

			foreach (KeyValuePair<int, SortedList<int, StateManagerState>> kvp in BranchStates.ToList())
			{
				SortedList<int, StateManagerState> stateList = kvp.Value;
				if (stateList == null)
					continue;
				/*
				if (stateList.ContainsKey(branchHash))
				{
					if (stateHasDuplicate(kvp.Key, branchHash) == -2)
					{
						if (!stateList[branchHash].IsOnDisk)
							Used -= (ulong)stateList[branchHash].Length;
					}
				}
				*/
				stateList.Remove(branchHash);
				if (stateList.Count == 0)
					BranchStates.Remove(kvp.Key);
			}
		}

		public void UpdateBranch(int index)
		{
			int branchHash = _movie.BranchHashByIndex(index);

			// RemoveBranch
			foreach (KeyValuePair<int, SortedList<int, StateManagerState>> kvp in BranchStates.ToList())
			{
				SortedList<int, StateManagerState> stateList = kvp.Value;
				if (stateList == null)
					continue;
				/*
				if (stateList.ContainsKey(branchHash))
				{
					if (stateHasDuplicate(kvp.Key, branchHash) == -2)
					{
						if (!stateList[branchHash].IsOnDisk)
							Used -= (ulong)stateList[branchHash].Length;
					}
				}
				*/
				stateList.Remove(branchHash);
				if (stateList.Count == 0)
					BranchStates.Remove(kvp.Key);
			}

			// AddBranch
			foreach (KeyValuePair<int, StateManagerState> kvp in States)
			{
				if (!BranchStates.ContainsKey(kvp.Key))
					BranchStates.Add(kvp.Key, new SortedList<int, StateManagerState>());

				SortedList<int, StateManagerState> stateList = BranchStates[kvp.Key];

				if (stateList == null)
				{
					stateList = new SortedList<int, StateManagerState>();
					BranchStates[kvp.Key] = stateList;
				}
				stateList.Add(branchHash, kvp.Value);
			}
		}

		public void LoadBranch(int index)
		{
			int branchHash = _movie.BranchHashByIndex(index);

			//Invalidate(0); // Not a good way of doing it?

			foreach (KeyValuePair<int, SortedList<int, StateManagerState>> kvp in BranchStates)
			{
				if (kvp.Key == 0 && States.ContainsKey(0))
					continue; // TODO: It might be a better idea to just not put state 0 in BranchStates.

				if (kvp.Value.ContainsKey(branchHash))
					SetState(kvp.Key, kvp.Value[branchHash].State);
			}
		}

		#endregion
	}
}
