using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IStateManager
	{
		/// <summary>
		/// Retrieves the savestate for the given frame,
		/// If this frame does not have a state currently, will return an empty array
		/// </summary>
		/// <returns>A savestate for the given frame or an empty array if there isn't one</returns>
		byte[] this[int frame] { get; }

		/// <summary>
		/// Attaches a core to the given state manager instance, this must be done and
		/// it must be done only once, a state manager can not and should not exist for more
		/// than the lifetime of the core
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown if attempting to attach a core when one is already attached
		/// or if the given core does not meet all required dependencies
		/// </exception>
		void Attach(IEmulator emulator);

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

		void UpdateStateFrequency();

		/// <summary>
		/// Returns index of the state right above the given frame
		/// </summary>
		int GetStateIndexByFrame(int frame);

		/// <summary>
		/// Returns frame of the state at the given index
		/// </summary>
		int GetStateFrameByIndex(int index);

		/// <summary>
		/// Directly remove a state from the given frame, if it exists
		/// Should only be called by pruning operations
		/// </summary>
		bool Remove(int frame);
	}
}
