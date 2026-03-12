using System.Collections.Generic;
using System.IO;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	internal class FakeStateManager : IStateManager
	{
		public IStateManagerSettings Settings => throw new NotImplementedException();

		public int Count => throw new NotImplementedException();

		public int Last => throw new NotImplementedException();

		public void Capture(int frame, IStatable source, bool force) => throw new NotImplementedException();
		public void Clear() => throw new NotImplementedException();
		public void Dispose() => throw new NotImplementedException();
		public void Engage(byte[] frameZeroState) { /* nothing */ }
		public KeyValuePair<int, Stream> GetStateClosestToFrame(int frame) => throw new NotImplementedException();
		public bool HasState(int frame) => throw new NotImplementedException();
		public bool InvalidateAfter(int frame) => false;
		public void LoadStateHistory(BinaryReader br) => throw new NotImplementedException();
		public void SaveStateHistory(BinaryWriter bw) => throw new NotImplementedException();
		public void Unreserve(int frame) { /* nothing */ }
		public IStateManager UpdateSettings(IStateManagerSettings settings, bool keepOldStates = false) => throw new NotImplementedException();
	}

	internal class FakeStateManagerSettings : IStateManagerSettings
	{
		public IStateManagerSettings Clone() => throw new NotImplementedException();
		public IStateManager CreateManager(Func<int, bool> reserveCallback) => new FakeStateManager();
	}
}
