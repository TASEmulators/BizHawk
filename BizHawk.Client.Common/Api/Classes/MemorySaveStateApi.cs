using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class MemorySaveStateApi : IMemorySaveState
	{
		[RequiredService]
		private IStatable StatableCore { get; set; }

		public MemorySaveStateApi(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public MemorySaveStateApi() : this(Console.WriteLine) {}

		private readonly Action<string> LogCallback;

		private readonly Dictionary<Guid, byte[]> _memorySavestates = new Dictionary<Guid, byte[]>();

		public string SaveCoreStateToMemory()
		{
			var guid = Guid.NewGuid();
			_memorySavestates.Add(guid, (byte[]) StatableCore.SaveStateBinary().Clone());
			return guid.ToString();
		}

		public void LoadCoreStateFromMemory(string identifier)
		{
			var guid = new Guid(identifier);
			try
			{
				using var ms = new MemoryStream(_memorySavestates[guid]);
				using var br = new BinaryReader(ms);
				StatableCore.LoadStateBinary(br);
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
