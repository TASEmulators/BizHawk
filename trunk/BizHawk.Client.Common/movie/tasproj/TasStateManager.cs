using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Captures savestates and manages the logic of adding, retrieving, 
	/// invalidating/clearing of states.  Also does memory management and limiting of states
	/// </summary>
	public class TasStateManager
	{
		private readonly SortedList<int, byte[]> States = new SortedList<int, byte[]>();

		private readonly TasMovie _movie;
		private int _expectedStateSize = 0;

		private const int _minFrequency = 2;
		private const int _maxFrequency = 16; 
		private int StateFrequency
		{
			get
			{
				var freq = _expectedStateSize / 65536;

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

		public TasStateManager(TasMovie movie)
		{
			_movie = movie;
			Settings = new TasStateManagerSettings(Global.Config.DefaultTasProjSettings);

			var cap = Settings.Cap;

			int limit = 0;
			if (Global.Emulator != null)
			{
				_expectedStateSize = Global.Emulator.SaveStateBinary().Length;

				if (_expectedStateSize > 0)
				{
					limit = cap / _expectedStateSize;
				}
			}

			States = new SortedList<int, byte[]>(limit);
		}

		public TasStateManagerSettings Settings { get; set; }

		/// <summary>
		/// Retrieves the savestate for the given frame,
		/// If this frame does not have a state currently, will return an empty array
		/// </summary>
		/// <returns>A savestate for the given frame or an empty array if there isn't one</returns>
		public byte[] this[int frame]
		{
			get
			{
				if (frame == 0 && _movie.StartsFromSavestate)
				{
					return _movie.BinarySavestate;
				}

				if (States.ContainsKey(frame))
				{
					return States[frame];
				}

				return new byte[0];
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

				return States[0];
			}
		}

		/// <summary>
		/// Requests that the current emulator state be captured 
		/// Unless force is true, the state may or may not be captured depending on the logic employed by "greenzone" management
		/// </summary>
		public void Capture(bool force = false)
		{
			bool shouldCapture = false;

			if (_movie.StartsFromSavestate && Global.Emulator.Frame == 0) // Never capture frame 0 on savestate anchored movies since we have it anyway
			{
				shouldCapture = false;
			}
			else if (force)
			{
				shouldCapture = force;
			}
			else if (Global.Emulator.Frame == 0) // For now, long term, TasMovie should have a .StartState property, and a tasproj file for the start state in non-savestate anchored movies
			{
				shouldCapture = true;
			}
			else if (_movie.Markers.IsMarker(Global.Emulator.Frame))
			{
				shouldCapture = true; // Markers shoudl always get priority
			}
			else
			{
				shouldCapture = Global.Emulator.Frame % StateFrequency == 0;
			}

			if (shouldCapture)
			{
				var frame = Global.Emulator.Frame;
				var state = (byte[])Global.Emulator.SaveStateBinary().Clone();

				if (States.ContainsKey(frame))
				{
					States[frame] = state;
				}
				else
				{
					if (Used + state.Length >= Settings.Cap)
					{
						var first = _movie.StartsFromSavestate ? 0 : 1;

						Used -= States.ElementAt(first).Value.Length;
						States.RemoveAt(first);
					}

					States.Add(frame, state);
					Used += state.Length;
				}
			}
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
		public void Invalidate(int frame)
		{
			if (Any())
			{
				if (!_movie.StartsFromSavestate && frame == 0) // Never invalidate frame 0 on a non-savestate-anchored movie
				{
					frame = 1;
				}

				var statesToRemove = States
					.Where(x => x.Key >= frame)
					.ToList();
				foreach (var state in statesToRemove)
				{
					Used -= state.Value.Length;
					States.Remove(state.Key);
				}
			}
		}

		/// <summary>
		/// Clears all state information
		/// </summary>
		/// 
		public void Clear()
		{
			States.Clear();
			Used = 0;
		}

		public void ClearGreenzone()
		{
			if (States.Any())
			{
				var power = States.FirstOrDefault(s => s.Key == 0);
				States.Clear();

				if (power.Value.Length > 0)
				{
					States.Add(0, power.Value);
					Used = power.Value.Length;
				}
				else
				{
					Used = 0;
				}
			}
		}

		public void Save(BinaryWriter bw)
		{
			bw.Write(States.Count);
			foreach (var kvp in States)
			{
				bw.Write(kvp.Key);
				bw.Write(kvp.Value.Length);
				bw.Write(kvp.Value);
			}
		}

		public void Load(BinaryReader br)
		{
			States.Clear();
			int nstates = br.ReadInt32();
			for (int i = 0; i < nstates; i++)
			{
				int frame = br.ReadInt32();
				int len = br.ReadInt32();
				byte[] data = br.ReadBytes(len);
				States.Add(frame, data);
				Used += len;
			}
		}

		public KeyValuePair<int, byte[]> GetStateClosestToFrame(int frame)
		{
			var s = States.LastOrDefault(state => state.Key < frame);

			if (s.Value == null && _movie.StartsFromSavestate)
			{
				return new KeyValuePair<int, byte[]>(0, _movie.BinarySavestate);
			}

			return s;
		}

		// Map:
		// 4 bytes - total savestate count
		//[Foreach state]
		// 4 bytes - frame
		// 4 bytes - length of savestate
		// 0 - n savestate

		private int Used
		{
			get;
			set;
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
				var kk = States.Keys;
				int index = kk.Count;
				if (index == 0)
				{
					return 0;
				}

				return kk[index - 1];
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
	}
}
