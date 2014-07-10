using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Captures savestates and manages the logic of adding, retrieving, 
	/// invalidating/clearing of states.  Also does memory management and limiting of states
	/// </summary>
	public class TasStateManager
	{
		private readonly Dictionary<int, byte[]> States = new Dictionary<int, byte[]>();

		/// <summary>
		/// Retrieves the savestate for the given frame,
		/// If this frame does not have a state currently, will return an empty array
		/// </summary>
		/// <returns>A savestate for the given frame or an empty array if there isn't one</returns>
		public byte[] this[int frame]
		{
			get
			{
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
				States.Add(frame, state);
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
			// TODO be more efficient, this could get slow
			var toRemove = States
				.Where(x => x.Key > frame)
				.Select(x => x.Key)
				.ToList();

			foreach (var f in toRemove)
			{
				States.Remove(f);
			}
		}

		/// <summary>
		/// Clears all state information
		/// </summary>
		public void Clear()
		{
			States.Clear();
		}
	}
}
