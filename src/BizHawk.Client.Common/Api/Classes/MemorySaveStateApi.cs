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
#pragma warning disable RS0030 // this is to ensure no collisions
			var guid = Guid.NewGuid();
#pragma warning restore RS0030
			_memorySavestates.Add(guid, StatableCore.CloneSavestate());
			return guid.ToString("D");
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
