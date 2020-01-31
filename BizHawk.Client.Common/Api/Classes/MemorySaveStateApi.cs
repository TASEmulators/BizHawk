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

		private readonly Action<string> _logCallback;

		private readonly Dictionary<Guid, byte[]> _memorySavestates = new Dictionary<Guid, byte[]>();

		public MemorySaveStateApi(Action<string> logCallback)
		{
			_logCallback = logCallback;
		}

		public MemorySaveStateApi() : this(Console.WriteLine) {}

		public void ClearInMemoryStates() => _memorySavestates.Clear();

		public void DeleteState(Guid guid) => _memorySavestates.Remove(guid);

		public void LoadCoreStateFromMemory(Guid guid)
		{
			try
			{
				using var ms = new MemoryStream(_memorySavestates[guid]);
				using var br = new BinaryReader(ms);
				StatableCore.LoadStateBinary(br);
			}
			catch
			{
				_logCallback("Unable to find the given savestate in memory");
			}
		}

		public Guid SaveCoreStateToMemory()
		{
			var guid = Guid.NewGuid();
			_memorySavestates.Add(guid, (byte[]) StatableCore.SaveStateBinary().Clone());
			return guid;
		}
	}
}
