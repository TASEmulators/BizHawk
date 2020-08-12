using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IStateManager
	{
		/// <summary>
		/// Retrieves the savestate for the given frame,
		/// If this frame does not have a state currently, will return an empty array.false
		/// Try not to use this as it is not fast.
		/// </summary>
		/// <returns>A savestate for the given frame or an empty array if there isn't one</returns>
		byte[] this[int frame] { get; }

		ZwinderStateManagerSettings Settings { get; }

		/// <summary>
		/// Requests that the current emulator state be captured 
		/// Unless force is true, the state may or may not be captured depending on the logic employed by "green-zone" management
		/// </summary>
		void Capture(int frame, IStatable source, bool force = false);

		bool HasState(int frame);

		/// <summary>
		/// Clears out all savestates after the given frame number
		/// </summary>
		bool InvalidateAfter(int frame);

		// Remove all states, but not the frame 0 state
		void Clear();

		/// <summary>
		/// Get a nearby state.  The returned frame must be less then or equal to the passed frame.
		/// This may not fail; the StateManager strongly holds a frame 0 state to ensure there's always a possible result.
		/// </summary>
		/// <param name="frame"></param>
		/// <returns>This stream may be consumed only once, and before any other calls to statemanager occur</returns>
		KeyValuePair<int, Stream> GetStateClosestToFrame(int frame);

		/// <summary>
		/// Returns the total number of states currently held by the state manager
		/// </summary>
		/// <value></value>
		int Count { get; }

		/// <summary>
		/// Returns the most recent frame number that the state manager possesses
		/// </summary>
		/// <value></value>
		int Last { get; }

		/// <summary>
		/// Updates the internal state saving logic settings
		/// </summary>
		void UpdateSettings(ZwinderStateManagerSettings settings);

		/// <summary>
		/// Serializes the current state of the instance for persisting to disk
		/// </summary>
		void SaveStateHistory(BinaryWriter bw);

		/// <summary>
		/// Enables the instance to be used. An instance of <see cref="IStateManager"/> should not
		/// be useable until this method is called
		/// </summary>
		/// <param name="frameZeroState"></param>
		void Engage(byte[] frameZeroState);
	}
}
