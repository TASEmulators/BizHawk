using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class MemorySaveStateApi : IMemorySaveStateApi
	{
		[RequiredService]
		private IStatable StatableCore { get; set; }

		private readonly Action<string> LogCallback;

		private readonly Dictionary<Guid, byte[]> _memorySavestates = new Dictionary<Guid, byte[]>();

		public MemorySaveStateApi(Action<string> logCallback) => LogCallback = logCallback;

		public string SaveCoreStateToMemory()
		{
			var guid = Guid.NewGuid();
			_memorySavestates.Add(guid, StatableCore.CloneSavestate());
			return guid.ToString();
		}

		public void LoadCoreStateFromMemory(string identifier)
		{
			var guid = new Guid(identifier);
			try
			{
				StatableCore.LoadStateBinary(_memorySavestates[guid]);
			}
			catch
			{
				LogCallback("Unable to find the given savestate in memory");
			}
		}

		public void DeleteState(string identifier) => _memorySavestates.Remove(new Guid(identifier));

		public void ClearInMemoryStates() => _memorySavestates.Clear();
	}
}
