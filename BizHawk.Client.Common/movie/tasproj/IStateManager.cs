using System;

namespace BizHawk.Client.Common
{
	using System.Collections.Generic;
	using System.IO;

	using BizHawk.Common;

	public interface IStateManager : IDisposable
	{
		// byte[] this[int frame] { get; } // TODO: I had it refactored to this back in the day
		KeyValuePair<int, byte[]> this[int frame] { get; }

		TasStateManagerSettings Settings { get; set; }

		Action<int> InvalidateCallback { set; }

		void Capture(bool force = false);

		bool HasState(int frame);

		bool Invalidate(int frame);

		// TODO: rename to Clear()
		// TODO: consider it passing a bool if anything was cleared, and the .Any() could go away
		void ClearStateHistory();

		void Save(BinaryWriter bw);

		void Load(BinaryReader br);

		KeyValuePair<int, byte[]> GetStateClosestToFrame(int frame);

		bool Any();

		// TODO: rename to Last
		int LastStatedFrame { get; }

		// ********* Delete these **********
		void MountWriteAccess();

		// TODO: delete me, I don't work
		NDBDatabase NdbDatabase { get; }

		// *********** Reconsider these ************/
		void LimitStateCount();

		void UpdateStateFrequency();

		bool RemoveState(int frame);

		int LastEditedFrame { get; }
		bool StateIsMarker(int frame);

		int StateCount { get; }

		int GetStateIndexByFrame(int frame);

		int GetStateFrameByIndex(int index);
	}
}
