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

		public TasStateManager(TasMovie movie)
		{
			_movie = movie;
			Settings = new ManagerSettings();

			var cap = Settings.Cap;

			int limit = 0;
			if (Global.Emulator != null)
			{
				var stateSize = Global.Emulator.SaveStateBinary().Length;

				if (stateSize > 0)
				{
					limit = cap / stateSize;
				}
			}

			States = new SortedList<int, byte[]>(limit);
		}

		public ManagerSettings Settings { get; set; }

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
			if (force)
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
				shouldCapture = Global.Emulator.Frame % 2 > 0;
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
						Used -= States.ElementAt(0).Value.Length;
						States.RemoveAt(0);
					}

					States.Add(frame, state);
					Used += state.Length;
				}
			}
		}

		public bool HasState(int frame)
		{
			return States.ContainsKey(frame);
		}

		/// <summary>
		/// Clears out all savestates after the given frame number
		/// </summary>
		public void Invalidate(int frame)
		{
			if (States.Count > 0 && frame > 0) // Never invalidate frame 0, TODO: Only if movie is a power-on movie should we keep frame 0, check this
			{
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

		public byte[] GetStateClosestToFrame(int frame)
		{
			return States.LastOrDefault(state => state.Key < frame).Value;
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
			return States.Count > 1; // TODO: power-on MUST have a state, savestate-anchored movies do not, take this into account
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

		public class ManagerSettings
		{
			public ManagerSettings()
			{
				SaveGreenzone = true;
				Capacitymb = 512;
			}

			/// <summary>
			/// Whether or not to save greenzone information to disk
			/// </summary>
			public bool SaveGreenzone { get; set; }

			/// <summary>
			/// The total amount of memory to devote to greenzone in megabytes
			/// </summary>
			public int Capacitymb { get; set; }

			public int Cap
			{
				get { return Capacitymb * 1024 * 1024; }
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();

				sb.AppendLine(SaveGreenzone.ToString());
				sb.AppendLine(Capacitymb.ToString());

				return sb.ToString();
			}

			public void PopulateFromString(string settings)
			{
				var lines = settings.Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
				SaveGreenzone = bool.Parse(lines[0]);
				Capacitymb = int.Parse(lines[1]);
			}
		}
	}
}
