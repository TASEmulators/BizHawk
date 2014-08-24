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

		/// <summary>
		/// Requests that the current emulator state be captured 
		/// </summary>
		public void Capture()
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

		public bool HasState(int frame)
		{
			return States.ContainsKey(frame);
		}

		/// <summary>
		/// Clears out all savestates after the given frame number
		/// </summary>
		public void Invalidate(int frame)
		{
			if (States.Count == 0)
				return;
			// TODO be more efficient, this could get slow
			while (LastKey >= frame)
			{
				var state = States[LastKey];
				Used -= state.Length;
				States.RemoveAt(States.Count - 1);
			}
		}

		/// <summary>
		/// Clears all state information
		/// </summary>
		public void Clear()
		{
			States.Clear();
			Used = 0;
		}

		public byte[] ToArray()
		{
			MemoryStream ms = new MemoryStream();
			var bytes = BitConverter.GetBytes(States.Count);
			ms.Write(bytes, 0, bytes.Length);
			foreach (var kvp in States)
			{
				var frame = BitConverter.GetBytes(kvp.Key);
				ms.Write(frame, 0, frame.Length);

				var stateLen = BitConverter.GetBytes(kvp.Value.Length);
				ms.Write(stateLen, 0, stateLen.Length);
				ms.Write(kvp.Value, 0, kvp.Value.Length);
			}

			return ms.ToArray();
		}

		// Map:
		// 4 bytes - total savestate count
		//[Foreach state]
		// 4 bytes - frame
		// 4 bytes - length of savestate
		// 0 - n savestate
		public void FromArray(byte[] bytes)
		{
			var position = 0;
			var stateCount = BitConverter.ToInt32(bytes, 0);
			position += 4;
			for (int i = 0; i < stateCount; i++)
			{
				var frame = BitConverter.ToInt32(bytes, position);
				position += 4;
				var stateLen = BitConverter.ToInt32(bytes, position);
				position += 4;
				var state = bytes
					.Skip(position)
					.Take(stateLen)
					.ToArray();

				position += stateLen;
				States.Add(frame, state);
				Used += state.Length;
			}
		}

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

		public int LastKey
		{
			get
			{
				var kk = States.Keys;
				int index = kk.Count;
				if (index == 0)
					return 0;
				return kk[index - 1];
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
