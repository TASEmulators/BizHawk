using System;
using System.Collections.Generic;
using System.IO;

using LuaInterface;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public sealed class MemorySavestateEmuLuaLibrary : LuaLibraryBase
	{
		public MemorySavestateEmuLuaLibrary(Lua lua)
			: base(lua) { }

		public MemorySavestateEmuLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "memorysavestate"; } }

		private readonly Dictionary<Guid, byte[]> MemorySavestates = new Dictionary<Guid, byte[]>();

		[RequiredService]
		private IStatable _statableCore { get; set; }

		[LuaMethodAttributes(
			"savecorestate",
			"creates a core savestate and stores it in memory.  Note: a core savestate is only the raw data from the core, and not extras such as movie input logs, or framebuffers. Returns a unique identifer for the savestate"
		)]
		public string SaveCoreStateToMemory()
		{
			var guid = Guid.NewGuid();
			var bytes = (byte[])_statableCore.SaveStateBinary().Clone();

			MemorySavestates.Add(guid, bytes);

			return guid.ToString();
		}

		[LuaMethodAttributes(
			"loadcorestate",
			"loads an in memory state with the given identifier"
		)]
		public void LoadCoreStateFromMemory(string identifier)
		{
			var guid = new Guid(identifier);

			try
			{
				var state = MemorySavestates[guid];

				using (MemoryStream ms = new MemoryStream(state))
				using (BinaryReader br = new BinaryReader(ms))
				{
					_statableCore.LoadStateBinary(br);
				}
			}
			catch
			{
				Log("Unable to find the given savestate in memory");
			}
		}

		[LuaMethodAttributes(
			"removestate",
			"removes the savestate with the given identifier from memory"
		)]
		public void DeleteState(string identifier)
		{
			var guid = new Guid(identifier);
			MemorySavestates.Remove(guid);
		}

		[LuaMethodAttributes(
			"clearstatesfrommemory",
			"clears all savestates stored in memory"
		)]
		public void ClearInMemoryStates()
		{
			MemorySavestates.Clear();
		}
	}
}
