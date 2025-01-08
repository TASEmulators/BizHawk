using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IStateManager : IDisposable
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

		/// <summary>
		/// Commands the state manager to remove a reserved state for the given frame, if it is exists
		/// </summary>
		void EvictReserved(int frame);

		bool HasState(int frame);

		/// <summary>
		/// Clears out all savestates after the given frame number
		/// </summary>
		bool InvalidateAfter(int frame);

		// Remove all states, but not the frame 0 state
		void Clear();

		/// <summary>
		/// Get a nearby state.  The returned frame must be less than or equal to the passed frame.
		/// This may not fail; the StateManager strongly holds a frame 0 state to ensure there's always a possible result.
		/// </summary>
		/// <returns>This stream may be consumed only once, and before any other calls to statemanager occur</returns>
		KeyValuePair<int, Stream> GetStateClosestToFrame(int frame);

		/// <value>the total number of states currently held by the state manager</value>
		int Count { get; }

		/// <value>the most recent frame number that the state manager possesses</value>
		int Last { get; }

		/// <summary>
		/// Updates the internal state saving logic settings
		/// </summary>
		void UpdateSettings(ZwinderStateManagerSettings settings, bool keepOldStates = false);

		/// <summary>
		/// Serializes the current state of the instance for persisting to disk
		/// </summary>
		void SaveStateHistory(BinaryWriter bw);

		/// <summary>
		/// Enables the instance to be used. An instance of <see cref="IStateManager"/> should not
		/// be useable until this method is called
		/// </summary>
		void Engage(byte[] frameZeroState);
	}
}
