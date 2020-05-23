#nullable enable

using System;

using BizHawk.API.ApiHawk;
using BizHawk.API.Base;
using BizHawk.Client.EmuHawk.APIImpl;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal sealed class MemorySaveStateLibLegacyImpl : LibBase<GlobalsAccessAPIEnvironment>, IMemorySaveStateLib, IMemorySaveState
	{
		private IStatable StatableCore => Env.StatableCore ?? throw new NullReferenceException();

		public MemorySaveStateLibLegacyImpl(out Action<GlobalsAccessAPIEnvironment> updateEnv) : base(out updateEnv) {}

		[LegacyApiHawk]
		public void ClearInMemoryStates() => ClearSnapshots();

		public void ClearSnapshots() => Env.MemorySnapshots.Clear();

		public Guid CreateSnapshot()
		{
			var guid = Guid.NewGuid();
			Env.MemorySnapshots.Add(guid, (byte[]) StatableCore.SaveStateBinary().Clone());
			return guid;
		}

		[LegacyApiHawk]
		public void DeleteState(string identifier) => RemoveSnapshotWithID(new Guid(identifier));

		[LegacyApiHawk]
		public void LoadCoreStateFromMemory(string identifier) => LoadSnapshotWithID(new Guid(identifier));

		public void LoadSnapshotWithID(Guid guid)
		{
			try
			{
				StatableCore.LoadStateBinary(Env.MemorySnapshots[guid]);
			}
			catch
			{
				Env.LogCallback("Unable to find the given savestate in memory");
			}
		}

		public void RemoveSnapshotWithID(Guid guid) => Env.MemorySnapshots.Remove(guid);

		[LegacyApiHawk]
		public string SaveCoreStateToMemory() => CreateSnapshot().ToString();
	}
}
