using System;
using System.IO;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Represents a savestate in the <seealso cref="TasStateManager"/>
	/// </summary>
	internal class StateManagerState : IDisposable
	{
		public int Frame { get; }

		public byte[] State { get; set; }

		public int Length => State.Length;

	    public StateManagerState(byte[] state, int frame)
		{
			State = state;
			Frame = frame;
		}

		public void Dispose()
		{
		}
	}
}
