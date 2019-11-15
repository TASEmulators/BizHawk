using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.ApiHawk
{
	public sealed class MemorySaveStateApi : IMemorySaveState
	{
		public MemorySaveStateApi()
		{ }

		[RequiredService]
		private IStatable StatableCore { get; set; }

		private readonly Dictionary<Guid, byte[]> _memorySavestates = new Dictionary<Guid, byte[]>();

		public string SaveCoreStateToMemory()
		{
			var guid = Guid.NewGuid();
			var bytes = (byte[])StatableCore.SaveStateBinary().Clone();

			_memorySavestates.Add(guid, bytes);

			return guid.ToString();
		}

		public void LoadCoreStateFromMemory(string identifier)
		{
			var guid = new Guid(identifier);

			try
			{
				var state = _memorySavestates[guid];

				using var ms = new MemoryStream(state);
				using var br = new BinaryReader(ms);
				StatableCore.LoadStateBinary(br);
			}
			catch
			{
				Console.WriteLine("Unable to find the given savestate in memory");
			}
		}

		public void DeleteState(string identifier)
		{
			var guid = new Guid(identifier);
			_memorySavestates.Remove(guid);
		}

		public void ClearInMemoryStates()
		{
			_memorySavestates.Clear();
		}
	}
}
