using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Client.Common
{
	public interface IStateManager
	{
		// byte[] this[int frame] { get; } // TODO: I had it refactored to this back in the day

		/// <summary>
		/// Retrieves the savestate for the given frame,
		/// If this frame does not have a state currently, will return an empty array
		/// </summary>
		/// <returns>A savestate for the given frame or an empty array if there isn't one</returns>
		KeyValuePair<int, byte[]> this[int frame] { get; }

		TasStateManagerSettings Settings { get; set; }

		Action<int> InvalidateCallback { set; }

		/// <summary>
		/// Requests that the current emulator state be captured 
		/// Unless force is true, the state may or may not be captured depending on the logic employed by "green-zone" management
		/// </summary>
		void Capture(bool force = false);

		bool HasState(int frame);

		/// <summary>
		/// Clears out all savestates after the given frame number
		/// </summary>
		bool Invalidate(int frame);

		void Clear();

		void Save(BinaryWriter bw);

		void Load(BinaryReader br);

		KeyValuePair<int, byte[]> GetStateClosestToFrame(int frame);

		bool Any();

		int Count { get; }

		int Last { get; }

		bool IsMarkerState(int frame);

		void UpdateStateFrequency();

		/// <summary>
		/// Returns index of the state right above the given frame
		/// </summary>
		int GetStateIndexByFrame(int frame);

		/// <summary>
		/// Returns frame of the state at the given index
		/// </summary>
		int GetStateFrameByIndex(int index);

		bool Remove(int frame);
	}
}
