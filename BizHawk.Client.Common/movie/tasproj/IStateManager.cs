using System;

namespace BizHawk.Client.Common
{
	using System.Collections.Generic;
	using System.IO;

	using BizHawk.Common;

	public interface IStateManager
	{
		// byte[] this[int frame] { get; } // TODO: I had it refactored to this back in the day
		KeyValuePair<int, byte[]> this[int frame] { get; }

		TasStateManagerSettings Settings { get; set; }

		Action<int> InvalidateCallback { set; }

		void Capture(bool force = false);

		bool HasState(int frame);

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

		int GetStateIndexByFrame(int frame);

		int GetStateFrameByIndex(int index);

		bool Remove(int frame);

		// ********* Delete these **********
		void MountWriteAccess();
	}
}
